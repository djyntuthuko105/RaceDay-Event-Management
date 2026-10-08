namespace RaceDay_Event_Management.API.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }

    public int ParticipantId { get; set; }

    public User Participant { get; set; } = null!;

    public int EventId { get; set; }

    public Event Event { get; set; } = null!;

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public DateTime EnrolmentDate { get; set; }

    public string Status { get; set; } = EnrolmentStatuses.Pending;

    public Result? Result { get; set; }
}
