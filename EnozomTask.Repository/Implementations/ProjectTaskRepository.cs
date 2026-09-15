using EnozomTask.Data;
using EnozomTask.Data.Entities;
using EnozomTask.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnozomTask.Repository.Implementations;

public class ProjectTaskRepository : IProjectTaskRepository
{
    private readonly AppDbContext _context;

    public ProjectTaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectTask?> GetByClockifyIdAsync(
        string clockifyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Also find records added earlier in the current batch but not saved yet.
        var trackedProjectTask = _context.ProjectTasks.Local
            .FirstOrDefault(task => task.ClockifyId == clockifyId);

        if (trackedProjectTask is not null)
            return trackedProjectTask;

        return await _context.ProjectTasks
            .SingleOrDefaultAsync(
                task => task.ClockifyId == clockifyId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectTask>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.ProjectTasks
            .AsNoTracking()
            .Include(task => task.Project)
            .Include(task => task.AssignedUser)
            .OrderBy(task => task.Project.Name)
            .ThenBy(task => task.Name)
            .ThenBy(task => task.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        ProjectTask task, CancellationToken cancellationToken = default)
    {
        await _context.ProjectTasks.AddAsync(task, cancellationToken);
    }
}

