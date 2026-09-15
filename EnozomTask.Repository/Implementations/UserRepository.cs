using EnozomTask.Data;
using EnozomTask.Data.Entities;
using EnozomTask.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EnozomTask.Repository.Implementations;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ClockifyUser?> GetByClockifyIdAsync(
        string clockifyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // find record not saved in db yet, if found return it.
        var trackedUser = _context.Users.Local
            .FirstOrDefault(user => user.ClockifyId == clockifyId);

        if (trackedUser is not null)
            return trackedUser;

        // if more than one record raise an exception.
        return await _context.Users
            .SingleOrDefaultAsync(
                user => user.ClockifyId == clockifyId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ClockifyUser>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        ClockifyUser user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }
}

