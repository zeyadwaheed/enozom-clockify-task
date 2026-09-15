using EnozomTask.Data.Entities;

namespace EnozomTask.Repository.Interfaces;

public interface IProjectTaskRepository
{
    // Returns a tracked entity so the service can modify it before saving.
    Task<ProjectTask?> GetByClockifyIdAsync(
        string clockifyId, CancellationToken cancellationToken = default);

    // Returns untracked entities for reading and reporting.
    Task<IReadOnlyList<ProjectTask>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ProjectTask task, CancellationToken cancellationToken = default);
}

