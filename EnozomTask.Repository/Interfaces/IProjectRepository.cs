using EnozomTask.Data.Entities;

namespace EnozomTask.Repository.Interfaces;

public interface IProjectRepository
{
    // Returns a tracked entity so the service can modify it before saving.
    Task<Project?> GetByClockifyIdAsync(
        string clockifyId, CancellationToken cancellationToken = default);

    // Returns untracked entities for reading and reporting.
    Task<IReadOnlyList<Project>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Project project, CancellationToken cancellationToken = default);
}

