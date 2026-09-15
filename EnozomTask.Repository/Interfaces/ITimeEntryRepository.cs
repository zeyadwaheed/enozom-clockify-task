using EnozomTask.Data.Entities;

namespace EnozomTask.Repository.Interfaces;

public interface ITimeEntryRepository
{
    // Returns a tracked entity so the service can modify it before saving.
    Task<TimeEntry?> GetByClockifyIdAsync(
        string clockifyId, CancellationToken cancellationToken = default);

    // Returns untracked entities for reading and reporting.
    Task<IReadOnlyList<TimeEntry>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TimeEntry entry, CancellationToken cancellationToken = default);
}

