using Microsoft.AspNetCore.Mvc;
using RaceDay_Event_Management.API.Contracts;
using RaceDay.Web.Security;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
[RequireRole("Participant")]
public class ParticipantController(RaceDayApi api) : Controller
{
    public async Task<IActionResult> Events() => View(await api.GetAsync<List<EnrolmentResponse>>("api/enrolments/me"));
    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            await api.SendAsync<ApiMessage>(HttpMethod.Delete, $"api/enrolments/{id}");
            TempData["Success"] = "Your entry has been cancelled.";
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Events));
    }
}
