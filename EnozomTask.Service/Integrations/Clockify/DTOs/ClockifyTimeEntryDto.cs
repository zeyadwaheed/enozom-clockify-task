namespace EnozomTask.Service.Integrations.Clockify.DTOs;

public class ClockifyTimeEntryDto
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string? ProjectId { get; set; }
    public string? TaskId { get; set; }
    public string? Description { get; set; }
    public ClockifyTimeIntervalDto? TimeInterval { get; set; }
}

public class ClockifyTimeIntervalDto
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset? End { get; set; }
}
