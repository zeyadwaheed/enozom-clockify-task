using EnozomTask.Service.DTOs;

namespace EnozomTask.Service.Interfaces;

public interface ITimeReportService
{
    Task<TimeReportDto> GetReportAsync(CancellationToken cancellationToken = default);
    Task<CsvExportDto> ExportCsvAsync(CancellationToken cancellationToken = default);
}
