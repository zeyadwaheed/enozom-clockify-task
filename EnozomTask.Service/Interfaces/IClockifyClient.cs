using EnozomTask.Service.DTOs;
using EnozomTask.Service.Integrations.Clockify.DTOs;

namespace EnozomTask.Service.Interfaces;

public interface IClockifyClient
{
    Task<IReadOnlyList<ClockifyUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClockifyProjectDto>> GetProjectsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClockifyTaskDto>> GetTasksAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClockifyTimeEntryDto>> GetTimeEntriesAsync(string userId, CancellationToken cancellationToken = default);
    Task<ClockifyProjectDto> CreateProjectAsync(string name, CancellationToken cancellationToken = default);
    Task<ClockifyTaskDto> CreateTaskAsync(string projectId, ImportTaskDto task, CancellationToken cancellationToken = default);
    Task<ClockifyTimeEntryDto> CreateTimeEntryAsync(string projectId, string taskId, ImportTimeEntryDto entry, CancellationToken cancellationToken = default);
}
