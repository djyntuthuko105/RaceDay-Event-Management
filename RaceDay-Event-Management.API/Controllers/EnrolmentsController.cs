using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Mapping;
using RaceDay_Event_Management.API.Models;
using RaceDay_Event_Management.API.Security;

namespace RaceDay_Event_Management.API.Controllers;

[ApiController]
[Tags("Enrolments")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Enters the logged-in participant into an event under the chosen category.
    /// </summary>
    /// <response code="201">The enrolment was saved.</response>
    /// <response code="400">The category does not belong to this event.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The caller is not a participant.</response>
    /// <response code="404">The event or category does not exist.</response>
    /// <response code="409">The participant is already entered, registration has closed, or the category is full.</response>
    [HttpPost("api/events/{eventId:int}/enrolments")]
    [RequireSession(Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int eventId, CreateEnrolmentRequest request, CancellationToken cancellationToken)
    {
        var race = await _db.Events.FirstOrDefaultAsync(item => item.EventId == eventId, cancellationToken);
        if (race is null)
            return NotFound(new ApiMessage("Event does not exist."));

        var category = await _db.Categories.FirstOrDefaultAsync(
            item => item.CategoryId == request.CategoryId,
            cancellationToken);
        if (category is null)
            return NotFound(new ApiMessage("Category does not exist."));

        if (category.EventId != eventId)
            return BadRequest(new ApiMessage("That category does not belong to this event."));

        if (DateOnly.FromDateTime(DateTime.Today) > race.RegistrationDeadline)
            return Conflict(new ApiMessage("Registration for this event has closed."));

        var participantId = SessionReader.UserId(HttpContext);
        var alreadyEntered = await _db.Enrolments.AnyAsync(
            enrolment => enrolment.ParticipantId == participantId && enrolment.EventId == eventId,
            cancellationToken);
        if (alreadyEntered)
            return Conflict(new ApiMessage("You are already enrolled in this event."));

        if (category.MaximumParticipants is int limit)
        {
            var taken = await _db.Enrolments.CountAsync(
                enrolment => enrolment.CategoryId == category.CategoryId,
                cancellationToken);
            if (taken >= limit)
                return Conflict(new ApiMessage("This category is full."));
        }

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = eventId,
            CategoryId = category.CategoryId,
            Status = EnrolmentStatuses.Confirmed,
            EnrolmentDate = DateTime.Now
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync(cancellationToken);

        var saved = await WithDetails().FirstAsync(item => item.EnrolmentId == enrolment.EnrolmentId, cancellationToken);
        return Created($"/api/enrolments/{saved.EnrolmentId}", Responses.Enrolment(saved));
    }

    /// <summary>
    /// Lists the events the logged-in participant has entered.
    /// </summary>
    /// <response code="200">The participant's enrolments were returned.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The caller is not a participant.</response>
    [HttpGet("api/enrolments/me")]
    [RequireSession(Roles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var participantId = SessionReader.UserId(HttpContext);
        var enrolments = await WithDetails()
            .Where(enrolment => enrolment.ParticipantId == participantId)
            .OrderByDescending(enrolment => enrolment.EnrolmentDate)
            .ToListAsync(cancellationToken);

        return Ok(enrolments.Select(Responses.Enrolment));
    }

    /// <summary>
    /// Returns one enrolment belonging to the logged-in participant.
    /// </summary>
    /// <response code="200">The enrolment was found.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The enrolment belongs to someone else, or the caller is not a participant.</response>
    /// <response code="404">No enrolment exists with that id.</response>
    [HttpGet("api/enrolments/{id:int}")]
    [RequireSession(Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var enrolment = await WithDetails().FirstOrDefaultAsync(item => item.EnrolmentId == id, cancellationToken);
        if (enrolment is null)
            return NotFound(new ApiMessage("Enrolment does not exist."));

        if (enrolment.ParticipantId != SessionReader.UserId(HttpContext))
            return StatusCode(StatusCodes.Status403Forbidden, new ApiMessage("This enrolment belongs to another participant."));

        return Ok(Responses.Enrolment(enrolment));
    }

    /// <summary>
    /// Cancels an enrolment while the event is still in the future and no result has been recorded.
    /// </summary>
    /// <response code="204">The enrolment was cancelled.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The enrolment belongs to someone else, or the caller is not a participant.</response>
    /// <response code="404">No enrolment exists with that id.</response>
    /// <response code="409">The enrolment can no longer be cancelled.</response>
    [HttpDelete("api/enrolments/{id:int}")]
    [RequireSession(Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var enrolment = await _db.Enrolments
            .Include(item => item.Event)
            .Include(item => item.Result)
            .FirstOrDefaultAsync(item => item.EnrolmentId == id, cancellationToken);

        if (enrolment is null)
            return NotFound(new ApiMessage("Enrolment does not exist."));

        if (enrolment.ParticipantId != SessionReader.UserId(HttpContext))
            return StatusCode(StatusCodes.Status403Forbidden, new ApiMessage("This enrolment belongs to another participant."));

        var today = DateOnly.FromDateTime(DateTime.Today);
        var locked = enrolment.Result is not null
            || enrolment.Status == EnrolmentStatuses.Completed
            || enrolment.Event.EventDate <= today;

        if (locked)
            return Conflict(new ApiMessage("This enrolment can no longer be cancelled."));

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private IQueryable<Enrolment> WithDetails()
    {
        return _db.Enrolments
            .AsNoTracking()
            .Include(enrolment => enrolment.Participant)
            .Include(enrolment => enrolment.Event)
            .Include(enrolment => enrolment.Category);
    }
}
