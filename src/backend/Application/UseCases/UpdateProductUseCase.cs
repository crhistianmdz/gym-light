namespace GymFlow.Application.UseCases;

using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Domain.Interfaces;

public class UpdateProductUseCase
{
    private readonly IProductRepository _productRepository;

    public UpdateProductUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(Guid id, UpdateProductRequestDto request, CancellationToken ct = default)
    {
        var product = await _productRepository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<ProductDto>.NotFound("Producto no encontrado.");

        product.UpdateDetails(
            request.Sku,
            request.Name,
            request.Description,
            request.Price
        );

        await _productRepository.UpdateAsync(product, ct);
        
        var dto = new ProductDto(
            Id: product.Id,
            Sku: product.Sku,
            Name: product.Name,
            Description: product.Description,
            Price: product.Price,
            Stock: product.Stock,
            InitialStock: product.InitialStock,
            IsLowStock: product.Stock <= product.InitialStock * 0.20
        );

        return Result<ProductDto>.Success(dto);
    }
}