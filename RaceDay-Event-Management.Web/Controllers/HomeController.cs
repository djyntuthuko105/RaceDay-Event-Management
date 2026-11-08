using Microsoft.AspNetCore.Mvc;
namespace RaceDay.Web.Controllers;
public class HomeController : Controller
{
    public IActionResult Error() => View("Message", "We could not complete that request. Please try again shortly.");
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return View("Message", code == 404 ? "That page could not be found." : "This request could not be completed.");
    }
}
