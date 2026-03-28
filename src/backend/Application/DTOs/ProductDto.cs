namespace GymFlow.Application.DTOs;

public record ProductDto(
    Guid Id,
    string? Sku,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    int InitialStock,
    bool IsLowStock
);