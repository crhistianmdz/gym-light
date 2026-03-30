using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación de IMembershipFreezeRepository usando EF Core.
/// HU-07: gestión de congelamientos de membresías.
/// </summary>
public class MembershipFreezeRepository : IMembershipFreezeRepository
{
    private readonly GymFlowDbContext _context;

    public MembershipFreezeRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MembershipFreeze>> GetByMemberAndYearAsync(
        Guid memberId,
        int year,
        CancellationToken ct = default)
    {
        // Filtra por año calendario del StartDate (regla HU-07 R1)
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd   = new DateOnly(year, 12, 31);

        return await _context.MembershipFreezes
            .Where(f => f.MemberId == memberId
                     && f.StartDate >= yearStart
                     && f.StartDate <= yearEnd)
            .OrderBy(f => f.StartDate)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MembershipFreeze>> GetByMemberAsync(
        Guid memberId,
        CancellationToken ct = default)
    {
        return await _context.MembershipFreezes
            .Where(f => f.MemberId == memberId)
            .OrderByDescending(f => f.StartDate)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<MembershipFreeze?> GetActiveAsync(
        Guid memberId,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _context.MembershipFreezes
            .Where(f => f.MemberId == memberId
                     && f.StartDate <= today
                     && f.EndDate >= today)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc/>
    public async Task AddAsync(MembershipFreeze freeze, CancellationToken ct = default)
    {
        await _context.MembershipFreezes.AddAsync(freeze, ct);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(MembershipFreeze freeze, CancellationToken ct = default)
    {
        _context.MembershipFreezes.Remove(freeze);
        await _context.SaveChangesAsync(ct);
    }
}
