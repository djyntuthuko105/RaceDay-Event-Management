using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay_Event_Management.API.Contracts;
using RaceDay.Web.Models;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public partial class OrganiserController
{
    public async Task<IActionResult> Categories(int id, int? categoryId)
    {
        var race = await OwnedEvent(id);
        var page = new CategoryPage { Event = race };
        if (categoryId is not null)
        {
            var category = race.Categories.FirstOrDefault(item => item.CategoryId == categoryId);
            if (category is null) return NotFound();
            page.CategoryId = category.CategoryId; page.CategoryName = category.CategoryName;
            page.MinimumAge = category.MinimumAge; page.MaximumAge = category.MaximumAge;
            page.CategoryDistanceKm = category.CategoryDistanceKm; page.MaximumParticipants = category.MaximumParticipants;
        }
        return View(page);
    }
    [HttpPost]
    public async Task<IActionResult> Categories(int id, int? categoryId, CategoryWriteRequest model)
    {
        var race = await OwnedEvent(id);
        if (categoryId is not null && !race.Categories.Any(item => item.CategoryId == categoryId)) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                var path = categoryId is null ? $"api/events/{id}/categories" : $"api/categories/{categoryId}";
                await api.SendAsync<CategoryResponse>(categoryId is null ? HttpMethod.Post : HttpMethod.Put, path, model);
                TempData["Success"] = "Race category saved.";
                return RedirectToAction(nameof(Categories), new { id });
            }
            catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict) { ModelState.AddModelError("", ex.Message); }
        }
        return View(new CategoryPage { Event = race, CategoryId = categoryId ?? 0, CategoryName = model.CategoryName, MinimumAge = model.MinimumAge, MaximumAge = model.MaximumAge, CategoryDistanceKm = model.CategoryDistanceKm, MaximumParticipants = model.MaximumParticipants });
    }
    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, int categoryId)
    {
        var race = await OwnedEvent(id);
        if (!race.Categories.Any(item => item.CategoryId == categoryId)) return NotFound();
        try { await api.SendAsync<ApiMessage>(HttpMethod.Delete, $"api/categories/{categoryId}"); TempData["Success"] = "Category deleted."; }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Categories), new { id });
    }
}
