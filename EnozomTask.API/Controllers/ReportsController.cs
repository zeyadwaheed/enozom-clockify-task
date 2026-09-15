using System.Globalization;
using EnozomTask.API.Contracts;
using EnozomTask.Service.DTOs;
using EnozomTask.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EnozomTask.API.Controllers;

[ApiController]
[Route("api/reports")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
public class ReportsController : ControllerBase
{
    private readonly ITimeReportService _reportService;

    public ReportsController(ITimeReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>Returns completed-time totals by task, with the task's assigned user.</summary>
    [HttpGet("time")]
    [ProducesResponseType(typeof(TimeReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TimeReportDto>> GetTimeReport(CancellationToken cancellationToken)
    {
        var report = await _reportService.GetReportAsync(cancellationToken);
        return Ok(report);
    }

    /// <summary>Downloads the consolidated tracked-time CSV.</summary>
    /// <remarks>
    /// Running timers are excluded. Their count is returned in X-Running-Entries-Excluded.
    /// </remarks>
    [HttpGet("time/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportTimeReport(CancellationToken cancellationToken)
    {
        var export = await _reportService.ExportCsvAsync(cancellationToken);
        Response.Headers["X-Running-Entries-Excluded"] =
            export.RunningEntriesExcluded.ToString(CultureInfo.InvariantCulture);
        return File(export.Content, export.ContentType, export.FileName);
    }
}
