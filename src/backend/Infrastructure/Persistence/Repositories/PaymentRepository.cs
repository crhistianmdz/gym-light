using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for the Payment entity.
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly GymFlowDbContext _context;

    public PaymentRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Payment payment, CancellationToken ct = default)
    {
        await _context.Payments.AddAsync(payment, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<Payment?> GetByClientGuidAsync(Guid clientGuid, CancellationToken ct = default)
    {
        return await _context.Payments.FirstOrDefaultAsync(p => p.ClientGuid == clientGuid, ct);
    }

    public async Task<bool> ClientGuidExistsAsync(Guid clientGuid, CancellationToken ct = default)
    {
        return await _context.Payments.AnyAsync(p => p.ClientGuid == clientGuid, ct);
    }

    public async Task<IReadOnlyList<MonthlyAggregateRow>> GetMonthlyIncomeAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);

        return await _context.Payments
            .Where(p => p.Timestamp >= fromDt && p.Timestamp <= toDt)
            .GroupBy(p => new { p.Timestamp.Year, p.Timestamp.Month, p.Category })
            .Select(g => new MonthlyAggregateRow(
                g.Key.Year,
                g.Key.Month,
                g.Key.Category,
                g.Sum(p => p.Amount)))
            .ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;

        var totalCount = await _context.Payments.CountAsync(ct);
        var items = await _context.Payments
            .OrderByDescending(p => p.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}