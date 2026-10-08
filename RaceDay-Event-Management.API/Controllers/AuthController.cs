using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Mapping;
using RaceDay_Event_Management.API.Models;
using RaceDay_Event_Management.API.Security;

namespace RaceDay_Event_Management.API.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public AuthController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Creates an account. The caller chooses Organiser or Participant, and the password is stored as a hash.
    /// </summary>
    /// <response code="201">The account was created.</response>
    /// <response code="400">The details are missing or the role is not recognised.</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!Roles.IsKnown(request.Role))
            return BadRequest(new ApiMessage("Role must be Organiser or Participant."));

        var email = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await _db.Users.AnyAsync(user => user.Email == email, cancellationToken);
        if (emailTaken)
            return Conflict(new ApiMessage("An account with this email already exists."));

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            PhoneNumber = Clean(request.PhoneNumber),
            Role = request.Role,
            CreatedAt = DateTime.Now
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return Created($"/api/users/{user.UserId}", Responses.User(user));
    }

    /// <summary>
    /// Checks the email and password, then stores the user id and role in the server session.
    /// </summary>
    /// <response code="200">Login succeeded. Later calls on this browser send the session cookie.</response>
    /// <response code="401">The email or password is wrong.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(item => item.Email == email, cancellationToken);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new ApiMessage("The email or password is incorrect."));

        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(Responses.User(user));
    }

    /// <summary>
    /// Ends the current session.
    /// </summary>
    /// <response code="200">The session was cleared.</response>
    /// <response code="401">There is no active session.</response>
    [HttpPost("logout")]
    [RequireSession]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new ApiMessage("You have been logged out."));
    }

    /// <summary>
    /// Returns the user id and role kept in the current session.
    /// </summary>
    /// <response code="200">The caller is logged in.</response>
    /// <response code="401">There is no active session.</response>
    [HttpGet("session")]
    [RequireSession]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Session(CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(new object[] { SessionReader.UserId(HttpContext) }, cancellationToken);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return Unauthorized(new ApiMessage("You need to log in first."));
        }

        return Ok(Responses.User(user));
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
