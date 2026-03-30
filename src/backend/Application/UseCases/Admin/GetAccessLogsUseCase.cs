using GymFlow.Application.DTOs;
using GymFlow.Domain.Interfaces;

namespace GymFlow.Application.UseCases.Admin;

public class GetAccessLogsUseCase
{
    private readonly IAccessLogRepository _accessLogRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IUserRepository _userRepository;

    public GetAccessLogsUseCase(
        IAccessLogRepository accessLogRepository,
        IMemberRepository memberRepository,
        IUserRepository userRepository)
    {
        _accessLogRepository = accessLogRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<PagedResultDto<AccessLogDto>>> ExecuteAsync(
        AccessLogFilterDto filterDto,
        CancellationToken ct = default)
    {
        var filter = new AccessLogFilter
        {
            FromDate = filterDto.FromDate,
            ToDate = filterDto.ToDate,
            PerformedByUserId = filterDto.PerformedByUserId,
            MemberId = filterDto.MemberId,
            Result = filterDto.Result
        };

        var (items, totalCount) = await _accessLogRepository.GetPagedAsync(
            filter, filterDto.Page, filterDto.PageSize, ct);

        var accessLogDtos = new List<AccessLogDto>();
        foreach (var item in items)
        {
            var member = await _memberRepository.GetByIdAsync(item.MemberId, ct);
            var user = await _userRepository.GetByIdAsync(item.PerformedByUserId, ct);

            accessLogDtos.Add(new AccessLogDto
            {
                Id = item.Id,
                MemberId = item.MemberId,
                MemberName = member?.FullName ?? "Unknown",
                PerformedByUserId = item.PerformedByUserId,
                PerformedByUserName = user?.FullName ?? "Unknown",
                Result = item.WasAllowed ? "Allowed" : "Denied",
                DenialReason = item.DenialReason,
                CreatedAt = item.Timestamp,
                ClientGuid = item.ClientGuid
            });
        }

        var result = new PagedResultDto<AccessLogDto>
        {
            Items = accessLogDtos,
            TotalCount = totalCount,
            Page = filterDto.Page,
            PageSize = filterDto.PageSize
        };

        return Result<PagedResultDto<AccessLogDto>>.Success(result);
    }
}