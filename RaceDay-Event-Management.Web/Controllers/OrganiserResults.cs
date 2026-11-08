using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay_Event_Management.API.Contracts;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public partial class OrganiserController
{
    public async Task<IActionResult> Enrolments(int id) => View(await EntryPage(id));
    [HttpPost]
    public async Task<IActionResult> SaveResult(int id, int enrolmentId, int? resultId, ResultWriteRequest model)
    {
        var page = await EntryPage(id);
        if (!page.Enrolments.Any(entry => entry.EnrolmentId == enrolmentId)) return NotFound();
        if (resultId is not null && !page.Results.Any(result => result.ResultId == resultId && result.EnrolmentId == enrolmentId)) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                await api.SendAsync<ResultResponse>(resultId is null ? HttpMethod.Post : HttpMethod.Put,
                    resultId is null ? $"api/enrolments/{enrolmentId}/result" : $"api/results/{resultId}", model);
                TempData["Success"] = "Result published. The participant can now see it in My results.";
                return RedirectToAction(nameof(Enrolments), new { id });
            }
            catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict) { ModelState.AddModelError("", ex.Message); }
        }
        ViewData["ErrorEnrolmentId"] = enrolmentId;
        ViewData["AttemptedTime"] = model.FinishTime.ToString("HH:mm:ss");
        ViewData["AttemptedPosition"] = model.FinishPosition;
        return View("Enrolments", page);
    }
    private async Task<EnrolmentPage> EntryPage(int id) => new()
    {
        Event = await OwnedEvent(id),
        Enrolments = await api.GetAsync<List<EnrolmentResponse>>($"api/events/{id}/enrolments"),
        Results = await api.GetAsync<List<ResultResponse>>($"api/events/{id}/results")
    };
}
