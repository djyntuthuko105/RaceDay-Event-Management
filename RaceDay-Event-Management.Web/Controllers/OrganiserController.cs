using Microsoft.AspNetCore.Mvc;
using RaceDay_Event_Management.API.Contracts;
using RaceDay.Web.Security;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
[RequireRole("Organiser")]
public partial class OrganiserController(RaceDayApi api) : Controller
{
    public async Task<IActionResult> Index() => View(await api.GetAsync<List<OrganiserEventResponse>>("api/events/managed"));
    public async Task<IActionResult> Events() => View(await api.GetAsync<List<OrganiserEventResponse>>("api/events/managed"));
}
