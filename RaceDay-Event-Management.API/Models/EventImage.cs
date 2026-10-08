namespace RaceDay_Event_Management.API.Models;

public class EventImage
{
    public int EventImageId { get; set; }

    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    public string ImageUrl { get; set; } = string.Empty;

    public string ImageType { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }
}
