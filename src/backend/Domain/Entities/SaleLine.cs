using System;
namespace GymFlow.Domain.Entities;

/// <summary>
/// Representa una línea de detalle dentro de una venta.
/// </summary>
public class SaleLine
{
    public Guid Id { get; private set; }
    public Guid SaleId { get; private set; }
    public Sale Sale { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Subtotal => Quantity * UnitPrice;

    private SaleLine() { }

    public static SaleLine Create(Guid productId, Product product, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("La cantidad debe ser positiva.", nameof(quantity));

        if (unitPrice <= 0)
            throw new ArgumentException("El precio unitario debe ser positivo.", nameof(unitPrice));

        return new SaleLine
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Product = product,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}