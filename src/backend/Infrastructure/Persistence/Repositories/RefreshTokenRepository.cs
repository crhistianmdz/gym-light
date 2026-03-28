namespace GymFlow.Infrastructure.Persistence.Repositories;

using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

public class RefreshTokenRepository(GymFlowDbContext db) : IRefreshTokenRepository
{
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        await db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        await db.RefreshTokens.AddAsync(token, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task RevokeAsync(RefreshToken token, CancellationToken ct = default)
    {
        token.Revoke();
        db.RefreshTokens.Update(token);
        await db.SaveChangesAsync(ct);
    }
}