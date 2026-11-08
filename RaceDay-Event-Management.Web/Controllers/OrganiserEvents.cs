using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay_Event_Management.API.Contracts;
using RaceDay.Web.Models;
using RaceDay.Web.Security;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public partial class OrganiserController
{
    public async Task<IActionResult> Create()
    {
        var page = new EventFormPage { EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)), RegistrationDeadline = DateOnly.FromDateTime(DateTime.Today.AddDays(25)), DistanceKm = 5 };
        await LoadChoices(page);
        return View("EventForm", page);
    }
    [HttpPost]
    public async Task<IActionResult> Create(EventFormPage model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var created = (await api.SendAsync<EventResponse>(HttpMethod.Post, "api/events", model))!;
                if (!await SaveBanner(created.EventId, model.BannerFile)) return RedirectToAction(nameof(Edit), new { id = created.EventId });
                TempData["Success"] = "Event created. Add your race categories next.";
                return RedirectToAction(nameof(Categories), new { id = created.EventId });
            }
            catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict) { ModelState.AddModelError("", ex.Message); }
        }
        await LoadChoices(model);
        return View("EventForm", model);
    }
    public async Task<IActionResult> Edit(int id)
    {
        var race = await OwnedEvent(id);
        var page = new EventFormPage { EventId = id, EventName = race.EventName, Description = race.Description, EventDate = race.EventDate, RegistrationDeadline = race.RegistrationDeadline, EventTypeId = race.EventTypeId, LocationId = race.LocationId, DistanceKm = race.DistanceKm };
        await LoadChoices(page);
        return View("EventForm", page);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormPage model)
    {
        model.EventId = id;
        await OwnedEvent(id);
        if (ModelState.IsValid)
        {
            try
            {
                await api.SendAsync<EventResponse>(HttpMethod.Put, $"api/events/{id}", model);
                if (!await SaveBanner(id, model.BannerFile)) return RedirectToAction(nameof(Edit), new { id });
                TempData["Success"] = "Event details saved.";
                return RedirectToAction(nameof(Events));
            }
            catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict) { ModelState.AddModelError("", ex.Message); }
        }
        await LoadChoices(model);
        return View("EventForm", model);
    }
    public async Task<IActionResult> Delete(int id) => View(await OwnedEvent(id));
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await api.SendAsync<ApiMessage>(HttpMethod.Delete, $"api/events/{id}");
            TempData["Success"] = "The event has been deleted.";
            return RedirectToAction(nameof(Events));
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        { ModelState.AddModelError("", ex.Message); return View("Delete", await OwnedEvent(id)); }
    }
    public IActionResult NewLocation() => View(new LocationWriteRequest());
    [HttpPost]
    public async Task<IActionResult> NewLocation(LocationWriteRequest model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await api.SendAsync<LocationResponse>(HttpMethod.Post, "api/locations", model);
            TempData["Success"] = "Venue added. You can now select it when creating or editing an event.";
            return RedirectToAction(nameof(Events));
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest) { ModelState.AddModelError("", ex.Message); return View(model); }
    }
    [HttpPost]
    public async Task<IActionResult> DeleteImage(int id, int imageId)
    {
        await OwnedEvent(id);
        var images = await api.GetAsync<List<EventImageResponse>>($"api/events/{id}/images");
        if (!images.Any(image => image.EventImageId == imageId)) return NotFound();
        await api.SendAsync<ApiMessage>(HttpMethod.Delete, $"api/event-images/{imageId}");
        TempData["Success"] = "Banner image removed.";
        return RedirectToAction(nameof(Edit), new { id });
    }
    private async Task<bool> SaveBanner(int id, IFormFile? file)
    {
        if (file is null) return true;
        try { await api.UploadAsync<EventImageResponse>($"api/events/{id}/images", file); return true; }
        catch (ApiException ex) when (ex.StatusCode != HttpStatusCode.Unauthorized)
        { TempData["Error"] = "The event was saved, but the banner could not be uploaded. Use a JPG, PNG, GIF or WebP up to 5 MB and try again."; return false; }
        catch (HttpRequestException)
        { TempData["Error"] = "The event was saved, but image storage is unavailable. You can upload a banner later."; return false; }
    }
    private async Task LoadChoices(EventFormPage page)
    {
        page.Locations = await api.GetAsync<List<LocationResponse>>("api/locations");
        page.Types = await api.GetAsync<List<EventTypeResponse>>("api/event-types");
        if (page.EventId > 0) page.Images = await api.GetAsync<List<EventImageResponse>>($"api/events/{page.EventId}/images");
    }
    private async Task<EventDetailResponse> OwnedEvent(int id)
    {
        var race = await api.GetAsync<EventDetailResponse>($"api/events/{id}");
        if (race.OrganiserId != HttpContext.Session.GetInt32(WebSession.UserId)) throw new ApiException(HttpStatusCode.Forbidden, "This event belongs to another organiser.");
        return race;
    }
}
