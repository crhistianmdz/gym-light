namespace GymFlow.Application.DTOs;

public record SaleLineRequestDto(
    Guid ProductId,
    int Quantity
);