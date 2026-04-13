using GymFlow.Application.DTOs.Metrics;
using GymFlow.Domain.Interfaces;

using GymFlow.Application.Common;

namespace GymFlow.Application.UseCases.Admin;
using GymFlow.Domain.Enums;

public class GetIncomeReportUseCase
{
    private readonly IPaymentRepository _paymentRepository;

    public GetIncomeReportUseCase(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<IncomeReportDto>> ExecuteAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (from > to)
        {
            return Result<IncomeReportDto>.ValidationError("El rango de fechas es inválido. El valor 'from' debe ser anterior o igual a 'to'.");
        }

        var rows = await _paymentRepository.GetMonthlyIncomeAsync(from, to, ct);

        // Pivot rows into MonthlyBreakdownDto
        var byMonth = rows
            .GroupBy(r => (r.Year, r.Month))
            .Select(group => new MonthlyBreakdownDto
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                Membership = group.Where(g => g.Category == PaymentCategory.Membership).Sum(g => g.Total),
                Pos = group.Where(g => g.Category == PaymentCategory.POS).Sum(g => g.Total),
                Total = group.Sum(g => g.Total)
            })
            .OrderBy(b => (b.Year, b.Month))
            .ToList();

        var totalIncome = byMonth.Sum(m => m.Total);

        return Result<IncomeReportDto>.Success(new IncomeReportDto
        {
            From = from.ToString("yyyy-MM-dd"),
            To = to.ToString("yyyy-MM-dd"),
            TotalIncome = totalIncome,
            ByMonth = byMonth
        });
    }
}