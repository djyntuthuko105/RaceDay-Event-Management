namespace RaceDay_Event_Management.API.Models;

public class Category
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    public string CategoryName { get; set; } = string.Empty;

    public int? MinimumAge { get; set; }

    public int? MaximumAge { get; set; }

    public decimal? CategoryDistanceKm { get; set; }

    public int? MaximumParticipants { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
