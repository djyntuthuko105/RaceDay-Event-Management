using Microsoft.AspNetCore.Mvc;
using RaceDay_Event_Management.API.Contracts;
using RaceDay.Web.Security;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public class AccountController(RaceDayApi api) : Controller
{
    [HttpGet]
    public IActionResult Login(bool expired = false)
    {
        if (expired) ViewData["Notice"] = "Your session ended. Please log in again.";
        return View(new LoginRequest());
    }
    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            HttpContext.Session.Clear();
            var user = (await api.SendAsync<UserResponse>(HttpMethod.Post, "api/auth/login", model))!;
            HttpContext.Session.SetInt32(WebSession.UserId, user.UserId);
            HttpContext.Session.SetString(WebSession.Role, user.Role);
            HttpContext.Session.SetString(WebSession.Name, user.FirstName);
            return user.Role == "Organiser" ? RedirectToAction("Index", "Organiser") : RedirectToAction("Index", "Events");
        }
        catch (ApiException ex) when ((int)ex.StatusCode < 500)
        {
            ModelState.AddModelError("", "We could not log you in. Check your email and password.");
            return View(model);
        }
    }
    [HttpGet]
    public IActionResult Register() => View(new RegisterRequest { Role = "Participant" });
    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            await api.SendAsync<UserResponse>(HttpMethod.Post, "api/auth/register", model);
            TempData["Success"] = "Your account is ready. Log in to get started.";
            return RedirectToAction(nameof(Login));
        }
        catch (ApiException ex) when ((int)ex.StatusCode < 500)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }
    [HttpPost, RequireRole]
    public async Task<IActionResult> Logout()
    {
        try { await api.SendAsync<ApiMessage>(HttpMethod.Post, "api/auth/logout"); }
        finally { HttpContext.Session.Clear(); }
        return RedirectToAction(nameof(Login));
    }
}
