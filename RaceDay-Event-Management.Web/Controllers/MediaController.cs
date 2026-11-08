using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
namespace RaceDay.Web.Controllers;
public class MediaController(RaceDayApi api) : Controller
{
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> Event(int id)
    {
        var file = await api.DownloadAsync($"api/event-images/{id}/content");
        return File(file.Bytes, file.ContentType);
    }
}
