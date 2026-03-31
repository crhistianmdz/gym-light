using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Payment entity representing a financial transaction, adhering to domain rules.
/// </summary>
public class Payment
{
    public Guid Id { get; private set; }
    public Guid? MemberId { get; private set; } // Nullable for POS payments
    public decimal Amount { get; private set; }
    public PaymentCategory Category { get; private set; }
    public DateTime Timestamp { get; private set; } // Stored in UTC
    public Guid CreatedByUserId { get; private set; }
    public string? Notes { get; private set; }
    public Guid? SaleId { get; private set; } // Optional FK to Sale
    public Guid ClientGuid { get; private set; } // Unique identifier for idempotence

    // EF Core constructor
    private Payment() { }

    /// <summary>
    /// Static factory method to create a Payment entity.
    /// Validates business rules.
    /// </summary>
    public static Payment Create(
        Guid? memberId,
        decimal amount,
        PaymentCategory category,
        Guid createdByUserId,
        Guid clientGuid,
        string? notes = null,
        Guid? saleId = null)
    {
        if (amount <= 0)
            throw new ArgumentException("The amount must be greater than zero.", nameof(amount));

        return new Payment
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            Amount = amount,
            Category = category,
            Timestamp = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            ClientGuid = clientGuid,
            Notes = notes,
            SaleId = saleId
        };
    }

    /// <summary>
    /// Maps this Payment entity to its DTO representation.
    /// </summary>
    public GymFlow.Application.DTOs.Metrics.PaymentDto ToDto() =>
        new(Id, MemberId, Amount, Category.ToString(), Timestamp, CreatedByUserId, Notes, SaleId);
}