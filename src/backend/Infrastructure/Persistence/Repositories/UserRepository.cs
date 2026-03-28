namespace GymFlow.Infrastructure.Persistence.Repositories;

using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class UserRepository(GymFlowDbContext db) : IUserRepository
{
    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await db.AppUsers
                .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), ct);

    public async Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.AppUsers.FindAsync(new object[] { id }, ct);

    public async Task AddAsync(AppUser user, CancellationToken ct = default)
    {
        await db.AppUsers.AddAsync(user, ct);
        await db.SaveChangesAsync(ct);
    }
}