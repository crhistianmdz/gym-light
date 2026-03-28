namespace GymFlow.Application.UseCases;

using GymFlow.Application.Common;
using GymFlow.Domain.Interfaces;

public class DeleteProductUseCase
{
    private readonly IProductRepository _productRepository;

    public DeleteProductUseCase(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<bool>> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _productRepository.GetByIdAsync(id, ct);
        if (product is null)
            return Result<bool>.NotFound("Producto no encontrado.");

        await _productRepository.DeleteAsync(product, ct);
        return Result<bool>.Success(true);
    }
}