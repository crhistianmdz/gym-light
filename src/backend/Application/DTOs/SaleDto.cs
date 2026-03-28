namespace GymFlow.Application.DTOs;

public record SaleDto(
    Guid Id,
    Guid ClientGuid,
    Guid PerformedByUserId,
    DateTime Timestamp,
    string Status,
    decimal Total,
    List<SaleLineDto> Lines
);