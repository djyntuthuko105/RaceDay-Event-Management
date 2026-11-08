using Microsoft.AspNetCore.Mvc;
using RaceDay_Event_Management.API.Contracts;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public class EventsController(RaceDayApi api) : Controller
{
    public async Task<IActionResult> Index(string? search, int? typeId, string? city)
    {
        var events = await api.GetAsync<List<EventResponse>>("api/events");
        // These filters only shape the display; enrolment rules remain in the API.
        if (!string.IsNullOrWhiteSpace(search)) events = events.Where(e => e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        if (typeId is not null) events = events.Where(e => e.EventTypeId == typeId).ToList();
        if (!string.IsNullOrWhiteSpace(city)) events = events.Where(e => e.City.Contains(city, StringComparison.OrdinalIgnoreCase)).ToList();
        return View(new BrowsePage { Events = events, Types = await api.GetAsync<List<EventTypeResponse>>("api/event-types"), Search = search, TypeId = typeId, City = city });
    }
    public async Task<IActionResult> Details(int id) => View(new EventDetailsPage { Event = await api.GetAsync<EventDetailResponse>($"api/events/{id}") });
    [HttpPost, RaceDay.Web.Security.RequireRole("Participant")]
    public async Task<IActionResult> Enter(int id, CreateEnrolmentRequest model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await api.SendAsync<EnrolmentResponse>(HttpMethod.Post, $"api/events/{id}/enrolments", model);
                TempData["Success"] = "You're in! Your entry is confirmed in My events.";
                return RedirectToAction("Events", "Participant");
            }
            catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Conflict)
            { ModelState.AddModelError("", ex.Message); }
        }
        return View("Details", new EventDetailsPage { Event = await api.GetAsync<EventDetailResponse>($"api/events/{id}"), CategoryId = model.CategoryId });
    }
}
