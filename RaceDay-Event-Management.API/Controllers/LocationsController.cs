using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Mapping;
using RaceDay_Event_Management.API.Models;
using RaceDay_Event_Management.API.Security;

namespace RaceDay_Event_Management.API.Controllers;

[ApiController]
[Route("api/locations")]
[Tags("Locations")]
public class LocationsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public LocationsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lists venues that can be attached to an event. The caller must be logged in.
    /// </summary>
    /// <response code="200">The locations were returned.</response>
    /// <response code="401">The caller is not logged in.</response>
    [HttpGet]
    [RequireSession]
    [ProducesResponseType(typeof(IEnumerable<LocationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var locations = await _db.Locations
            .AsNoTracking()
            .OrderBy(location => location.City)
            .ThenBy(location => location.VenueName)
            .ToListAsync(cancellationToken);

        return Ok(locations.Select(Responses.Location));
    }

    /// <summary>
    /// Returns one venue.
    /// </summary>
    /// <response code="200">The location was found.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="404">No location exists with that id.</response>
    [HttpGet("{id:int}")]
    [RequireSession]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var location = await _db.Locations.AsNoTracking()
            .FirstOrDefaultAsync(item => item.LocationId == id, cancellationToken);
        if (location is null)
            return NotFound(new ApiMessage("Location does not exist."));

        return Ok(Responses.Location(location));
    }

    /// <summary>
    /// Adds a venue. Only an organiser can do this.
    /// </summary>
    /// <response code="201">The location was created.</response>
    /// <response code="400">The address details are not valid.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The caller is not an organiser.</response>
    [HttpPost]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(LocationWriteRequest request, CancellationToken cancellationToken)
    {
        var location = Map(new Location(), request);
        _db.Locations.Add(location);
        await _db.SaveChangesAsync(cancellationToken);
        return Created($"/api/locations/{location.LocationId}", Responses.Location(location));
    }

    /// <summary>
    /// Updates a venue.
    /// </summary>
    /// <response code="200">The location was updated.</response>
    /// <response code="400">The address details are not valid.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The caller is not an organiser.</response>
    /// <response code="404">No location exists with that id.</response>
    [HttpPut("{id:int}")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, LocationWriteRequest request, CancellationToken cancellationToken)
    {
        var location = await _db.Locations.FirstOrDefaultAsync(item => item.LocationId == id, cancellationToken);
        if (location is null)
            return NotFound(new ApiMessage("Location does not exist."));

        Map(location, request);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(Responses.Location(location));
    }

    /// <summary>
    /// Deletes a venue that is not linked to an event.
    /// </summary>
    /// <response code="204">The location was deleted.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The caller is not an organiser.</response>
    /// <response code="404">No location exists with that id.</response>
    /// <response code="409">An event still uses this location.</response>
    [HttpDelete("{id:int}")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var location = await _db.Locations.FirstOrDefaultAsync(item => item.LocationId == id, cancellationToken);
        if (location is null)
            return NotFound(new ApiMessage("Location does not exist."));

        var inUse = await _db.Events.AnyAsync(race => race.LocationId == id, cancellationToken);
        if (inUse)
            return Conflict(new ApiMessage("This location is still used by an event."));

        _db.Locations.Remove(location);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static Location Map(Location location, LocationWriteRequest request)
    {
        location.VenueName = request.VenueName.Trim();
        location.AddressLine = request.AddressLine.Trim();
        location.City = request.City.Trim();
        location.Province = request.Province.Trim();
        location.PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim();
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        return location;
    }
}
