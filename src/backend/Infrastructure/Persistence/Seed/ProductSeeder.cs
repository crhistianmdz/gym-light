namespace GymFlow.Infrastructure.Persistence.Seed;

using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Seed data for development and testing environments.
/// Runs automatically in Development mode via Program.cs.
/// </summary>
public static class ProductSeeder
{
    public static async Task SeedAsync(
        GymFlowDbContext context,
        CancellationToken ct = default)
    {
        // Seed Products
        if (!await context.Products.AnyAsync(ct))
        {
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
        }

        // Seed Admin User
        if (!await context.Users.AnyAsync(ct))
        {
            // Password: admin123 (BCrypt hash for "admin123")
            const string passwordHash = "$2a$11$OvXHWz9E4T2CQvV3B5X3EOZ8Z6ZQZ1Z2Z3Z4Z5Z6Z7Z8Z9Z0Z1Z2Z3Z";
            
            var admin = AppUser.Create(
                "admin@demo.com",
                "Admin Demo",
                passwordHash,
                UserRole.Admin
            );
            context.Users.Add(admin);
        }

        // Seed Members (10 demo members)
        if (!await context.Members.AnyAsync(ct))
        {
            var memberNames = new[]
            {
                "Juan Pérez", "María González", "Carlos López", "Ana Martínez",
                "Pedro Rodríguez", "Sofia Hernández", "Miguel Ángel Torres", 
                "Laura Fernández", "Diego Ramírez", "Carmen Castro"
            };

            var members = memberNames.Select((name, i) => Member.Create(
                name,
                $"photo-{i + 1}.webp",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30 + i * 10))
            )).ToList();

            await context.Members.AddRangeAsync(members, ct);
        }

        await context.SaveChangesAsync(ct);
    }
}