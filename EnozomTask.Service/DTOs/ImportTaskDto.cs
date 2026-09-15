namespace EnozomTask.Service.DTOs;

public class ImportTaskDto
{
    public string ProjectName { get; set; } = "";
    public string TaskName { get; set; } = "";
    public string AssignedUserClockifyId { get; set; } = "";
    public decimal OriginalEstimateHours { get; set; }
}
