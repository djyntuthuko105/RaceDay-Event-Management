using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
namespace RaceDay.Web.Tests;
public class MvcWorkflowTests : IDisposable
{
    private readonly WebFactory factory = new();
    private HttpClient Browser() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true, BaseAddress = new Uri("https://localhost") });
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/L9sAAAAASUVORK5CYII=");
    private static async Task<string> Token(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, html);
        return WebUtility.HtmlDecode(token.Groups[1].Value);
    }
    private static async Task<HttpResponseMessage> Post(HttpClient client, string page, string action, Dictionary<string,string> fields)
    {
        fields["__RequestVerificationToken"] = await Token(client, page);
        return await client.PostAsync(action, new FormUrlEncodedContent(fields));
    }
    private static async Task Login(HttpClient client, string email)
    {
        var response = await Post(client, "/Account/Login", "/Account/Login", new() { ["Email"] = email, ["Password"] = "Password123!" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    [Fact]
    public async Task Home_FiltersEventsAndShowsSportingLayout()
    {
        var client = Browser();
        var home = await client.GetStringAsync("/");
        Assert.Contains("Your next", home);
        Assert.Contains("Pretoria City Run", home);
        var filtered = await client.GetStringAsync("/Events?city=Pretoria");
        Assert.Contains("Pretoria City Run", filtered);
        Assert.DoesNotContain("Bloemfontein Family Walk", filtered);
    }
    [Fact]
    public async Task AnonymousProtectedPages_RedirectToLogin()
    {
        var client = Browser();
        foreach (var path in new[] { "/Profile", "/Participant/Events", "/Participant/Results", "/Organiser" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("Account/Login", response.Headers.Location!.ToString());
        }
    }
    [Fact]
    public async Task Forms_RejectMissingAntiforgeryTokens()
    {
        var response = await Browser().PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string,string> { ["Email"] = "kabelo@example.com", ["Password"] = "Password123!" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task Login_RejectsWrongPasswordWithoutRawErrors()
    {
        var client = Browser();
        var response = await Post(client, "/Account/Login", "/Account/Login", new() { ["Email"] = "kabelo@example.com", ["Password"] = "WrongPassword" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Check your email and password", await response.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task Registration_AllowsBothRolesAndCreatesRealApiAccounts()
    {
        foreach (var role in new[] { "Organiser", "Participant" })
        {
            var client = Browser();
            var email = Guid.NewGuid().ToString("N") + "@example.test";
            var response = await Post(client, "/Account/Register", "/Account/Register", new() { ["FirstName"] = "Test", ["LastName"] = "Runner", ["Email"] = email, ["Password"] = "Password123!", ["Role"] = role });
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            await Login(client, email);
            var profile = await client.GetStringAsync("/Profile");
            Assert.Contains(email, profile);
            Assert.Contains(role, profile);
        }
    }
    [Fact]
    public async Task Sessions_IsolateUsersEnforceRolesAndClearOnLogout()
    {
        var participant = Browser();
        await Login(participant, "kabelo@example.com");
        Assert.Equal(HttpStatusCode.Forbidden, (await participant.GetAsync("/Organiser")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await Browser().GetAsync("/Participant/Events")).StatusCode);
        var organiser = Browser();
        await Login(organiser, "thabo@raceday.co.za");
        var dashboard = await organiser.GetStringAsync("/Organiser");
        Assert.Contains("Pretoria City Run", dashboard);
        Assert.DoesNotContain("Bloemfontein Family Walk", dashboard);
        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/Participant/Events")).StatusCode);
        var logout = await Post(participant, "/Profile", "/Account/Logout", new());
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await participant.GetAsync("/Profile")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await organiser.GetAsync("/Profile")).StatusCode);
    }
    [Fact]
    public async Task Participant_CanUpdateDetailsAndUploadProfilePicture()
    {
        var participant = Browser();
        await Login(participant, "kabelo@example.com");
        var token = await Token(participant, "/Profile");
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(token), "__RequestVerificationToken");
        form.Add(new StringContent("Updated"), "FirstName");
        form.Add(new StringContent("Runner"), "LastName");
        form.Add(new StringContent("0825550100"), "PhoneNumber");
        var file = new ByteArrayContent(Png); file.Headers.ContentType = new("image/png");
        form.Add(file, "PictureFile", "profile.png");
        var response = await participant.PostAsync("/Profile", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Updated Runner", await participant.GetStringAsync("/Profile"));
        Assert.Equal(Png, await participant.GetByteArrayAsync("/Profile/Picture"));
    }
    [Fact]
    public async Task FullRaceFlow_CreatesCategoryEnrolmentBannerAndPublishedResult()
    {
        var organiser = Browser(); await Login(organiser, "thabo@raceday.co.za");
        var participant = Browser(); await Login(participant, "kabelo@example.com");
        var future = DateOnly.FromDateTime(DateTime.Today.AddDays(20));
        var fields = new Dictionary<string,string> { ["EventName"] = "MVC integration race", ["Description"] = "A test race.", ["EventDate"] = future.ToString("yyyy-MM-dd"), ["RegistrationDeadline"] = future.AddDays(-2).ToString("yyyy-MM-dd"), ["DistanceKm"] = "10", ["LocationId"] = "1", ["EventTypeId"] = "1" };
        var created = await Post(organiser, "/Organiser/Create", "/Organiser/Create", fields);
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var id = Regex.Matches(created.Headers.Location!.ToString(), "[0-9]+").Last().Value;
        var category = await Post(organiser, $"/Organiser/Categories/{id}", $"/Organiser/Categories/{id}", new() { ["CategoryName"] = "Open 10 km", ["CategoryDistanceKm"] = "10" });
        Assert.Equal(HttpStatusCode.Redirect, category.StatusCode);
        var detail = await participant.GetStringAsync($"/Events/Details/{id}");
        var categoryId = Regex.Match(detail, "<option value=\"([0-9]+)\">Open 10 km").Groups[1].Value;
        Assert.NotEmpty(categoryId);
        var entry = await Post(participant, $"/Events/Details/{id}", $"/Events/Enter/{id}", new() { ["CategoryId"] = categoryId });
        Assert.Equal(HttpStatusCode.Redirect, entry.StatusCode);
        Assert.Contains("MVC integration race", await participant.GetStringAsync("/Participant/Events"));
        var duplicate = await Post(participant, $"/Events/Details/{id}", $"/Events/Enter/{id}", new() { ["CategoryId"] = categoryId });
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        Assert.Contains("already", (await duplicate.Content.ReadAsStringAsync()).ToLowerInvariant());
        var token = await Token(organiser, $"/Organiser/Edit/{id}");
        fields["EventDate"] = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)).ToString("yyyy-MM-dd");
        fields["RegistrationDeadline"] = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)).ToString("yyyy-MM-dd");
        fields["__RequestVerificationToken"] = token;
        using var edit = new MultipartFormDataContent();
        foreach (var field in fields) edit.Add(new StringContent(field.Value), field.Key);
        var file = new ByteArrayContent(Png); file.Headers.ContentType = new("image/png"); edit.Add(file, "BannerFile", "banner.png");
        Assert.Equal(HttpStatusCode.Redirect, (await organiser.PostAsync($"/Organiser/Edit/{id}", edit)).StatusCode);
        detail = await participant.GetStringAsync($"/Events/Details/{id}");
        var imageId = Regex.Match(detail, "/Media/Event/([0-9]+)").Groups[1].Value;
        Assert.NotEmpty(imageId);
        Assert.Equal(Png, await participant.GetByteArrayAsync($"/Media/Event/{imageId}"));
        var enrolments = await organiser.GetStringAsync($"/Organiser/Enrolments/{id}");
        var enrolmentId = Regex.Match(enrolments, "name=\"FinishTime\"|id=\"time-([0-9]+)\"").Groups[1].Value;
        Assert.NotEmpty(enrolmentId);
        var result = await Post(organiser, $"/Organiser/Enrolments/{id}", $"/Organiser/SaveResult/{id}?enrolmentId={enrolmentId}", new() { ["FinishTime"] = "00:45:32", ["FinishPosition"] = "1" });
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        var history = await participant.GetStringAsync("/Participant/Results");
        Assert.Contains("MVC integration race", history); Assert.Contains("00:45:32", history); Assert.Contains("out of 1 recorded finishers", history);
    }
    public void Dispose() => factory.Dispose();
}
