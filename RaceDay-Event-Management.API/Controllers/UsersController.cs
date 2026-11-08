using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Mapping;
using RaceDay_Event_Management.API.Models;
using RaceDay_Event_Management.API.Security;

namespace RaceDay_Event_Management.API.Controllers;

[ApiController]
[Route("api/users")]
[Tags("User Profile")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the profile of the logged-in user.
    /// </summary>
    /// <response code="200">The profile was found.</response>
    /// <response code="401">The caller is not logged in.</response>
    [HttpGet("me")]
    [RequireSession]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var user = await FindCurrentUser(cancellationToken);
        if (user is null)
            return Unauthorized(new ApiMessage("You need to log in first."));

        return Ok(Responses.User(user));
    }

    /// <summary>
    /// Updates the logged-in user's name, phone number, and profile picture url.
    /// </summary>
    /// <response code="200">The profile was updated.</response>
    /// <response code="400">The new details are not valid.</response>
    /// <response code="401">The caller is not logged in.</response>
    [HttpPut("me")]
    [RequireSession]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMine(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await FindCurrentUser(cancellationToken);
        if (user is null)
            return Unauthorized(new ApiMessage("You need to log in first."));

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.ProfilePictureUrl = string.IsNullOrWhiteSpace(request.ProfilePictureUrl)
            ? null
            : request.ProfilePictureUrl.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(Responses.User(user));
    }

    /// <summary>
    /// Returns another user's profile. Only an organiser can call this.
    /// </summary>
    /// <response code="200">The profile was found.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The caller is not an organiser.</response>
    /// <response code="404">No user exists with that id.</response>
    [HttpGet("{id:int}")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FindAsync(new object[] { id }, cancellationToken);
        if (user is null)
            return NotFound(new ApiMessage("User does not exist."));

        return Ok(Responses.User(user));
    }

    [HttpPost("me/picture")]
    [RequireSession]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadPicture([FromForm] ImageUploadRequest request,
        [FromServices] RaceDay_Event_Management.API.Storage.EventImageStorage storage, CancellationToken cancellationToken)
    {
        var user = await FindCurrentUser(cancellationToken);
        if (user is null) return Unauthorized();
        if (request.File is null) return BadRequest(new ApiMessage("Choose a profile image."));
        try
        {
            var image = await storage.SaveImageAsync($"profiles/{user.UserId}", request.File, cancellationToken);
            user.ProfilePictureUrl = image.Url;
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(Responses.User(user));
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiMessage(ex.Message)); }
    }

    [HttpGet("me/picture")]
    [RequireSession]
    public async Task<IActionResult> Picture([FromServices] RaceDay_Event_Management.API.Storage.EventImageStorage storage, CancellationToken cancellationToken)
    {
        var user = await FindCurrentUser(cancellationToken);
        if (user?.ProfilePictureUrl is null) return NotFound();
        var stream = await storage.OpenAsync(user.ProfilePictureUrl, cancellationToken);
        if (stream is null) return NotFound();
        var type = Path.GetExtension(new Uri(new Uri("http://localhost"), user.ProfilePictureUrl).AbsolutePath).ToLowerInvariant() switch
        { ".jpg" or ".jpeg" => "image/jpeg", ".gif" => "image/gif", ".webp" => "image/webp", _ => "image/png" };
        return File(stream, type);
    }

    private Task<User?> FindCurrentUser(CancellationToken cancellationToken)
    {
        var userId = SessionReader.UserId(HttpContext);
        return _db.Users.FirstOrDefaultAsync(user => user.UserId == userId, cancellationToken);
    }
}
