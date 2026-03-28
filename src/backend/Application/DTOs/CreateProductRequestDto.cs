namespace GymFlow.Application.DTOs;

public record CreateProductRequestDto(
    string? Sku,
    string Name,
    string? Description,
    decimal Price,
    int InitialStock
);