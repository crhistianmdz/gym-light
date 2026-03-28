namespace GymFlow.Application.UseCases;

using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Domain.Interfaces;

public class CancelSaleUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ISaleRepository _saleRepository;

    public CancelSaleUseCase(
        IProductRepository productRepository,
        ISaleRepository saleRepository)
    {
        _productRepository = productRepository;
        _saleRepository = saleRepository;
    }

    public async Task<Result<SaleDto>> ExecuteAsync(Guid saleId, CancellationToken ct = default)
    {
        var sale = await _saleRepository.GetByIdAsync(saleId, ct);
        if (sale is null)
            return Result<SaleDto>.NotFound("Venta no encontrada.");

        if (sale.Status != "Active")
            return Result<SaleDto>.ValidationError("La venta ya fue cancelada.");

        foreach (var line in sale.Lines)
        {
            var product = await _productRepository.GetByIdAsync(line.ProductId, ct);
            if (product is null)
                continue;

            product.Stock += line.Quantity;
            await _productRepository.UpdateAsync(product, ct);
        }

        sale.Cancel();
        await _saleRepository.UpdateAsync(sale, ct);

        var dto = sale.ToDto();
        return Result<SaleDto>.Success(dto);
    }
}