namespace GymFlow.Application.UseCases;

using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Domain.Interfaces;

public class GetProductsUseCase
{
    private readonly IProductRepository _productRepository;

    public GetProductsUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<List<ProductDto>>> ExecuteAsync(CancellationToken ct = default)
    {
        var products = await _productRepository.GetAllAsync(ct);
        var dtos = products
            .Select(p => new ProductDto(
                p.Id,
                p.Sku,
                p.Name,
                p.Description,
                p.Price,
                p.Stock,
                p.InitialStock,
                IsLowStock: p.Stock <= p.InitialStock * 0.20
            ))
            .ToList();

        return Result<List<ProductDto>>.Success(dtos);
    }
}