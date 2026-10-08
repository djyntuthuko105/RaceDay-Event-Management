using RaceDay_Event_Management.API.Contracts;
using RaceDay_Event_Management.API.Models;

namespace RaceDay_Event_Management.API.Mapping;

public static class Responses
{
    public static UserResponse User(User user) => new()
    {
        UserId = user.UserId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Role = user.Role,
        ProfilePictureUrl = user.ProfilePictureUrl,
        CreatedAt = user.CreatedAt
    };

    public static EventTypeResponse EventType(EventType type) => new()
    {
        EventTypeId = type.EventTypeId,
        TypeName = type.TypeName,
        Description = type.Description
    };

    public static LocationResponse Location(Location location) => new()
    {
        LocationId = location.LocationId,
        VenueName = location.VenueName,
        AddressLine = location.AddressLine,
        City = location.City,
        Province = location.Province,
        PostalCode = location.PostalCode,
        Latitude = location.Latitude,
        Longitude = location.Longitude
    };

    public static EventResponse Event(Event race) => new()
    {
        EventId = race.EventId,
        EventName = race.EventName,
        Description = race.Description,
        EventDate = race.EventDate,
        DistanceKm = race.DistanceKm,
        RegistrationDeadline = race.RegistrationDeadline,
        OrganiserId = race.OrganiserId,
        OrganiserName = race.Organiser is null
            ? string.Empty
            : $"{race.Organiser.FirstName} {race.Organiser.LastName}",
        EventTypeId = race.EventTypeId,
        EventType = race.EventType?.TypeName ?? string.Empty,
        LocationId = race.LocationId,
        VenueName = race.Location?.VenueName ?? string.Empty,
        City = race.Location?.City ?? string.Empty,
        Province = race.Location?.Province ?? string.Empty,
        CreatedAt = race.CreatedAt
    };

    public static CategoryResponse Category(Category category) => new()
    {
        CategoryId = category.CategoryId,
        EventId = category.EventId,
        CategoryName = category.CategoryName,
        MinimumAge = category.MinimumAge,
        MaximumAge = category.MaximumAge,
        CategoryDistanceKm = category.CategoryDistanceKm,
        MaximumParticipants = category.MaximumParticipants
    };

    public static EnrolmentResponse Enrolment(Enrolment enrolment) => new()
    {
        EnrolmentId = enrolment.EnrolmentId,
        ParticipantId = enrolment.ParticipantId,
        ParticipantName = enrolment.Participant is null
            ? string.Empty
            : $"{enrolment.Participant.FirstName} {enrolment.Participant.LastName}",
        ParticipantEmail = enrolment.Participant?.Email ?? string.Empty,
        EventId = enrolment.EventId,
        EventName = enrolment.Event?.EventName ?? string.Empty,
        EventDate = enrolment.Event?.EventDate ?? default,
        CategoryId = enrolment.CategoryId,
        CategoryName = enrolment.Category?.CategoryName ?? string.Empty,
        Status = enrolment.Status,
        EnrolmentDate = enrolment.EnrolmentDate
    };

    public static ResultResponse Result(Result result) => new()
    {
        ResultId = result.ResultId,
        EnrolmentId = result.EnrolmentId,
        EventId = result.Enrolment?.EventId ?? 0,
        EventName = result.Enrolment?.Event?.EventName ?? string.Empty,
        CategoryName = result.Enrolment?.Category?.CategoryName ?? string.Empty,
        ParticipantId = result.Enrolment?.ParticipantId ?? 0,
        ParticipantName = result.Enrolment?.Participant is null
            ? string.Empty
            : $"{result.Enrolment.Participant.FirstName} {result.Enrolment.Participant.LastName}",
        FinishTime = result.FinishTime,
        FinishPosition = result.FinishPosition,
        RecordedAt = result.RecordedAt
    };

    public static EventImageResponse EventImage(EventImage image) => new()
    {
        EventImageId = image.EventImageId,
        EventId = image.EventId,
        ImageUrl = image.ImageUrl,
        ImageType = image.ImageType,
        UploadedAt = image.UploadedAt
    };

    public static WeatherResponse Weather(WeatherSnapshot snapshot) => new()
    {
        WeatherSnapshotId = snapshot.WeatherSnapshotId,
        EventId = snapshot.EventId,
        TemperatureCelsius = snapshot.TemperatureCelsius,
        FeelsLikeCelsius = snapshot.FeelsLikeCelsius,
        WeatherCondition = snapshot.WeatherCondition,
        WindSpeedKmh = snapshot.WindSpeedKmh,
        HumidityPercentage = snapshot.HumidityPercentage,
        RecordedAt = snapshot.RecordedAt
    };
}
