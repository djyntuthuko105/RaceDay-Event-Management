using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Mapping;
using RaceDay_Event_Management.API.Models;
using RaceDay_Event_Management.API.Security;

namespace RaceDay_Event_Management.API.Controllers;

[ApiController]
[Tags("Weather")]
public class WeatherController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public WeatherController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the weather snapshots stored for an event.
    /// </summary>
    /// <response code="200">The weather snapshots were returned.</response>
    /// <response code="404">No event exists with that id.</response>
    [HttpGet("api/events/{eventId:int}/weather")]
    [ProducesResponseType(typeof(IEnumerable<WeatherResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId, CancellationToken cancellationToken)
    {
        var eventExists = await _db.Events.AnyAsync(race => race.EventId == eventId, cancellationToken);
        if (!eventExists)
            return NotFound(new ApiMessage("Event does not exist."));

        var snapshots = await _db.WeatherSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.EventId == eventId)
            .OrderByDescending(snapshot => snapshot.RecordedAt)
            .ToListAsync(cancellationToken);

        return Ok(snapshots.Select(Responses.Weather));
    }

    /// <summary>
    /// Stores a weather snapshot against an event. Any organiser can do this.
    /// </summary>
    /// <response code="201">The snapshot was saved.</response>
    /// <response code="400">The weather details are not valid.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The caller is not an organiser.</response>
    /// <response code="404">No event exists with that id.</response>
    [HttpPost("api/events/{eventId:int}/weather")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, WeatherWriteRequest request, CancellationToken cancellationToken)
    {
        var eventExists = await _db.Events.AnyAsync(race => race.EventId == eventId, cancellationToken);
        if (!eventExists)
            return NotFound(new ApiMessage("Event does not exist."));

        var snapshot = new WeatherSnapshot
        {
            EventId = eventId,
            TemperatureCelsius = request.TemperatureCelsius,
            FeelsLikeCelsius = request.FeelsLikeCelsius,
            WeatherCondition = request.WeatherCondition.Trim(),
            WindSpeedKmh = request.WindSpeedKmh,
            HumidityPercentage = request.HumidityPercentage,
            RecordedAt = request.RecordedAt ?? DateTime.Now
        };

        _db.WeatherSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(cancellationToken);
        return Created($"/api/weather/{snapshot.WeatherSnapshotId}", Responses.Weather(snapshot));
    }

    /// <summary>
    /// Returns one weather snapshot.
    /// </summary>
    /// <response code="200">The snapshot was found.</response>
    /// <response code="404">No snapshot exists with that id.</response>
    [HttpGet("api/weather/{id:int}")]
    [ProducesResponseType(typeof(WeatherResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var snapshot = await _db.WeatherSnapshots.AsNoTracking()
            .FirstOrDefaultAsync(item => item.WeatherSnapshotId == id, cancellationToken);
        if (snapshot is null)
            return NotFound(new ApiMessage("Weather snapshot does not exist."));

        return Ok(Responses.Weather(snapshot));
    }
}
