namespace RaceDay_Event_Management.API.Models;

public static class Roles
{
    public const string Organiser = "Organiser";

    public const string Participant = "Participant";

    public static bool IsKnown(string? role) =>
        role == Organiser || role == Participant;
}

public static class EnrolmentStatuses
{
    public const string Pending = "Pending";

    public const string Confirmed = "Confirmed";

    public const string Cancelled = "Cancelled";

    public const string Completed = "Completed";
}
