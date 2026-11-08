using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

const string apiUrl = "http://127.0.0.1:5080/";
const string webUrl = "http://127.0.0.1:5000/";
var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/L9sAAAAASUVORK5CYII=");
using var api = Browser(apiUrl);
using var web = Browser(webUrl);

await WaitUntilReady(api, "health");
await WaitUntilReady(web, "");
var email = $"smoke-{Guid.NewGuid():N}@example.test";
const string password = "LocalSmoke123!";
await SendJson(api, HttpMethod.Post, "api/auth/register", new
{
    firstName = "Local", lastName = "Smoke Test", email, password, role = "Organiser"
});
await SendJson(api, HttpMethod.Post, "api/auth/login", new { email, password });
using (var login = await web.PostAsync("Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
{
    ["Email"] = email, ["Password"] = password,
    ["__RequestVerificationToken"] = await Token(web, "Account/Login")
})))
{
    login.EnsureSuccessStatusCode();
    Check(login.RequestMessage!.RequestUri!.AbsolutePath.StartsWith("/Organiser"), "MVC login failed.");
}

var locations = await api.GetFromJsonAsync<JsonElement>("api/locations");
var types = await api.GetFromJsonAsync<JsonElement>("api/event-types");
var today = DateOnly.FromDateTime(DateTime.Today);
var eventFields = new Dictionary<string, string>
{
    ["EventName"] = "Local smoke test race", ["Description"] = "Temporary local integration fixture.",
    ["EventDate"] = today.AddDays(30).ToString("yyyy-MM-dd"),
    ["RegistrationDeadline"] = today.AddDays(28).ToString("yyyy-MM-dd"),
    ["DistanceKm"] = "10", ["LocationId"] = locations[0].GetProperty("locationId").ToString(),
    ["EventTypeId"] = types[0].GetProperty("eventTypeId").ToString(),
    ["__RequestVerificationToken"] = await Token(web, "Organiser/Create")
};
using var eventForm = ImageForm(eventFields, "BannerFile", png);
using var created = await web.PostAsync("Organiser/Create", eventForm);
created.EnsureSuccessStatusCode();
var createdPath = created.RequestMessage!.RequestUri!.AbsolutePath;
Check(createdPath.StartsWith("/Organiser/Categories/"), "Event creation or banner upload failed.");
var eventId = int.Parse(createdPath.Split('/').Last());
var images = await api.GetFromJsonAsync<JsonElement>($"api/events/{eventId}/images");
Check(images.GetArrayLength() == 1, "The API did not persist the banner.");
var image = images[0];
var imageId = image.GetProperty("eventImageId").GetInt32();
Check(image.GetProperty("imageUrl").GetString()!.Contains("/raceday-images/events/"), "This check requires the Blob-enabled demo stack.");
Check((await web.GetByteArrayAsync($"Media/Event/{imageId}")).SequenceEqual(png), "Banner bytes changed during the Blob round-trip.");
Check((await web.GetStringAsync($"Events/Details/{eventId}")).Contains("/Media/Event/"), "The event page did not display its banner.");

using var profileForm = ImageForm(new Dictionary<string, string>
{
    ["FirstName"] = "Local", ["LastName"] = "Smoke Test", ["PhoneNumber"] = "",
    ["__RequestVerificationToken"] = await Token(web, "Profile")
}, "PictureFile", png);
using var profile = await web.PostAsync("Profile", profileForm);
profile.EnsureSuccessStatusCode();
var user = await api.GetFromJsonAsync<JsonElement>("api/users/me");
Check(user.GetProperty("profilePictureUrl").GetString()!.Contains("/raceday-images/profiles/"), "Profile Blob URL was not persisted.");
Check((await web.GetByteArrayAsync("Profile/Picture")).SequenceEqual(png), "Profile image did not round-trip.");

using var logout = await web.PostAsync("Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
{
    ["__RequestVerificationToken"] = await Token(web, "Profile")
}));
logout.EnsureSuccessStatusCode();
using var protectedPage = await web.GetAsync("Profile");
Check(protectedPage.RequestMessage!.RequestUri!.AbsolutePath == "/Account/Login", "Logout did not clear the session.");
await SendJson(api, HttpMethod.Delete, $"api/event-images/{imageId}");
await SendJson(api, HttpMethod.Delete, $"api/events/{eventId}");
Console.WriteLine("PASS: MVC session, event creation, API-mediated Blob banner, persisted URL, image display, profile picture and logout.");

static HttpClient Browser(string url) => new(new HttpClientHandler
{
    CookieContainer = new CookieContainer(), AllowAutoRedirect = true
}) { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(20) };

static async Task WaitUntilReady(HttpClient client, string path)
{
    // A newly started container may reset a connection before its HTTP listener is ready.
    for (var attempt = 0; attempt < 45; attempt++)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var response = await client.GetAsync(path, timeout.Token);
            if (response.IsSuccessStatusCode) return;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { }
        await Task.Delay(TimeSpan.FromSeconds(2));
    }
    throw new InvalidOperationException($"Local service did not become ready: {client.BaseAddress}");
}

static async Task<string> Token(HttpClient client, string path)
{
    var page = await client.GetStringAsync(path);
    var match = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
    Check(match.Success, "The form did not include an antiforgery token.");
    return WebUtility.HtmlDecode(match.Groups[1].Value);
}

static MultipartFormDataContent ImageForm(Dictionary<string, string> fields, string field, byte[] bytes)
{
    var form = new MultipartFormDataContent();
    foreach (var item in fields) form.Add(new StringContent(item.Value), item.Key);
    var file = new ByteArrayContent(bytes);
    file.Headers.ContentType = new("image/png");
    form.Add(file, field, "smoke.png");
    return form;
}

static async Task SendJson(HttpClient client, HttpMethod method, string path, object? body = null)
{
    using var request = new HttpRequestMessage(method, path);
    if (body is not null) request.Content = JsonContent.Create(body);
    using var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
