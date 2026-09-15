namespace EnozomTask.Data.Entities;

public class ProjectTask
{
    public int Id { get; set; }
    public required string ClockifyId { get; set; }
    public required string Name { get; set; }

    public decimal OriginalEstimateHours { get; set; }

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public int AssignedUserId { get; set; }
    public ClockifyUser AssignedUser { get; set; } = null!;

    public ICollection<TimeEntry> TimeEntries { get; set; }
        = new List<TimeEntry>();
}
