namespace EnozomTask.Service.DTOs;

public record TimeReportRowDto(
    string User,
    string Project,
    string Task,
    decimal OriginalEstimateHours,
    decimal TimeSpentHours);
