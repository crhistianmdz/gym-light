namespace GymFlow.Application.DTOs.Metrics;

public record PaymentDto(
    Guid Id,
    Guid? MemberId,
    decimal Amount,
    string Category,
    DateTime Timestamp,
    Guid CreatedByUserId,
    string? Notes,
    Guid? SaleId
);