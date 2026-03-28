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
    }
}
