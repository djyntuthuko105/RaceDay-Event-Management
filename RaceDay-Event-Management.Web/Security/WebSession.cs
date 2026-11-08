using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace RaceDay.Web.Security;
public static class WebSession
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string Name = "DisplayName";
}
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute(string? role = null) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Session.GetInt32(WebSession.UserId) is null)
            context.Result = new RedirectToActionResult("Login", "Account", null);
        else if (role is not null && context.HttpContext.Session.GetString(WebSession.Role) != role)
            context.Result = new StatusCodeResult(403);
    }
}
