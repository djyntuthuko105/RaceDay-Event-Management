using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Mapping;
using RaceDay_Event_Management.API.Models;
using RaceDay_Event_Management.API.Security;
using RaceDay_Event_Management.API.Storage;

namespace RaceDay_Event_Management.API.Controllers;

[ApiController]
[Tags("Event Images")]
public class EventImagesController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly EventImageStorage _storage;

    public EventImagesController(RaceDayDbContext db, EventImageStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    /// <summary>
    /// Lists the images stored for an event.
    /// </summary>
    /// <response code="200">The images were returned.</response>
    /// <response code="404">No event exists with that id.</response>
    [HttpGet("api/events/{eventId:int}/images")]
    [ProducesResponseType(typeof(IEnumerable<EventImageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(int eventId, CancellationToken cancellationToken)
    {
        var eventExists = await _db.Events.AnyAsync(race => race.EventId == eventId, cancellationToken);
        if (!eventExists)
            return NotFound(new ApiMessage("Event does not exist."));

        var images = await _db.EventImages
            .AsNoTracking()
            .Where(image => image.EventId == eventId)
            .OrderByDescending(image => image.UploadedAt)
            .ToListAsync(cancellationToken);

        return Ok(images.Select(Responses.EventImage));
    }

    /// <summary>
    /// Stores an image for an event the organiser owns and keeps the public url.
    /// </summary>
    /// <response code="201">The image was saved.</response>
    /// <response code="400">The file is missing or is not an allowed image.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No event exists with that id.</response>
    [HttpPost("api/events/{eventId:int}/images")]
    [RequireSession(Roles.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventImageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upload(int eventId, [FromForm] ImageUploadRequest? request, CancellationToken cancellationToken)
    {
        var race = await _db.Events.FirstOrDefaultAsync(item => item.EventId == eventId, cancellationToken);
        if (race is null)
            return NotFound(new ApiMessage("Event does not exist."));

        if (race.OrganiserId != SessionReader.UserId(HttpContext))
            return StatusCode(StatusCodes.Status403Forbidden, new ApiMessage("You can only add images to events you organised."));

        var file = request?.File;
        if (file is null)
            return BadRequest(new ApiMessage("Choose an image file to upload."));

        string url;
        string imageType;
        try
        {
            (url, imageType) = await _storage.SaveAsync(eventId, file, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiMessage(ex.Message));
        }

        var image = new EventImage
        {
            EventId = eventId,
            ImageUrl = url,
            ImageType = imageType,
            UploadedAt = DateTime.Now
        };

        _db.EventImages.Add(image);
        await _db.SaveChangesAsync(cancellationToken);
        return Created($"/api/events/{eventId}/images", Responses.EventImage(image));
    }

    /// <summary>
    /// Removes an image from an event the organiser owns.
    /// </summary>
    /// <response code="204">The image was removed.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No image exists with that id.</response>
    [HttpDelete("api/event-images/{id:int}")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var image = await _db.EventImages
            .Include(item => item.Event)
            .FirstOrDefaultAsync(item => item.EventImageId == id, cancellationToken);

        if (image is null)
            return NotFound(new ApiMessage("Image does not exist."));

        if (image.Event.OrganiserId != SessionReader.UserId(HttpContext))
            return StatusCode(StatusCodes.Status403Forbidden, new ApiMessage("You can only remove images from events you organised."));

        _storage.Delete(image.ImageUrl);
        _db.EventImages.Remove(image);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
