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
}
