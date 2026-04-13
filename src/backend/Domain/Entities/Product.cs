using System;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Entidad que representa un producto vendible en el gimnasio (ej. snacks, bebidas).
/// </summary>
public class Product
{
    public Guid Id { get; private set; }
    public string? Sku { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public int InitialStock { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Constructor sin parámetros requerido por EF Core
    private Product() { }

    public static Product Create(string name, decimal price, int initialStock, string? sku = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del producto es obligatorio.", nameof(name));

        if (price <= 0)
            throw new ArgumentException("El precio debe ser positivo.", nameof(price));

        if (initialStock < 0)
            throw new ArgumentException("El stock inicial no puede ser negativo.", nameof(initialStock));

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            InitialStock = initialStock,
            Stock = initialStock,
            Sku = sku,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void AdjustStock(int quantity)
    {
        if (Stock + quantity < 0)
            throw new InvalidOperationException("El stock no puede ser negativo.");

        Stock += quantity;
        UpdatedAt = DateTime.UtcNow;
        }

    public void Update(string? sku, string name, string? description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del producto es obligatorio.", nameof(name));

        if (price <= 0)
            throw new ArgumentException("El precio debe ser positivo.", nameof(price));

        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
        UpdatedAt = DateTime.UtcNow;
    }
}