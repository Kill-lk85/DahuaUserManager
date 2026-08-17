namespace DahuaUserManager.Models.Schedules;

public class WorkSchedule
{
    public long Id { get; set; }

    public string Name { get; set; } = "";

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public double NormHours { get; set; } = 8;

    public bool IsActive { get; set; } = true;
}