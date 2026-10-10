using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Mapping;
using RaceDay_Event_Management.API.Models;
using RaceDay_Event_Management.API.Security;

namespace RaceDay_Event_Management.API.Controllers;

[ApiController]
[Route("api/events")]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lists events that have not taken place yet. Anyone can call this.
    /// </summary>
    /// <response code="200">The upcoming events were returned.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var events = await Details()
            .Where(race => race.EventDate >= today)
            .OrderBy(race => race.EventDate)
            .ToListAsync(cancellationToken);

        return Ok(events.Select(Responses.Event));
    }

    /// <summary>
    /// Returns one event with its type, venue, organiser, and categories.
    /// </summary>
    /// <response code="200">The event was found.</response>
    /// <response code="404">No event exists with that id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var race = await Details()
            .Include(item => item.Categories)
            .FirstOrDefaultAsync(item => item.EventId == id, cancellationToken);
        if (race is null)
            return NotFound(new ApiMessage("Event does not exist."));

        return Ok(Responses.EventDetail(race));
    }

    /// <summary>
    /// Creates an event for the logged-in organiser.
    /// </summary>
    /// <response code="201">The event was created.</response>
    /// <response code="400">The event details are not valid.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The caller is not an organiser.</response>
    [HttpPost]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventWriteRequest request, CancellationToken cancellationToken)
    {
        var problem = await ValidateWrite(request, cancellationToken);
        if (problem is not null)
            return problem;

        var organiserId = SessionReader.UserId(HttpContext);
        var race = new Event
        {
            OrganiserId = organiserId,
            EventTypeId = request.EventTypeId,
            LocationId = request.LocationId,
            EventName = request.EventName.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate,
            DistanceKm = request.DistanceKm,
            RegistrationDeadline = request.RegistrationDeadline,
            CreatedAt = DateTime.Now
        };

        _db.Events.Add(race);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await Details().FirstAsync(item => item.EventId == race.EventId, cancellationToken);
        return Created($"/api/events/{created.EventId}", Responses.Event(created));
    }

    /// <summary>
    /// Updates an event. The organiser can only change events they created.
    /// </summary>
    /// <response code="200">The event was updated.</response>
    /// <response code="400">The new details are not valid, or the event would be shorter than one of its categories.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No event exists with that id.</response>
    [HttpPut("{id:int}")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventWriteRequest request, CancellationToken cancellationToken)
    {
        var owned = await FindOwned(id, cancellationToken);
        if (owned.Error is not null)
            return owned.Error;

        var problem = await ValidateWrite(request, cancellationToken);
        if (problem is not null)
            return problem;

        var categoryDistances = await _db.Categories
            .Where(category => category.EventId == id && category.CategoryDistanceKm != null)
            .Select(category => category.CategoryDistanceKm!.Value)
            .ToListAsync(cancellationToken);
        if (categoryDistances.Any(distance => distance > request.DistanceKm))
            return BadRequest(new ApiMessage("The event distance cannot be shorter than one of its categories."));

        var race = owned.Event!;
        race.EventTypeId = request.EventTypeId;
        race.LocationId = request.LocationId;
        race.EventName = request.EventName.Trim();
        race.Description = request.Description.Trim();
        race.EventDate = request.EventDate;
        race.DistanceKm = request.DistanceKm;
        race.RegistrationDeadline = request.RegistrationDeadline;

        await _db.SaveChangesAsync(cancellationToken);

        var updated = await Details().FirstAsync(item => item.EventId == race.EventId, cancellationToken);
        return Ok(Responses.Event(updated));
    }

    /// <summary>
    /// Deletes an event that has no categories, enrolments, images, or weather snapshots.
    /// </summary>
    /// <response code="204">The event was deleted.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No event exists with that id.</response>
    /// <response code="409">The event still has related records.</response>
    [HttpDelete("{id:int}")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var owned = await FindOwned(id, cancellationToken);
        if (owned.Error is not null)
            return owned.Error;

        var inUse = await _db.Categories.AnyAsync(category => category.EventId == id, cancellationToken)
            || await _db.Enrolments.AnyAsync(enrolment => enrolment.EventId == id, cancellationToken)
            || await _db.EventImages.AnyAsync(image => image.EventId == id, cancellationToken)
            || await _db.WeatherSnapshots.AnyAsync(snapshot => snapshot.EventId == id, cancellationToken);

        if (inUse)
            return Conflict(new ApiMessage("Remove the event's categories, enrolments, images, and weather before deleting it."));

        _db.Events.Remove(owned.Event!);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lists everyone entered in an event. Only the organiser who owns the event can call this.
    /// </summary>
    /// <response code="200">The enrolment list was returned.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No event exists with that id.</response>
    [HttpGet("{id:int}/enrolments")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Enrolments(int id, CancellationToken cancellationToken)
    {
        var owned = await FindOwned(id, cancellationToken);
        if (owned.Error is not null)
            return owned.Error;

        var enrolments = await _db.Enrolments
            .AsNoTracking()
            .Include(enrolment => enrolment.Participant)
            .Include(enrolment => enrolment.Event)
            .Include(enrolment => enrolment.Category)
            .Where(enrolment => enrolment.EventId == id)
            .OrderBy(enrolment => enrolment.EnrolmentDate)
            .ToListAsync(cancellationToken);

        return Ok(enrolments.Select(Responses.Enrolment));
    }

    private IQueryable<Event> Details()
    {
        return _db.Events
            .AsNoTracking()
            .Include(race => race.Organiser)
            .Include(race => race.EventType)
            .Include(race => race.Location);
    }

    private async Task<(Event? Event, IActionResult? Error)> FindOwned(int id, CancellationToken cancellationToken)
    {
        var race = await _db.Events.FirstOrDefaultAsync(item => item.EventId == id, cancellationToken);
        if (race is null)
            return (null, NotFound(new ApiMessage("Event does not exist.")));

        if (race.OrganiserId != SessionReader.UserId(HttpContext))
            return (null, StatusCode(StatusCodes.Status403Forbidden, new ApiMessage("You can only manage events you organised.")));

        return (race, null);
    }

    private async Task<IActionResult?> ValidateWrite(EventWriteRequest request, CancellationToken cancellationToken)
    {
        if (request.RegistrationDeadline > request.EventDate)
            return BadRequest(new ApiMessage("Registration must close on or before the event date."));

        var typeExists = await _db.EventTypes.AnyAsync(type => type.EventTypeId == request.EventTypeId, cancellationToken);
        if (!typeExists)
            return BadRequest(new ApiMessage("The selected event type does not exist."));

        var locationExists = await _db.Locations.AnyAsync(location => location.LocationId == request.LocationId, cancellationToken);
        if (!locationExists)
            return BadRequest(new ApiMessage("The selected location does not exist."));

        return null;
    }
}
