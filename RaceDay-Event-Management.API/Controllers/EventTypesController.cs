using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Mapping;
using RaceDay_Event_Management.API.Models;
using RaceDay_Event_Management.API.Security;

namespace RaceDay_Event_Management.API.Controllers;

[ApiController]
[Route("api/event-types")]
[Tags("Event Types")]
public class EventTypesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventTypesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lists the event types, such as Run, Walk, and Cycle.
    /// </summary>
    /// <response code="200">The event types were returned.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventTypeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var types = await _db.EventTypes
            .AsNoTracking()
            .OrderBy(type => type.TypeName)
            .ToListAsync(cancellationToken);

        return Ok(types.Select(Responses.EventType));
    }

    /// <summary>
    /// Returns one event type.
    /// </summary>
    /// <response code="200">The event type was found.</response>
    /// <response code="404">No event type exists with that id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var type = await _db.EventTypes.AsNoTracking().FirstOrDefaultAsync(item => item.EventTypeId == id, cancellationToken);
        if (type is null)
            return NotFound(new ApiMessage("Event type does not exist."));

        return Ok(Responses.EventType(type));
    }

    /// <summary>
    /// Adds an event type. Names must be unique.
    /// </summary>
    /// <response code="201">The event type was created.</response>
    /// <response code="400">The details are not valid.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The caller is not an organiser.</response>
    /// <response code="409">That event type already exists.</response>
    [HttpPost]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(EventTypeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(EventTypeWriteRequest request, CancellationToken cancellationToken)
    {
        var name = request.TypeName.Trim();
        if (await NameTaken(name, null, cancellationToken))
            return Conflict(new ApiMessage("An event type with that name already exists."));

        var type = new EventType
        {
            TypeName = name,
            Description = Clean(request.Description)
        };

        _db.EventTypes.Add(type);
        await _db.SaveChangesAsync(cancellationToken);
        return Created($"/api/event-types/{type.EventTypeId}", Responses.EventType(type));
    }

    /// <summary>
    /// Updates an event type.
    /// </summary>
    /// <response code="200">The event type was updated.</response>
    /// <response code="400">The details are not valid.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The caller is not an organiser.</response>
    /// <response code="404">No event type exists with that id.</response>
    /// <response code="409">Another event type already uses that name.</response>
    [HttpPut("{id:int}")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(EventTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, EventTypeWriteRequest request, CancellationToken cancellationToken)
    {
        var type = await _db.EventTypes.FirstOrDefaultAsync(item => item.EventTypeId == id, cancellationToken);
        if (type is null)
            return NotFound(new ApiMessage("Event type does not exist."));

        var name = request.TypeName.Trim();
        if (await NameTaken(name, id, cancellationToken))
            return Conflict(new ApiMessage("An event type with that name already exists."));

        type.TypeName = name;
        type.Description = Clean(request.Description);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(Responses.EventType(type));
    }

    /// <summary>
    /// Removes an event type that is not used by any event.
    /// </summary>
    /// <response code="204">The event type was deleted.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The caller is not an organiser.</response>
    /// <response code="404">No event type exists with that id.</response>
    /// <response code="409">An event still uses this type.</response>
    [HttpDelete("{id:int}")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var type = await _db.EventTypes.FirstOrDefaultAsync(item => item.EventTypeId == id, cancellationToken);
        if (type is null)
            return NotFound(new ApiMessage("Event type does not exist."));

        var inUse = await _db.Events.AnyAsync(race => race.EventTypeId == id, cancellationToken);
        if (inUse)
            return Conflict(new ApiMessage("This event type is still used by an event."));

        _db.EventTypes.Remove(type);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Task<bool> NameTaken(string name, int? exceptId, CancellationToken cancellationToken)
    {
        var normalised = name.ToLower();
        return _db.EventTypes.AnyAsync(
            type => type.TypeName.ToLower() == normalised
                && (exceptId == null || type.EventTypeId != exceptId),
            cancellationToken);
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
