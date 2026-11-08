using RaceDay_Event_Management.API.Contracts;
namespace RaceDay.Web.Models;
public class BrowsePage
{
    public List<EventResponse> Events { get; set; } = [];
    public List<EventTypeResponse> Types { get; set; } = [];
    public string? Search { get; set; }
    public int? TypeId { get; set; }
    public string? City { get; set; }
}
