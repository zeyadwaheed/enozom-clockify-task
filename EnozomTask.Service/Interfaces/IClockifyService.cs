using EnozomTask.Service.DTOs;
using EnozomTask.Service.Integrations.Clockify.DTOs;

namespace EnozomTask.Service.Interfaces;

public interface IClockifyService
{
    Task<IReadOnlyList<ClockifyUserDto>> GetWorkspaceUsersAsync(CancellationToken cancellationToken = default);
    Task<SynchronizationResult> SynchronizeAsync(CancellationToken cancellationToken = default);
    Task<SynchronizationResult> ImportAsync(ImportDatasetRequest request, CancellationToken cancellationToken = default);
}
