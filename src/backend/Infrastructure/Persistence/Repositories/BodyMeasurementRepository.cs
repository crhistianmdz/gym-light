using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IBodyMeasurementRepository.
/// </summary>
public class BodyMeasurementRepository : IBodyMeasurementRepository
{
    private readonly GymFlowDbContext _context;

    public BodyMeasurementRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(BodyMeasurement measurement, CancellationToken ct = default)
    {
        await _context.BodyMeasurements.AddAsync(measurement, ct);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BodyMeasurement>> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default)
    {
        return await _context.BodyMeasurements
            .Where(m => m.MemberId == memberId)
            .OrderByDescending(m => m.RecordedAt)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<BodyMeasurement?> GetByClientGuidAsync(string clientGuid, CancellationToken ct = default)
    {
        return await _context.BodyMeasurements
            .FirstOrDefaultAsync(m => m.ClientGuid == clientGuid, ct);
    }
}