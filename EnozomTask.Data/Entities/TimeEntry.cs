namespace EnozomTask.Data.Entities;

public class TimeEntry
{
    public int Id { get; set; }
    public required string ClockifyId { get; set; }

    public int ProjectTaskId { get; set; }
    public ProjectTask ProjectTask { get; set; } = null!;

    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }

    public string? Description { get; set; }
}
