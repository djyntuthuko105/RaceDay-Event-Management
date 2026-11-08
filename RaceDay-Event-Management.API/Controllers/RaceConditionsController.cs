using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Data;
using RaceDay_Event_Management.API.Services;
namespace RaceDay_Event_Management.API.Controllers;
[ApiController]
public class RaceConditionsController(RaceDayDbContext db, CurrentConditionsService weather) : ControllerBase
{
    [HttpGet("api/events/{id:int}/conditions")]
    [ProducesResponseType(typeof(RaceConditionsResponse), 200)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var race = await db.Events.Include(item => item.Location).FirstOrDefaultAsync(item => item.EventId == id, cancellationToken);
        return race is null ? NotFound(new ApiMessage("Event does not exist.")) : Ok(await weather.GetAsync(race.Location, cancellationToken));
    }
}
