using RaceDay_Event_Management.API.Contracts;
namespace RaceDay.Web.Models;
public class EventFormPage : EventWriteRequest
{
    public int EventId { get; set; }
    public IFormFile? BannerFile { get; set; }
    public List<EventImageResponse> Images { get; set; } = [];
    public List<LocationResponse> Locations { get; set; } = [];
    public List<EventTypeResponse> Types { get; set; } = [];
}

public class CategoryPage : CategoryWriteRequest
{
    public int CategoryId { get; set; }
    public EventDetailResponse Event { get; set; } = new();
}

public class EnrolmentPage
{
    public EventDetailResponse Event { get; set; } = new();
    public List<EnrolmentResponse> Enrolments { get; set; } = [];
    public List<ResultResponse> Results { get; set; } = [];
}
