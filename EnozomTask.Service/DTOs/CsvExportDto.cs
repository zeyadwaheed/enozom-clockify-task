namespace EnozomTask.Service.DTOs;

public record CsvExportDto(
    byte[] Content,
    string FileName,
    string ContentType,
    int RunningEntriesExcluded);
