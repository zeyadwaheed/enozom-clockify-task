namespace EnozomTask.Service.Integrations.Clockify.DTOs;

public class ClockifyTaskDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public string? Estimate { get; set; }
    public List<string>? AssigneeIds { get; set; }
    public string? AssigneeId { get; set; }

    public IReadOnlyList<string> GetAssigneeIds() =>
        AssigneeIds is { Count: > 0 }
            ? AssigneeIds.Distinct(StringComparer.Ordinal).ToList()
            : string.IsNullOrWhiteSpace(AssigneeId) ? [] : [AssigneeId];
}
