using Microsoft.AspNetCore.Mvc;
using RaceDay_Event_Management.API.Contracts;
using RaceDay.Web.Models;
using RaceDay.Web.Security;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
[RequireRole]
public class ProfileController(RaceDayApi api) : Controller
{
    public async Task<IActionResult> Index() => View(ToPage(await api.GetAsync<UserResponse>("api/users/me")));
    [HttpPost]
    public async Task<IActionResult> Index(ProfilePage model)
    {
        var current = await api.GetAsync<UserResponse>("api/users/me");
        model.Email = current.Email;
        model.Role = current.Role;
        model.ProfilePictureUrl = current.ProfilePictureUrl;
        if (!ModelState.IsValid) return View(model);
        try
        {
            var user = (await api.SendAsync<UserResponse>(HttpMethod.Put, "api/users/me", new UpdateProfileRequest { FirstName = model.FirstName, LastName = model.LastName, PhoneNumber = model.PhoneNumber, ProfilePictureUrl = current.ProfilePictureUrl }))!;
            HttpContext.Session.SetString(WebSession.Name, user.FirstName);
            TempData["Success"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        { ModelState.AddModelError("", ex.Message); return View(model); }
    }
    private static ProfilePage ToPage(UserResponse user) => new() { FirstName = user.FirstName, LastName = user.LastName, Email = user.Email, Role = user.Role, PhoneNumber = user.PhoneNumber, ProfilePictureUrl = user.ProfilePictureUrl };
}
