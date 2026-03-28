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
            Product.Create("AGU-500", "Agua mineral 500ml", null, 150, 100),
            Product.Create("BAR-PRO", "Barra de proteína", null, 800, 50),
            Product.Create("ISO-500", "Bebida isotónica 500ml", null, 350, 80),
            Product.Create("SUP-PRO", "Suplemento proteico 1kg", null, 4500, 30),
            Product.Create("GUA-ENT", "Guantes de entrenamiento", null, 2200, 20),
            Product.Create("CIN-MUN", "Cinta para muñecas", null, 950, 40),
            Product.Create("TOA-DEP", "Toalla deportiva", null, 1800, 25),
            Product.Create("SHA-CHO", "Shake de chocolate 350ml", null, 600, 60),
            Product.Create("CRE-MON", "Creatina monohidratada 300g", null, 3200, 35),
            Product.Create("CAM-DEP", "Camiseta deportiva", null, 2800, 45),
        };

        await context.Products.AddRangeAsync(products, ct);
        await context.SaveChangesAsync(ct);
    }
}