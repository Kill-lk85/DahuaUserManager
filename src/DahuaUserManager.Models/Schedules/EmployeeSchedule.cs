namespace DahuaUserManager.Models.Schedules;

public class EmployeeSchedule
{
    public long Id { get; set; }

    public string UserId { get; set; } = "";

    public long ScheduleId { get; set; }

    public DateTime DateFrom { get; set; }

    public DateTime? DateTo { get; set; }
}