namespace RaceDay_Event_Management.API.Models;

public class Result
{
    public int ResultId { get; set; }

    public int EnrolmentId { get; set; }

    public Enrolment Enrolment { get; set; } = null!;

    public TimeOnly FinishTime { get; set; }

    public int FinishPosition { get; set; }

    public DateTime RecordedAt { get; set; }
}
