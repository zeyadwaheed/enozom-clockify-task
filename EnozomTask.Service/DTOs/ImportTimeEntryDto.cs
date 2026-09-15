namespace EnozomTask.Service.DTOs;

public class ImportTimeEntryDto
{
    public string UserClockifyId { get; set; } = "";
    public string ProjectName { get; set; } = "";
    public string TaskName { get; set; } = "";
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public string? Description { get; set; }
}
