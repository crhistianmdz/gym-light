namespace GymFlow.Infrastructure.Persistence.Seed;

using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public static class ProductSeeder
{
    public static async Task SeedAsync(
        GymFlowDbContext context,
        CancellationToken ct = default)
    {
        if (await context.Products.AnyAsync(ct))
            return;

        var products = new List<Product>
        {
            Product.Create("Agua mineral 500ml",        150m,  100, sku: "AGU-500"),
            Product.Create("Barra de proteína",         800m,   50, sku: "BAR-PRO"),
            Product.Create("Bebida isotónica 500ml",    350m,   80, sku: "ISO-500"),
            Product.Create("Suplemento proteico 1kg",  4500m,   30, sku: "SUP-PRO"),
            Product.Create("Guantes de entrenamiento", 2200m,   20, sku: "GUA-ENT"),
            Product.Create("Cinta para muñecas",        950m,   40, sku: "CIN-MUN"),
            Product.Create("Toalla deportiva",         1800m,   25, sku: "TOA-DEP"),
            Product.Create("Shake de chocolate 350ml",  600m,   60, sku: "SHA-CHO"),
            Product.Create("Creatina monohidratada 300g", 3200m, 35, sku: "CRE-MON"),
            Product.Create("Camiseta deportiva",       2800m,   45, sku: "CAM-DEP"),
        };

        await context.Products.AddRangeAsync(products, ct);
        await context.SaveChangesAsync(ct);
    }
}