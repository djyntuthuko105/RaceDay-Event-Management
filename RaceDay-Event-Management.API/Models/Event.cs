namespace RaceDay_Event_Management.API.Models;

public class Event
{
    public int EventId { get; set; }

    public int OrganiserId { get; set; }

    public User Organiser { get; set; } = null!;

    public int EventTypeId { get; set; }

    public EventType EventType { get; set; } = null!;

    public int LocationId { get; set; }

    public Location Location { get; set; } = null!;

    public string EventName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    public decimal DistanceKm { get; set; }

    public DateOnly RegistrationDeadline { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();

    public ICollection<EventImage> Images { get; set; } = new List<EventImage>();

    public ICollection<WeatherSnapshot> WeatherSnapshots { get; set; } = new List<WeatherSnapshot>();
}
