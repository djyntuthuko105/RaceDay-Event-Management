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
    public async Task<IActionResult> Picture()
    {
        var file = await api.DownloadAsync("api/users/me/picture");
        return File(file.Bytes, file.ContentType);
    }
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
            if (model.PictureFile is not null)
            {
                try { await api.UploadAsync<UserResponse>("api/users/me/picture", model.PictureFile); }
                catch (ApiException ex) when ((int)ex.StatusCode >= 400 && ex.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                { TempData["Error"] = "Your details were saved, but your picture could not be uploaded. Choose a supported image up to 5 MB and retry."; }
            }
            TempData["Success"] = "Your profile details have been updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        { ModelState.AddModelError("", ex.Message); return View(model); }
    }
    private static ProfilePage ToPage(UserResponse user) => new() { FirstName = user.FirstName, LastName = user.LastName, Email = user.Email, Role = user.Role, PhoneNumber = user.PhoneNumber, ProfilePictureUrl = user.ProfilePictureUrl };
}
