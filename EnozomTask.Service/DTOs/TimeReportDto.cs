namespace EnozomTask.Service.DTOs;

public record TimeReportDto(
    IReadOnlyList<TimeReportRowDto> Rows,
    int RunningEntriesExcluded);
