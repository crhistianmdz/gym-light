using System;
using System.Collections.Generic;
using System.Linq;
namespace GymFlow.Domain.Entities;

/// <summary>
/// Entidad que representa una venta realizada en el gimnasio.
/// </summary>
public class Sale
{
    public Guid Id { get; private set; }
    public Guid ClientGuid { get; private set; } // Idempotencia (UUID v4 generado client-side)
    public Guid PerformedByUserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public SaleStatus Status { get; private set; } = SaleStatus.Active;
    public decimal Total { get; private set; }
    public ICollection<SaleLine> Lines { get; private set; } = new List<SaleLine>();

    private Sale() { }

    public static Sale Create(Guid performedByUserId, Guid clientGuid, IEnumerable<SaleLine> saleLines)
    {
        if (saleLines == null)
            throw new ArgumentException("La venta debe tener líneas de detalle.", nameof(saleLines));

        var lines = new List<SaleLine>(saleLines);
        if (lines.Count == 0)
            throw new ArgumentException("La venta debe incluir al menos un producto.", nameof(saleLines));

        return new Sale
        {
            Id = Guid.NewGuid(),
            PerformedByUserId = performedByUserId,
            ClientGuid = clientGuid,
            Timestamp = DateTime.UtcNow,
            Total = CalculateTotal(lines),
            Lines = lines
        };
    }

    public static Sale Create(Guid performedByUserId, Guid clientGuid, DateTime timestamp)
    {
        return new Sale
        {
            Id = Guid.NewGuid(),
            PerformedByUserId = performedByUserId,
            ClientGuid = clientGuid,
            Timestamp = timestamp,
            Status = SaleStatus.Pending,
            Total = 0,
            Lines = new List<SaleLine>()
        };
    }

    public SaleLine AddLine(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        var line = SaleLine.Create(productId, productName, quantity, unitPrice);
        Lines.Add(line);
        return line;
    }

    public void Complete(decimal total)
    {
        Total = total;
        Status = SaleStatus.Completed;
    }

    public SaleDto ToDto()
    {
        return new SaleDto(
            Id,
            ClientGuid,
            PerformedByUserId,
            Timestamp,
            Status.ToString(),
            Total,
            Lines.Select(l => new SaleLineDto(l.Id, l.ProductId, l.Product.Name, l.Quantity, l.UnitPrice, l.Subtotal)).ToList()
        );
    }

    private static decimal CalculateTotal(IEnumerable<SaleLine> saleLines)
    {
        decimal total = 0;
        foreach (var line in saleLines)
        {
            total += line.Subtotal;
        }
        return total;
    }

    public void Cancel()
    {
        if (Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("La venta ya está cancelada.");

        Status = SaleStatus.Cancelled;
    }
}