using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;

using GymFlow.Application.Common;

namespace GymFlow.Application.UseCases;

public class GetSalesUseCase
{
    private readonly ISaleRepository _saleRepository;

    public GetSalesUseCase(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<Result<PagedResult<SaleDto>>> ExecuteAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (items, total) = await _saleRepository.GetAllPagedAsync(page, pageSize, ct);
        var dtos = items.Select(s => s.ToDto()).ToList();

        return Result<PagedResult<SaleDto>>.Success(
            new PagedResult<SaleDto>(dtos, total, page, pageSize));
    }
}