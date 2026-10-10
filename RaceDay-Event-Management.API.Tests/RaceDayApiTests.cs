using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RaceDay_Event_Management.API.Tests;

public class RaceDayApiTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;

    public RaceDayApiTests(RaceDayApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_CreatesAccount_WithoutReturningThePassword()
    {
        var client = CreateClient();
        var email = NewEmail("register");

        var response = await client.PostAsJsonAsync("/api/auth/register", Account(email, "Participant"));

        await AssertStatus(response, HttpStatusCode.Created);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(email, body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_SameEmailTwice_ReturnsConflict()
    {
        var client = CreateClient();
        var email = NewEmail("duplicate");

        var first = await client.PostAsJsonAsync("/api/auth/register", Account(email, "Organiser"));
        await AssertStatus(first, HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/auth/register", Account(email, "Organiser"));
        await AssertStatus(second, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_WithTheRightPassword_StartsASession()
    {
        var client = CreateClient();
        var email = NewEmail("login");
        await AssertStatus(
            await client.PostAsJsonAsync("/api/auth/register", Account(email, "Participant")),
            HttpStatusCode.Created);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });
        await AssertStatus(login, HttpStatusCode.OK);

        var session = await client.GetAsync("/api/auth/session");
        await AssertStatus(session, HttpStatusCode.OK);
        var json = await ReadJson(session);
        Assert.Equal("Participant", json.GetProperty("role").GetString());
        Assert.Equal(email, json.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_WithTheWrongPassword_IsRejected()
    {
        var client = CreateClient();
        var email = NewEmail("bad-password");
        await AssertStatus(
            await client.PostAsJsonAsync("/api/auth/register", Account(email, "Participant")),
            HttpStatusCode.Created);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "not-the-password" });
        await AssertStatus(login, HttpStatusCode.Unauthorized);

        var session = await client.GetAsync("/api/auth/session");
        await AssertStatus(session, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var client = await RegisterAndLogin("Participant");

        var logout = await client.PostAsync("/api/auth/logout", null);
        await AssertStatus(logout, HttpStatusCode.OK);

        var session = await client.GetAsync("/api/auth/session");
        await AssertStatus(session, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateEvent_WithoutLogin_ReturnsUnauthorized()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/events", new { eventName = "Nobody" });
        await AssertStatus(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        var participant = await RegisterAndLogin("Participant");
        var response = await participant.PostAsJsonAsync("/api/events", new { eventName = "Not allowed" });
        await AssertStatus(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Organiser_CanCreateUpdateAndDeleteAnEvent()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var eventId = await CreateBareEvent(organiser);

        var eventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(20));
        var updated = await organiser.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Updated park run",
            description = "The name was changed by the organiser.",
            eventDate,
            distanceKm = 5,
            registrationDeadline = eventDate.AddDays(-2),
            eventTypeId = await GetOrCreateRunType(organiser),
            locationId = await ReadLocationId(organiser, eventId)
        });
        await AssertStatus(updated, HttpStatusCode.OK);
        Assert.Equal("Updated park run", (await ReadJson(updated)).GetProperty("eventName").GetString());

        var removed = await organiser.DeleteAsync($"/api/events/{eventId}");
        await AssertStatus(removed, HttpStatusCode.NoContent);

        var missing = await organiser.GetAsync($"/api/events/{eventId}");
        await AssertStatus(missing, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task EventDetail_IncludesTheCategoriesForThatEvent()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);

        var detail = await CreateClient().GetAsync($"/api/events/{eventId}");
        await AssertStatus(detail, HttpStatusCode.OK);

        var categories = (await ReadJson(detail)).GetProperty("categories");
        var ids = categories.EnumerateArray().Select(item => item.GetProperty("categoryId").GetInt32());
        Assert.Contains(categoryId, ids);
    }

    [Fact]
    public async Task UpdateEvent_ByAnotherOrganiser_ReturnsForbidden()
    {
        var owner = await RegisterAndLogin("Organiser");
        var eventId = await CreateBareEvent(owner);
        var other = await RegisterAndLogin("Organiser");
        var detail = await ReadJson(await other.GetAsync($"/api/events/{eventId}"));
        var eventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(20));

        var response = await other.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Taken over",
            description = "Another organiser should not be able to change this event.",
            eventDate,
            distanceKm = 5,
            registrationDeadline = eventDate.AddDays(-2),
            eventTypeId = detail.GetProperty("eventTypeId").GetInt32(),
            locationId = detail.GetProperty("locationId").GetInt32()
        });
        await AssertStatus(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteEvent_WithACategory_ReturnsConflict()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var (eventId, _) = await CreateEventWithCategory(organiser);

        var response = await organiser.DeleteAsync($"/api/events/{eventId}");
        await AssertStatus(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateProfile_ChangesTheLoggedInUsersName()
    {
        var participant = await RegisterAndLogin("Participant");

        var response = await participant.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Amahle",
            lastName = "Nkosi",
            phoneNumber = "0821112233"
        });

        await AssertStatus(response, HttpStatusCode.OK);
        var profile = await ReadJson(response);
        Assert.Equal("Amahle", profile.GetProperty("firstName").GetString());
        Assert.Equal("Nkosi", profile.GetProperty("lastName").GetString());
    }

    [Fact]
    public async Task UpcomingEvents_CanBeListedWithoutLoggingIn()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var eventId = await CreateBareEvent(organiser);

        var guest = CreateClient();
        var list = await guest.GetAsync("/api/events");
        await AssertStatus(list, HttpStatusCode.OK);

        var json = await ReadJson(list);
        var ids = json.EnumerateArray().Select(item => item.GetProperty("eventId").GetInt32());
        Assert.Contains(eventId, ids);
    }

    [Fact]
    public async Task Participant_CanEnrol_AndASecondEnrolmentConflicts()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);
        var participant = await RegisterAndLogin("Participant");

        var first = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        await AssertStatus(first, HttpStatusCode.Created);
        var enrolmentId = (await ReadJson(first)).GetProperty("enrolmentId").GetInt32();

        var mine = await participant.GetAsync("/api/enrolments/me");
        await AssertStatus(mine, HttpStatusCode.OK);
        var myIds = (await ReadJson(mine)).EnumerateArray().Select(item => item.GetProperty("enrolmentId").GetInt32());
        Assert.Contains(enrolmentId, myIds);

        var organiserView = await organiser.GetAsync($"/api/events/{eventId}/enrolments");
        await AssertStatus(organiserView, HttpStatusCode.OK);
        Assert.Contains(
            enrolmentId,
            (await ReadJson(organiserView)).EnumerateArray().Select(item => item.GetProperty("enrolmentId").GetInt32()));

        var second = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        await AssertStatus(second, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        await AssertStatus(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Organiser_CanRecordAResult_AndTheParticipantCanReadIt()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);
        var participant = await RegisterAndLogin("Participant");

        var enrolment = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        await AssertStatus(enrolment, HttpStatusCode.Created);
        var enrolmentId = (await ReadJson(enrolment)).GetProperty("enrolmentId").GetInt32();

        var recorded = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new
        {
            finishTime = "00:42:15",
            finishPosition = 3
        });
        await AssertStatus(recorded, HttpStatusCode.Created);

        var mine = await participant.GetAsync("/api/results/me");
        await AssertStatus(mine, HttpStatusCode.OK);
        var result = (await ReadJson(mine)).EnumerateArray().Single();
        Assert.Equal(3, result.GetProperty("finishPosition").GetInt32());
        Assert.Equal("00:42:15", result.GetProperty("finishTime").GetString());
    }

    [Fact]
    public async Task RecordResult_DuplicateFinishPosition_ReturnsConflict()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);
        var firstId = await Enrol(eventId, categoryId);
        var secondId = await Enrol(eventId, categoryId);

        await AssertStatus(
            await organiser.PostAsJsonAsync($"/api/enrolments/{firstId}/result", new
            {
                finishTime = "00:40:00",
                finishPosition = 1
            }),
            HttpStatusCode.Created);

        var clash = await organiser.PostAsJsonAsync($"/api/enrolments/{secondId}/result", new
        {
            finishTime = "00:41:00",
            finishPosition = 1
        });
        await AssertStatus(clash, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetResult_BelongingToAnotherParticipant_ReturnsForbidden()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);
        var owner = await RegisterAndLogin("Participant");
        var enrolment = await owner.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        await AssertStatus(enrolment, HttpStatusCode.Created);
        var enrolmentId = (await ReadJson(enrolment)).GetProperty("enrolmentId").GetInt32();

        var recorded = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new
        {
            finishTime = "00:50:00",
            finishPosition = 4
        });
        await AssertStatus(recorded, HttpStatusCode.Created);
        var resultId = (await ReadJson(recorded)).GetProperty("resultId").GetInt32();

        var someoneElse = await RegisterAndLogin("Participant");
        var response = await someoneElse.GetAsync($"/api/results/{resultId}");
        await AssertStatus(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCategory_LongerThanTheEvent_ReturnsBadRequest()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var eventId = await CreateBareEvent(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Too long",
            categoryDistanceKm = 21
        });
        await AssertStatus(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateEvent_ShorterThanOneOfItsCategories_ReturnsBadRequest()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var (eventId, _) = await CreateEventWithCategory(organiser);
        var detail = await ReadJson(await organiser.GetAsync($"/api/events/{eventId}"));
        var eventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

        var response = await organiser.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            eventName = "Shortened run",
            description = "The 10 km category no longer fits inside this event.",
            eventDate,
            distanceKm = 5,
            registrationDeadline = eventDate.AddDays(-5),
            eventTypeId = detail.GetProperty("eventTypeId").GetInt32(),
            locationId = detail.GetProperty("locationId").GetInt32()
        });
        await AssertStatus(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Enrol_AfterRegistrationCloses_ReturnsConflict()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var eventId = await CreateEventOn(organiser, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));
        var category = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Open",
            maximumParticipants = 20
        });
        await AssertStatus(category, HttpStatusCode.Created);
        var categoryId = (await ReadJson(category)).GetProperty("categoryId").GetInt32();
        var participant = await RegisterAndLogin("Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        await AssertStatus(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Enrol_WhenTheCategoryIsFull_ReturnsConflict()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var eventId = await CreateBareEvent(organiser);
        var category = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Open",
            maximumParticipants = 1
        });
        await AssertStatus(category, HttpStatusCode.Created);
        var categoryId = (await ReadJson(category)).GetProperty("categoryId").GetInt32();

        await Enrol(eventId, categoryId);
        var second = await RegisterAndLogin("Participant");
        var response = await second.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        await AssertStatus(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Responses_DoNotIncludeThePasswordHash()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var eventId = await CreateBareEvent(organiser);

        var detail = await (await organiser.GetAsync($"/api/events/{eventId}")).Content.ReadAsStringAsync();
        var profile = await (await organiser.GetAsync("/api/users/me")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("pbkdf2", detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pbkdf2", profile, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordResult_AsParticipant_ReturnsForbidden()
    {
        var participant = await RegisterAndLogin("Participant");
        var response = await participant.PostAsJsonAsync("/api/enrolments/1/result", new
        {
            finishTime = "00:42:15",
            finishPosition = 1
        });
        await AssertStatus(response, HttpStatusCode.Forbidden);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });
    }

    private async Task<HttpClient> RegisterAndLogin(string role)
    {
        var client = CreateClient();
        var email = NewEmail(role);
        await AssertStatus(
            await client.PostAsJsonAsync("/api/auth/register", Account(email, role)),
            HttpStatusCode.Created);
        await AssertStatus(
            await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" }),
            HttpStatusCode.OK);
        return client;
    }

    private async Task<int> Enrol(int eventId, int categoryId)
    {
        var participant = await RegisterAndLogin("Participant");
        var enrolment = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        await AssertStatus(enrolment, HttpStatusCode.Created);
        return (await ReadJson(enrolment)).GetProperty("enrolmentId").GetInt32();
    }

    private Task<int> CreateBareEvent(HttpClient organiser)
    {
        return CreateEventOn(organiser, DateOnly.FromDateTime(DateTime.Today.AddDays(30)));
    }

    private async Task<int> CreateEventOn(HttpClient organiser, DateOnly eventDate)
    {
        var typeId = await GetOrCreateRunType(organiser);
        var locationId = await CreateLocation(organiser);

        var created = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventName = "Park run " + Guid.NewGuid().ToString("N")[..8],
            description = "A short event created by the test.",
            eventDate,
            distanceKm = 10,
            registrationDeadline = eventDate.AddDays(-5),
            eventTypeId = typeId,
            locationId
        });

        await AssertStatus(created, HttpStatusCode.Created);
        return (await ReadJson(created)).GetProperty("eventId").GetInt32();
    }

    private async Task<(int EventId, int CategoryId)> CreateEventWithCategory(HttpClient organiser)
    {
        var eventId = await CreateBareEvent(organiser);
        var category = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            categoryName = "Open",
            minimumAge = 18,
            maximumAge = 60,
            categoryDistanceKm = 10,
            maximumParticipants = 100
        });
        await AssertStatus(category, HttpStatusCode.Created);
        return (eventId, (await ReadJson(category)).GetProperty("categoryId").GetInt32());
    }

    private async Task<int> GetOrCreateRunType(HttpClient organiser)
    {
        var list = await organiser.GetAsync("/api/event-types");
        await AssertStatus(list, HttpStatusCode.OK);
        foreach (var item in (await ReadJson(list)).EnumerateArray())
        {
            if (item.GetProperty("typeName").GetString() == "Run")
                return item.GetProperty("eventTypeId").GetInt32();
        }

        var created = await organiser.PostAsJsonAsync("/api/event-types", new
        {
            typeName = "Run",
            description = "Running events"
        });
        await AssertStatus(created, HttpStatusCode.Created);
        return (await ReadJson(created)).GetProperty("eventTypeId").GetInt32();
    }

    private async Task<int> CreateLocation(HttpClient organiser)
    {
        var created = await organiser.PostAsJsonAsync("/api/locations", new
        {
            venueName = "Test Park " + Guid.NewGuid().ToString("N")[..8],
            addressLine = "1 Test Road",
            city = "Pretoria",
            province = "Gauteng",
            postalCode = "0002"
        });
        await AssertStatus(created, HttpStatusCode.Created);
        return (await ReadJson(created)).GetProperty("locationId").GetInt32();
    }

    private async Task<int> ReadLocationId(HttpClient organiser, int eventId)
    {
        var response = await organiser.GetAsync($"/api/events/{eventId}");
        await AssertStatus(response, HttpStatusCode.OK);
        return (await ReadJson(response)).GetProperty("locationId").GetInt32();
    }

    private static object Account(string email, string role) => new
    {
        firstName = "Test",
        lastName = role,
        email,
        password = "Password123!",
        phoneNumber = "0820000000",
        role
    };

    private static string NewEmail(string prefix) =>
        $"{prefix}.{Guid.NewGuid():N}@raceday.test";

    private static async Task AssertStatus(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode == expected)
            return;

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail($"Expected {(int)expected} but got {(int)response.StatusCode}. Body: {body}");
    }

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }
}
