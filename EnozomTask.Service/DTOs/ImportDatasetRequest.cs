namespace EnozomTask.Service.DTOs;

public class ImportDatasetRequest
{
    public List<ImportTaskDto> Tasks { get; set; } = [];
    public List<ImportTimeEntryDto> TimeEntries { get; set; } = [];
}
