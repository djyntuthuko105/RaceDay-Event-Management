using System.ComponentModel.DataAnnotations;

namespace RaceDay_Event_Management.API.Contracts;

public record ApiMessage(string Message);

public class RegisterRequest
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? ProfilePictureUrl { get; set; }
}

public class UserResponse
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class EventTypeWriteRequest
{
    [Required]
    [MaxLength(30)]
    public string TypeName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }
}

public class EventTypeResponse
{
    public int EventTypeId { get; set; }

    public string TypeName { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class LocationWriteRequest
{
    [Required]
    [MaxLength(150)]
    public string VenueName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Province { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? PostalCode { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }
}

public class LocationResponse
{
    public int LocationId { get; set; }

    public string VenueName { get; set; } = string.Empty;

    public string AddressLine { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public string? PostalCode { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }
}

public class EventWriteRequest
{
    [Required]
    [MaxLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateOnly EventDate { get; set; }

    [Range(0.01, 9999.99)]
    public decimal DistanceKm { get; set; }

    [Required]
    public DateOnly RegistrationDeadline { get; set; }

    [Range(1, int.MaxValue)]
    public int EventTypeId { get; set; }

    [Range(1, int.MaxValue)]
    public int LocationId { get; set; }
}

public class EventResponse
{
    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    public decimal DistanceKm { get; set; }

    public DateOnly RegistrationDeadline { get; set; }

    public int OrganiserId { get; set; }

    public string OrganiserName { get; set; } = string.Empty;

    public int EventTypeId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public int LocationId { get; set; }

    public string VenueName { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class CategoryWriteRequest
{
    [Required]
    [MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0, 120)]
    public int? MinimumAge { get; set; }

    [Range(0, 120)]
    public int? MaximumAge { get; set; }

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal? CategoryDistanceKm { get; set; }

    [Range(1, 100000)]
    public int? MaximumParticipants { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }

    public int EventId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int? MinimumAge { get; set; }

    public int? MaximumAge { get; set; }

    public decimal? CategoryDistanceKm { get; set; }

    public int? MaximumParticipants { get; set; }
}

public class CreateEnrolmentRequest
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

public class EnrolmentResponse
{
    public int EnrolmentId { get; set; }

    public int ParticipantId { get; set; }

    public string ParticipantName { get; set; } = string.Empty;

    public string ParticipantEmail { get; set; } = string.Empty;

    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public DateOnly EventDate { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime EnrolmentDate { get; set; }
}

public class ResultWriteRequest
{
    [Required]
    public TimeOnly FinishTime { get; set; }

    [Range(1, 100000)]
    public int FinishPosition { get; set; }
}

public class ResultResponse
{
    public int ResultId { get; set; }

    public int EnrolmentId { get; set; }

    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public int ParticipantId { get; set; }

    public string ParticipantName { get; set; } = string.Empty;

    public TimeOnly FinishTime { get; set; }

    public int FinishPosition { get; set; }

    public DateTime RecordedAt { get; set; }
}

public class ImageUploadRequest
{
    public IFormFile File { get; set; } = null!;
}

public class EventImageResponse
{
    public int EventImageId { get; set; }

    public int EventId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string ImageType { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; }
}

public class WeatherWriteRequest
{
    public decimal TemperatureCelsius { get; set; }

    public decimal FeelsLikeCelsius { get; set; }

    [Required]
    [MaxLength(100)]
    public string WeatherCondition { get; set; } = string.Empty;

    [Range(0, 500)]
    public decimal WindSpeedKmh { get; set; }

    [Range(0, 100)]
    public int HumidityPercentage { get; set; }

    public DateTime? RecordedAt { get; set; }
}

public class WeatherResponse
{
    public int WeatherSnapshotId { get; set; }

    public int EventId { get; set; }

    public decimal? TemperatureCelsius { get; set; }

    public decimal? FeelsLikeCelsius { get; set; }

    public string? WeatherCondition { get; set; }

    public decimal? WindSpeedKmh { get; set; }

    public int? HumidityPercentage { get; set; }

    public DateTime RecordedAt { get; set; }
}
