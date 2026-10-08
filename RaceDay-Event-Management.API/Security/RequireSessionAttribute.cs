using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay_Event_Management.API.Contracts;

namespace RaceDay_Event_Management.API.Security;

/// <summary>
/// Blocks the action unless the caller has a server-side session.
/// Pass a role when the action belongs to Organiser or Participant only.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireSessionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _roles;

    public RequireSessionAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        await session.LoadAsync();

        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);

        if (userId is null || string.IsNullOrWhiteSpace(role))
        {
            context.Result = new UnauthorizedObjectResult(
                new ApiMessage("You need to log in first."));
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(role))
        {
            context.Result = new ObjectResult(
                new ApiMessage("You do not have permission to do that."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}

public static class SessionReader
{
    public static int UserId(HttpContext httpContext)
    {
        return httpContext.Session.GetInt32(SessionKeys.UserId)
            ?? throw new InvalidOperationException("The session does not contain a user id.");
    }

    public static string Role(HttpContext httpContext)
    {
        return httpContext.Session.GetString(SessionKeys.Role)
            ?? throw new InvalidOperationException("The session does not contain a role.");
    }
}
