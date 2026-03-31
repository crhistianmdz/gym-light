namespace GymFlow.Application.DTOs.Metrics;

public record RegisterPaymentRequest(
    Guid? MemberId,
    decimal Amount,
    string Category,
    Guid ClientGuid,
    Guid CreatedByUserId,
    string? Notes,
    Guid? SaleId
);
