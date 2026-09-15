using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using EnozomTask.Repository.Interfaces;
using EnozomTask.Service.DTOs;
using ValidationException = EnozomTask.Service.Exceptions.ValidationException;
using EnozomTask.Service.Interfaces;

namespace EnozomTask.Service.Implementations;

public class TimeReportService : ITimeReportService
{
    private readonly ITimeEntryRepository _timeEntries;

    public TimeReportService(ITimeEntryRepository timeEntries)
    {
        _timeEntries = timeEntries;
    }

    public async Task<TimeReportDto> GetReportAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _timeEntries.GetAllAsync(cancellationToken);
        if (entries.Any(entry => entry.EndUtc.HasValue && entry.EndUtc <= entry.StartUtc))
            throw new ValidationException("A saved time entry has an invalid interval; synchronize or correct it before reporting.");

        var rows = entries
            .Where(entry => entry.EndUtc.HasValue)
            .GroupBy(entry => entry.ProjectTaskId)
            .Select(group =>
            {
                var first = group.First();
                var elapsedTicks = group.Sum(entry => (decimal)(entry.EndUtc!.Value - entry.StartUtc).Ticks);
                return new TimeReportRowDto(
                    first.ProjectTask.AssignedUser.Name,
                    first.ProjectTask.Project.Name,
                    first.ProjectTask.Name,
                    first.ProjectTask.OriginalEstimateHours,
                    decimal.Round(elapsedTicks / TimeSpan.TicksPerHour, 2, MidpointRounding.AwayFromZero));
            })
            .OrderBy(row => row.User, StringComparer.Ordinal)
            .ThenBy(row => row.Project, StringComparer.Ordinal)
            .ThenBy(row => row.Task, StringComparer.Ordinal)
            .ToList();

        return new TimeReportDto(rows, entries.Count(entry => !entry.EndUtc.HasValue));
    }

    public async Task<CsvExportDto> ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        var report = await GetReportAsync(cancellationToken);
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            InjectionOptions = InjectionOptions.Escape
        };
        using (var csv = new CsvWriter(writer, configuration, leaveOpen: true))
        {
            foreach (var header in new[] { "User", "Project", "Task", "Original Estimate(hrs)", "TimeSpent(hrs)" })
                csv.WriteField(header);
            csv.NextRecord();

            foreach (var row in report.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                csv.WriteField(row.User);
                csv.WriteField(row.Project);
                csv.WriteField(row.Task);
                csv.WriteField(row.OriginalEstimateHours.ToString("0.####", CultureInfo.InvariantCulture));
                csv.WriteField(row.TimeSpentHours.ToString("F2", CultureInfo.InvariantCulture));
                csv.NextRecord();
            }
        }

        return new CsvExportDto(
            Encoding.UTF8.GetBytes(writer.ToString()),
            "tracked-time.csv",
            "text/csv; charset=utf-8",
            report.RunningEntriesExcluded);
    }
}
