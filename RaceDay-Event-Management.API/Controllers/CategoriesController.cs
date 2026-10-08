using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Mapping;
using RaceDay_Event_Management.API.Models;
using RaceDay_Event_Management.API.Security;

namespace RaceDay_Event_Management.API.Controllers;

[ApiController]
[Tags("Categories")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lists the age or distance categories for one event.
    /// </summary>
    /// <response code="200">The categories were returned.</response>
    /// <response code="404">No event exists with that id.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListForEvent(int eventId, CancellationToken cancellationToken)
    {
        var eventExists = await _db.Events.AnyAsync(race => race.EventId == eventId, cancellationToken);
        if (!eventExists)
            return NotFound(new ApiMessage("Event does not exist."));

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(category => category.EventId == eventId)
            .OrderBy(category => category.CategoryName)
            .ToListAsync(cancellationToken);

        return Ok(categories.Select(Responses.Category));
    }

    /// <summary>
    /// Returns one category.
    /// </summary>
    /// <response code="200">The category was found.</response>
    /// <response code="404">No category exists with that id.</response>
    [HttpGet("api/categories/{id:int}")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var category = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(item => item.CategoryId == id, cancellationToken);
        if (category is null)
            return NotFound(new ApiMessage("Category does not exist."));

        return Ok(Responses.Category(category));
    }

    /// <summary>
    /// Adds a category to an event the organiser owns.
    /// </summary>
    /// <response code="201">The category was created.</response>
    /// <response code="400">The age range or other details are not valid.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No event exists with that id.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryWriteRequest request, CancellationToken cancellationToken)
    {
        var owned = await FindOwnedEvent(eventId, cancellationToken);
        if (owned.Error is not null)
            return owned.Error;

        var problem = Validate(request);
        if (problem is not null)
            return problem;

        var category = new Category { EventId = eventId };
        Apply(category, request);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return Created($"/api/categories/{category.CategoryId}", Responses.Category(category));
    }

    /// <summary>
    /// Updates a category on an event the organiser owns.
    /// </summary>
    /// <response code="200">The category was updated.</response>
    /// <response code="400">The age range or other details are not valid.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No category exists with that id.</response>
    [HttpPut("api/categories/{id:int}")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryWriteRequest request, CancellationToken cancellationToken)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(item => item.CategoryId == id, cancellationToken);
        if (category is null)
            return NotFound(new ApiMessage("Category does not exist."));

        var owned = await FindOwnedEvent(category.EventId, cancellationToken);
        if (owned.Error is not null)
            return owned.Error;

        var problem = Validate(request);
        if (problem is not null)
            return problem;

        Apply(category, request);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(Responses.Category(category));
    }

    /// <summary>
    /// Removes a category that nobody has entered.
    /// </summary>
    /// <response code="204">The category was deleted.</response>
    /// <response code="401">The caller is not logged in.</response>
    /// <response code="403">The event belongs to another organiser.</response>
    /// <response code="404">No category exists with that id.</response>
    /// <response code="409">Participants are already enrolled in this category.</response>
    [HttpDelete("api/categories/{id:int}")]
    [RequireSession(Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(item => item.CategoryId == id, cancellationToken);
        if (category is null)
            return NotFound(new ApiMessage("Category does not exist."));

        var owned = await FindOwnedEvent(category.EventId, cancellationToken);
        if (owned.Error is not null)
            return owned.Error;

        var inUse = await _db.Enrolments.AnyAsync(enrolment => enrolment.CategoryId == id, cancellationToken);
        if (inUse)
            return Conflict(new ApiMessage("This category still has enrolments."));

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<(Event? Event, IActionResult? Error)> FindOwnedEvent(int eventId, CancellationToken cancellationToken)
    {
        var race = await _db.Events.FirstOrDefaultAsync(item => item.EventId == eventId, cancellationToken);
        if (race is null)
            return (null, NotFound(new ApiMessage("Event does not exist.")));

        if (race.OrganiserId != SessionReader.UserId(HttpContext))
            return (null, StatusCode(StatusCodes.Status403Forbidden, new ApiMessage("You can only manage events you organised.")));

        return (race, null);
    }

    private static IActionResult? Validate(CategoryWriteRequest request)
    {
        if (request.MinimumAge is int minimum && request.MaximumAge is int maximum && maximum < minimum)
            return new BadRequestObjectResult(new ApiMessage("Maximum age cannot be lower than minimum age."));

        return null;
    }

    private static void Apply(Category category, CategoryWriteRequest request)
    {
        category.CategoryName = request.CategoryName.Trim();
        category.MinimumAge = request.MinimumAge;
        category.MaximumAge = request.MaximumAge;
        category.CategoryDistanceKm = request.CategoryDistanceKm;
        category.MaximumParticipants = request.MaximumParticipants;
    }
}
