using EnozomTask.Data.Entities;

namespace EnozomTask.Repository.Interfaces;

public interface IUserRepository
{
    // Returns a tracked entity so the service can modify it before saving.
    Task<ClockifyUser?> GetByClockifyIdAsync(
        string clockifyId, CancellationToken cancellationToken = default);

    // Returns untracked entities for reading and reporting.
    Task<IReadOnlyList<ClockifyUser>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ClockifyUser user, CancellationToken cancellationToken = default);
}

