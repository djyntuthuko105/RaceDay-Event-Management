using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Mapping;
using RaceDay_Event_Management.API.Models;
using RaceDay_Event_Management.API.Security;

namespace RaceDay_Event_Management.API.Controllers;

[ApiController]
[Tags("Results")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Records a finish time and position for one enrolment. Only the organiser of that event can do this.
    /// </summary>
    /// <response code="201">The result was saved.</response>
    /// <response code="400">The finish time or position is not valid.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No enrolment exists with that id.</response>
    /// <response code="409">A result already exists for this enrolment, or that finish position is already used.</response>
    [HttpPost("api/enrolments/{enrolmentId:int}/result")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int enrolmentId, ResultWriteRequest request, CancellationToken cancellationToken)
    {
        var enrolment = await _db.Enrolments
            .Include(item => item.Event)
            .Include(item => item.Result)
            .FirstOrDefaultAsync(item => item.EnrolmentId == enrolmentId, cancellationToken);

        if (enrolment is null)
            return NotFound(new ApiMessage("Enrolment does not exist."));

        if (enrolment.Event.OrganiserId != SessionReader.UserId(HttpContext))
            return StatusCode(StatusCodes.Status403Forbidden, new ApiMessage("You can only record results for your own events."));

        if (enrolment.Result is not null)
            return Conflict(new ApiMessage("A result has already been recorded for this enrolment."));

        if (enrolment.Status == EnrolmentStatuses.Cancelled)
            return Conflict(new ApiMessage("A cancelled enrolment cannot receive a result."));

        if (await PositionTaken(enrolment.EventId, request.FinishPosition, null, cancellationToken))
            return Conflict(new ApiMessage("That finish position is already used for this event."));

        var result = new Result
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = request.FinishTime,
            FinishPosition = request.FinishPosition,
            RecordedAt = DateTime.Now
        };

        enrolment.Status = EnrolmentStatuses.Completed;
        _db.Results.Add(result);
        await _db.SaveChangesAsync(cancellationToken);

        var saved = await WithDetails().FirstAsync(item => item.ResultId == result.ResultId, cancellationToken);
        return Created($"/api/results/{saved.ResultId}", Responses.Result(saved));
    }

    /// <summary>
    /// Returns the logged-in participant's finish times and positions.
    /// </summary>
    /// <response code="200">The results were returned.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The caller is not a participant.</response>
    [HttpGet("api/results/me")]
    [RequireSession(Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var participantId = SessionReader.UserId(HttpContext);
        var results = await WithDetails()
            .Where(result => result.Enrolment.ParticipantId == participantId)
            .OrderByDescending(result => result.RecordedAt)
            .ToListAsync(cancellationToken);

        return Ok(results.Select(Responses.Result));
    }

    /// <summary>
    /// Returns one result belonging to the logged-in participant.
    /// </summary>
    /// <response code="200">The result was found.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The result belongs to someone else, or the caller is not a participant.</response>
    /// <response code="404">No result exists with that id.</response>
    [HttpGet("api/results/{id:int}")]
    [RequireSession(Roles.Participant)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var result = await WithDetails().FirstOrDefaultAsync(item => item.ResultId == id, cancellationToken);
        if (result is null)
            return NotFound(new ApiMessage("Result does not exist."));

        if (result.Enrolment.ParticipantId != SessionReader.UserId(HttpContext))
            return StatusCode(StatusCodes.Status403Forbidden, new ApiMessage("This result belongs to another participant."));

        return Ok(Responses.Result(result));
    }

    /// <summary>
    /// Lists the results captured for one event. Only the organiser who owns the event can call this.
    /// </summary>
    /// <response code="200">The results were returned.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No event exists with that id.</response>
    [HttpGet("api/events/{eventId:int}/results")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId, CancellationToken cancellationToken)
    {
        var race = await _db.Events.AsNoTracking().FirstOrDefaultAsync(item => item.EventId == eventId, cancellationToken);
        if (race is null)
            return NotFound(new ApiMessage("Event does not exist."));

        if (race.OrganiserId != SessionReader.UserId(HttpContext))
            return StatusCode(StatusCodes.Status403Forbidden, new ApiMessage("You can only view results for your own events."));

        var results = await WithDetails()
            .Where(result => result.Enrolment.EventId == eventId)
            .OrderBy(result => result.FinishPosition)
            .ToListAsync(cancellationToken);

        return Ok(results.Select(Responses.Result));
    }

    /// <summary>
    /// Corrects a finish time or position. Only the organiser of that event can do this.
    /// </summary>
    /// <response code="200">The result was updated.</response>
    /// <response code="400">The finish time or position is not valid.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No result exists with that id.</response>
    /// <response code="409">That finish position is already used for this event.</response>
    [HttpPut("api/results/{id:int}")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, ResultWriteRequest request, CancellationToken cancellationToken)
    {
        var result = await _db.Results
            .Include(item => item.Enrolment)
            .ThenInclude(enrolment => enrolment.Event)
            .FirstOrDefaultAsync(item => item.ResultId == id, cancellationToken);

        if (result is null)
            return NotFound(new ApiMessage("Result does not exist."));

        if (result.Enrolment.Event.OrganiserId != SessionReader.UserId(HttpContext))
            return StatusCode(StatusCodes.Status403Forbidden, new ApiMessage("You can only update results for your own events."));

        if (await PositionTaken(result.Enrolment.EventId, request.FinishPosition, result.ResultId, cancellationToken))
            return Conflict(new ApiMessage("That finish position is already used for this event."));

        result.FinishTime = request.FinishTime;
        result.FinishPosition = request.FinishPosition;
        await _db.SaveChangesAsync(cancellationToken);

        var updated = await WithDetails().FirstAsync(item => item.ResultId == result.ResultId, cancellationToken);
        return Ok(Responses.Result(updated));
    }

    private Task<bool> PositionTaken(int eventId, int finishPosition, int? exceptResultId, CancellationToken cancellationToken)
    {
        return _db.Results.AnyAsync(
            result => result.Enrolment.EventId == eventId
                && result.FinishPosition == finishPosition
                && (exceptResultId == null || result.ResultId != exceptResultId),
            cancellationToken);
    }

    private IQueryable<Result> WithDetails()
    {
        return _db.Results
            .AsNoTracking()
            .Include(result => result.Enrolment)
                .ThenInclude(enrolment => enrolment.Event)
            .Include(result => result.Enrolment)
                .ThenInclude(enrolment => enrolment.Category)
            .Include(result => result.Enrolment)
                .ThenInclude(enrolment => enrolment.Participant);
    }
}
