namespace GymFlow.Application.UseCases;

using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;

public class CreateProductUseCase
{
    private readonly IProductRepository _productRepository;

    public CreateProductUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductDto>> ExecuteAsync(CreateProductRequestDto request, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(request.Sku))
        {
            var existing = await _productRepository.GetBySkuAsync(request.Sku, ct);
            if (existing is not null)
                return Result<ProductDto>.Conflict("El SKU ya existe.");
        }

        var product = Product.Create(
            request.Sku,
            request.Name,
            request.Description,
            request.Price,
            initialStock: request.InitialStock
        );

        await _productRepository.AddAsync(product, ct);

        var dto = new ProductDto(
            Id: product.Id,
            Sku: product.Sku,
            Name: product.Name,
            Description: product.Description,
            Price: product.Price,
            Stock: product.Stock,
            InitialStock: product.InitialStock,
            IsLowStock: false
        );

        return Result<ProductDto>.Success(dto);
    }
}