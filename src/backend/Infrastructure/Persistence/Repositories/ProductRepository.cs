using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación del repositorio para la entidad Product.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly GymFlowDbContext _context;

    public ProductRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        await _context.Products.FirstOrDefaultAsync(p => p.Sku == sku, ct);

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Products.ToListAsync(ct);

    public async Task<IEnumerable<Product>> GetLowStockAsync(CancellationToken ct = default) =>
        await _context.Products
                      .Where(p => p.Stock <= p.InitialStock * 0.20)
                      .ToListAsync(ct);

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await _context.Products.AddAsync(product, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await GetByIdAsync(id, ct);
        if (product == null) return;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(ct);
    }
}