using EnozomTask.Data;
using EnozomTask.Data.Entities;
using EnozomTask.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnozomTask.Repository.Implementations;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByClockifyIdAsync(
        string clockifyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Also find records added earlier in the current batch but not saved yet.
        var trackedProject = _context.Projects.Local
            .FirstOrDefault(project => project.ClockifyId == clockifyId);

        if (trackedProject is not null)
            return trackedProject;

        return await _context.Projects
            .SingleOrDefaultAsync(
                project => project.ClockifyId == clockifyId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .AsNoTracking()
            .OrderBy(project => project.Name)
            .ThenBy(project => project.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Project project, CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
    }
}

