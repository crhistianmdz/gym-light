using GymFlow.Domain.Entities;
namespace GymFlow.Domain.Interfaces;

/// <summary>
/// Repositorio para acceso a las ventas (Sale).
/// </summary>
public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ClientGuidExistsAsync(Guid clientGuid, CancellationToken ct = default);
    Task<Sale?> GetByClientGuidAsync(Guid clientGuid, CancellationToken ct = default);
    Task AddAsync(Sale sale, CancellationToken ct = default);
    Task UpdateAsync(Sale sale, CancellationToken ct = default);
    Task<(IReadOnlyList<Sale> Items, int TotalCount)> GetAllPagedAsync(
        int page, int pageSize, CancellationToken ct = default);
}
