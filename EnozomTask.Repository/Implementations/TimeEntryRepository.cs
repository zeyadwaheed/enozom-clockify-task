using EnozomTask.Data;
using EnozomTask.Data.Entities;
using EnozomTask.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnozomTask.Repository.Implementations;

public class TimeEntryRepository : ITimeEntryRepository
{
    private readonly AppDbContext _context;

    public TimeEntryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TimeEntry?> GetByClockifyIdAsync(
        string clockifyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // find record not saved in db yet, if found return it.
        var trackedTimeEntry = _context.TimeEntries.Local
            .FirstOrDefault(entry => entry.ClockifyId == clockifyId);

        if (trackedTimeEntry is not null)
            return trackedTimeEntry;

        // if not found, get from the database
        return await _context.TimeEntries
            .SingleOrDefaultAsync(
                entry => entry.ClockifyId == clockifyId,
                cancellationToken);
    }

    // returns all time entries in the database, including those that are not yet saved to the database
    public async Task<IReadOnlyList<TimeEntry>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.TimeEntries
            .AsNoTracking()
            .Include(entry => entry.ProjectTask)
                .ThenInclude(task => task.AssignedUser)
            .Include(entry => entry.ProjectTask)
                .ThenInclude(task => task.Project)
            .OrderBy(entry => entry.StartUtc)
            .ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);
    }

    // mark this entry for addition to the database, but do not save it yet.
    public async Task AddAsync(
        TimeEntry entry, CancellationToken cancellationToken = default)
    {
        await _context.TimeEntries.AddAsync(entry, cancellationToken);
    }
}
