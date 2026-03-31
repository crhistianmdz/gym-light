using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly GymFlowDbContext _context;

    public MemberRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Member?> GetByIdAsync(Guid memberId, CancellationToken ct = default) =>
        await _context.Members.FirstOrDefaultAsync(m => m.Id == memberId, ct);

    public async Task<IReadOnlyList<Member>> GetAllActiveAsync(CancellationToken ct = default) =>
        await _context.Members
            .Where(m => m.Status == Domain.Enums.MemberStatus.Active)
            .ToListAsync(ct);

    public async Task AddAsync(Member member, CancellationToken ct = default)
    {
        await _context.Members.AddAsync(member, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Member member, CancellationToken ct = default)
    {
        _context.Members.Update(member);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(int TotalMembers, int ActiveMembers, int NotRenewed)> GetChurnStatsAsync(int year, CancellationToken ct = default)
    {
        var totalMembers = await _context.Members.CountAsync(ct);

        var activeMembers = await _context.Members
            .CountAsync(m => m.Status == MemberStatus.Active, ct);

        var notRenewed = await _context.Members
            .CountAsync(m => !m.AutoRenewEnabled && m.MembershipEndDate < DateTime.UtcNow, ct);

        return (totalMembers, activeMembers, notRenewed);
    }
}