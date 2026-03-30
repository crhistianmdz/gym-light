using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence.Repositories;

public class AccessLogRepository : IAccessLogRepository
{
    private readonly GymFlowDbContext _context;

    public AccessLogRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ClientGuidExistsAsync(Guid clientGuid, CancellationToken ct = default) =>
        await _context.AccessLogs.AnyAsync(l => l.ClientGuid == clientGuid, ct);

    public async Task<AccessLog?> GetByClientGuidAsync(Guid clientGuid, CancellationToken ct = default) =>
        await _context.AccessLogs.FirstOrDefaultAsync(l => l.ClientGuid == clientGuid, ct);

    public async Task AddAsync(AccessLog accessLog, CancellationToken ct = default)
    {
        await _context.AccessLogs.AddAsync(accessLog, ct);
        await _context.SaveChangesAsync(ct);
    public async Task<(IEnumerable<AccessLog> Items, int TotalCount)> GetPagedAsync(
        AccessLogFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.AccessLogs.AsQueryable();

        // Apply 3-year retention filter
        var minDate = DateTime.UtcNow.AddYears(-3);
        query = query.Where(a => a.Timestamp >= minDate);

        // Apply optional filters
        if (filter.FromDate.HasValue)
            query = query.Where(a => a.Timestamp >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(a => a.Timestamp <= filter.ToDate.Value);
        if (filter.PerformedByUserId.HasValue)
            query = query.Where(a => a.PerformedByUserId == filter.PerformedByUserId.Value);
        if (filter.MemberId.HasValue)
            query = query.Where(a => a.MemberId == filter.MemberId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Result))
            query = query.Where(a => a.WasAllowed == (filter.Result == "Allowed"));

        int totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
