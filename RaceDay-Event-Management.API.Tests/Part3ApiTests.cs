using System.Net;
using System.Net.Http.Json;
using Xunit;
namespace RaceDay_Event_Management.API.Tests;
public partial class RaceDayApiTests
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/L9sAAAAASUVORK5CYII=");
    private static MultipartFormDataContent Image(byte[] bytes, string name = "banner.png")
    {
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(bytes), "File", name);
        return content;
    }
    [Fact]
    public async Task ManagedEvents_RequiresOrganiser()
    {
        await AssertStatus(await CreateClient().GetAsync("/api/events/managed"), HttpStatusCode.Unauthorized);
        var participant = await RegisterAndLogin("Participant");
        await AssertStatus(await participant.GetAsync("/api/events/managed"), HttpStatusCode.Forbidden);
    }
    [Fact]
    public async Task ManagedEvents_IncludesOwnPastEventsAndExcludesOtherOrganisers()
    {
        var first = await RegisterAndLogin("Organiser");
        var second = await RegisterAndLogin("Organiser");
        var mine = await CreateBareEvent(first);
        var other = await CreateBareEvent(second);
        await FinishEvent(first, mine);
        var response = await first.GetAsync("/api/events/managed");
        await AssertStatus(response, HttpStatusCode.OK);
        var events = (await ReadJson(response)).EnumerateArray().ToList();
        Assert.Single(events);
        Assert.Equal(mine, events[0].GetProperty("event").GetProperty("eventId").GetInt32());
        Assert.NotEqual(other, mine);
    }
    [Fact]
    public async Task Results_BeforeRaceDay_AreRejected()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);
        var enrolmentId = await Enrol(eventId, categoryId);
        await AssertStatus(await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new { finishTime = "00:45:00", finishPosition = 1 }), HttpStatusCode.Conflict);
    }
    [Fact]
    public async Task Results_HistoryIncludesEventDateAndRecordedFinisherCount()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategory(organiser);
        var participant = await RegisterAndLogin("Participant");
        var entry = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var firstId = (await ReadJson(entry)).GetProperty("enrolmentId").GetInt32();
        var secondId = await Enrol(eventId, categoryId);
        await FinishEvent(organiser, eventId);
        await AssertStatus(await organiser.PostAsJsonAsync($"/api/enrolments/{firstId}/result", new { finishTime = "00:40:00", finishPosition = 1 }), HttpStatusCode.Created);
        await AssertStatus(await organiser.PostAsJsonAsync($"/api/enrolments/{secondId}/result", new { finishTime = "00:45:00", finishPosition = 2 }), HttpStatusCode.Created);
        var result = (await ReadJson(await participant.GetAsync("/api/results/me"))).EnumerateArray().Single();
        Assert.Equal(2, result.GetProperty("totalFinishers").GetInt32());
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today.AddDays(-1)).ToString("yyyy-MM-dd"), result.GetProperty("eventDate").GetString());
    }
    [Fact]
    public async Task Banner_UploadPersistsUrlAndServesTheSameImage()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var eventId = await CreateBareEvent(organiser);
        using var upload = Image(Png);
        var response = await organiser.PostAsync($"/api/events/{eventId}/images", upload);
        await AssertStatus(response, HttpStatusCode.Created);
        var image = await ReadJson(response);
        var imageId = image.GetProperty("eventImageId").GetInt32();
        Assert.StartsWith("/event-images/events/", image.GetProperty("imageUrl").GetString());
        var served = await CreateClient().GetAsync($"/api/event-images/{imageId}/content");
        Assert.Equal(Png, await served.Content.ReadAsByteArrayAsync());
        await AssertStatus(await organiser.DeleteAsync($"/api/event-images/{imageId}"), HttpStatusCode.NoContent);
        await AssertStatus(await CreateClient().GetAsync($"/api/event-images/{imageId}/content"), HttpStatusCode.NotFound);
    }
    [Fact]
    public async Task Banner_RejectsFakeImagesAndAnotherOrganiser()
    {
        var organiser = await RegisterAndLogin("Organiser");
        var eventId = await CreateBareEvent(organiser);
        using var fake = Image("not an image"u8.ToArray());
        await AssertStatus(await organiser.PostAsync($"/api/events/{eventId}/images", fake), HttpStatusCode.BadRequest);
        var other = await RegisterAndLogin("Organiser");
        using var valid = Image(Png);
        await AssertStatus(await other.PostAsync($"/api/events/{eventId}/images", valid), HttpStatusCode.Forbidden);
    }
    [Fact]
    public async Task PictureUpload_UpdatesOnlyTheCurrentProfile()
    {
        var participant = await RegisterAndLogin("Participant");
        using var upload = Image(Png, "profile.png");
        var response = await participant.PostAsync("/api/users/me/picture", upload);
        await AssertStatus(response, HttpStatusCode.OK);
        Assert.StartsWith("/event-images/profiles/", (await ReadJson(response)).GetProperty("profilePictureUrl").GetString());
        Assert.Equal(Png, await participant.GetByteArrayAsync("/api/users/me/picture"));
        var other = await RegisterAndLogin("Participant");
        await AssertStatus(await other.GetAsync("/api/users/me/picture"), HttpStatusCode.NotFound);
        await AssertStatus(await CreateClient().GetAsync("/api/users/me/picture"), HttpStatusCode.Unauthorized);
    }
    [Fact]
    public async Task PictureUpload_RejectsOversizeAndUnsupportedFormats()
    {
        var participant = await RegisterAndLogin("Participant");
        using var oversize = Image(new byte[5 * 1024 * 1024 + 1]);
        await AssertStatus(await participant.PostAsync("/api/users/me/picture", oversize), HttpStatusCode.BadRequest);
        using var svg = Image("<svg/>"u8.ToArray(), "profile.svg");
        await AssertStatus(await participant.PostAsync("/api/users/me/picture", svg), HttpStatusCode.BadRequest);
    }
    [Fact]
    public async Task ProfileUpdate_CannotInjectArbitraryStorageUrls()
    {
        var participant = await RegisterAndLogin("Participant");
        var response = await participant.PutAsJsonAsync("/api/users/me", new { firstName = "Test", lastName = "Participant", profilePictureUrl = "https://example.test/not-an-upload.png" });
        await AssertStatus(response, HttpStatusCode.OK);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await ReadJson(response)).GetProperty("profilePictureUrl").ValueKind);
    }
}
