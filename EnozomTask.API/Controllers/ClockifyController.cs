using EnozomTask.API.Contracts;
using EnozomTask.Service.DTOs;
using EnozomTask.Service.Integrations.Clockify.DTOs;
using EnozomTask.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EnozomTask.API.Controllers;

[ApiController]
[Route("api/clockify")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status502BadGateway)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
public class ClockifyController : ControllerBase
{
    private readonly IClockifyService _clockifyService;

    public ClockifyController(IClockifyService clockifyService)
    {
        _clockifyService = clockifyService;
    }

    /// <summary>Lists real Clockify workspace members and their IDs for dataset mapping.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<ClockifyUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClockifyUserDto>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await _clockifyService.GetWorkspaceUsersAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>Pushes the supplied dataset to Clockify and saves corresponding local records.</summary>
    /// <remarks>
    /// Use real workspace user IDs. Existing matching records are reused.
    /// Earlier successes remain saved if a later remote request or database operation fails.
    /// </remarks>
    [HttpPost("import")]
    [ProducesResponseType(typeof(SynchronizationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<SynchronizationResult>> Import(
        [FromBody] ImportDatasetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _clockifyService.ImportAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Pulls Clockify data into the local database.</summary>
    /// <remarks>
    /// A completed scan returns 200. Inspect isComplete and issues: unsupported records
    /// are reported explicitly and are not silently assigned to different users or tasks.
    /// </remarks>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(SynchronizationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<SynchronizationResult>> Synchronize(
        CancellationToken cancellationToken)
    {
        var result = await _clockifyService.SynchronizeAsync(cancellationToken);
        return Ok(result);
    }
}
