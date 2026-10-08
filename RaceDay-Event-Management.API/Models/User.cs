using System.Text.Json.Serialization;

namespace RaceDay_Event_Management.API.Models;

public class User
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Kept off every JSON response, including any future endpoint that returns the user record.
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
