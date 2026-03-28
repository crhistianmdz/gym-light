namespace GymFlow.Application.UseCases;

using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;

public class CreateSaleUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ISaleRepository _saleRepository;

    public CreateSaleUseCase(
        IProductRepository productRepository,
        ISaleRepository saleRepository)
    {
        _productRepository = productRepository;
        _saleRepository = saleRepository;
    }

    public async Task<Result<SaleDto>> ExecuteAsync(CreateSaleRequestDto request, CancellationToken ct = default)
    {
        if (!request.Lines.Any())
            return Result<SaleDto>.ValidationError("Debe incluir al menos un producto.");

        var sale = Sale.Create(
            request.ClientGuid,
            request.PerformedByUserId,
            DateTime.UtcNow
        );

        decimal total = 0;
        foreach (var lineRequest in request.Lines)
        {
            var product = await _productRepository.GetByIdAsync(lineRequest.ProductId, ct);
            if (product is null)
                return Result<SaleDto>.NotFound($"Producto (ID: {lineRequest.ProductId}) no encontrado.");

            if (product.Stock < lineRequest.Quantity)
                return Result<SaleDto>.ValidationError($"Stock insuficiente para el producto: {product.Name}.");

            var line = sale.AddLine(
                product.Id,
                product.Name,
                lineRequest.Quantity,
                product.Price
            );

            total += line.Subtotal;
            product.Stock -= line.Quantity;

            await _productRepository.UpdateAsync(product, ct);
        }

        sale.Complete(total);
        await _saleRepository.AddAsync(sale, ct);

        var dto = sale.ToDto();
        return Result<SaleDto>.Success(dto);
    }
}