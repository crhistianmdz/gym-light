using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence.Repositories;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Implementación del repositorio para la entidad Sale.
/// </summary>
 public class SaleRepository : ISaleRepository
 {
    public async Task<(IReadOnlyList<Sale> Items, int TotalCount)> GetAllPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Sales
            .OrderByDescending(S => S.Timestamp);

Total Count..

(TItems Queries).);

More.

{
    private readonly GymFlowDbContext _context;

    public SaleRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Sales.Include(s => s.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<bool> ClientGuidExistsAsync(Guid clientGuid, CancellationToken ct = default) =>


    public async Task<Sale?> GetByClientGuidAsync(Guid clientGuid, CancellationToken ct) =>
        await _context.Sales
            .Include(s => s.Lines)
            .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(s => s.ClientGuid == clientGuid, ct);


    public async Task AddAsync(Sale sale, CancellationToken ct = default)
    {
        await _context.Sales.AddAsync(sale, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Sale sale, CancellationToken ct = default)
    {
        _context.Sales.Update(sale);
        await _context.SaveChangesAsync(ct);
    }
}