using RaceDay_Event_Management.API.Contracts;
namespace RaceDay.Web.Models;
public class EventFormPage : EventWriteRequest
{
    public int EventId { get; set; }
    public List<LocationResponse> Locations { get; set; } = [];
    public List<EventTypeResponse> Types { get; set; } = [];
}
