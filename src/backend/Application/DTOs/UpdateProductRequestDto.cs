namespace GymFlow.Application.DTOs;

// Stock no se modifica directamente
public record UpdateProductRequestDto(
    string? Sku,
    string Name,
    string? Description,
    decimal Price
);