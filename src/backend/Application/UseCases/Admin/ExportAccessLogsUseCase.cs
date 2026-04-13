using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Application.Common;
using GymFlow.Domain.Interfaces;
using System.Text;
using GymFlow.Domain.Models;

namespace GymFlow.Application.UseCases.Admin;

public class ExportAccessLogsUseCase
{
    private readonly IAccessLogRepository _accessLogRepository;

    public ExportAccessLogsUseCase(IAccessLogRepository accessLogRepository)
    {
        _accessLogRepository = accessLogRepository;
    }

    public async Task<Result<byte[]>> ExecuteAsync(
        AccessLogFilterDto filterDto, string format,
        CancellationToken ct = default)
    {
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return Result<byte[]>.Failure("PDF export not yet supported", 501);
        }

        var filter = new AccessLogFilter
        {
            FromDate = filterDto.FromDate,
            ToDate = filterDto.ToDate,
            PerformedByUserId = filterDto.PerformedByUserId,
            MemberId = filterDto.MemberId,
            Result = filterDto.Result
        };

        var (items, _) = await _accessLogRepository.GetPagedAsync(
            filter, filterDto.Page, filterDto.PageSize, ct);

        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("Id,MemberId,MemberName,PerformedByUserId,PerformedByUserName,Result,DenialReason,CreatedAt,ClientGuid");

        foreach (var item in items)
        {
            csvBuilder.AppendLine($"{item.Id},{item.MemberId},Unknown,{item.PerformedByUserId},Unknown,{(item.WasAllowed ? "Allowed" : "Denied")},{item.DenialReason},{item.Timestamp:O},{item.ClientGuid}");
        }

        return Result<byte[]>.Success(Encoding.UTF8.GetBytes(csvBuilder.ToString()));
    }
}