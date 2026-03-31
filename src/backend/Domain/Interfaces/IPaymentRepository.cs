using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken ct = default);
    Task<Payment?> GetByClientGuidAsync(Guid clientGuid, CancellationToken ct = default);
    Task<bool> ClientGuidExistsAsync(Guid clientGuid, CancellationToken ct = default);
    Task<IReadOnlyList<MonthlyAggregateRow>> GetMonthlyIncomeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);
    Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);
}

/// <summary>
/// Internal projection for monthly income aggregation.
/// </summary>
public record MonthlyAggregateRow
(
    int Year,
    int Month,
    PaymentCategory Category,
    decimal Total
);