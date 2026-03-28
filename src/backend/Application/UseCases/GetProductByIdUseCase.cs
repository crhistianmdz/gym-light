namespace GymFlow.Application.UseCases;

using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Domain.Interfaces;

public class GetProductByIdUseCase
{
    private readonly IProductRepository _productRepository;

    public GetProductByIdUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _productRepository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ProductDto>.NotFound("Producto no encontrado.");

        var dto = new ProductDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.InitialStock,
            IsLowStock: product.Stock <= product.InitialStock * 0.20
        );
        
        return Result<ProductDto>.Success(dto);
    }
}