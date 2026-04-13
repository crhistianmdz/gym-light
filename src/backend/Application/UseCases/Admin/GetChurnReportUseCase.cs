using GymFlow.Application.DTOs.Metrics;
using GymFlow.Domain.Interfaces;

using GymFlow.Application.Common;

namespace GymFlow.Application.UseCases.Admin;

public class GetChurnReportUseCase
{
    private readonly IMemberRepository _memberRepository;

    public GetChurnReportUseCase(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<Result<ChurnReportDto>> ExecuteAsync(int year, CancellationToken ct = default)
    {
        if (year < 2020 || year > DateTime.UtcNow.Year)
        {
            return Result<ChurnReportDto>.ValidationError("El año debe estar entre 2020 y el año actual.");
        }

        var (totalMembers, activeMembers, notRenewed) = 
            await _memberRepository.GetChurnStatsAsync(year, ct);

        var churnRate = totalMembers > 0 ? (double)notRenewed / totalMembers * 100 : 0;

        return Result<ChurnReportDto>.Success(new ChurnReportDto
        {
            Year = year,
            TotalMembers = totalMembers,
            ActiveMembers = activeMembers,
            NotRenewed = notRenewed,
            ChurnRate = churnRate
        });
    }
}