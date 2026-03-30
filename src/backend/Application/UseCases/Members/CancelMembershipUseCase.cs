using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Domain.Interfaces;

namespace GymFlow.Application.UseCases.Members;

public class CancelMembershipUseCase
{
    private readonly IMemberRepository _memberRepo;

    public CancelMembershipUseCase(IMemberRepository memberRepo)
    {
        _memberRepo = memberRepo;
    }

    public async Task<Result<MemberDto>> ExecuteAsync(
        Guid memberId,
        Guid callerId,
        string callerRole,
        CancelMembershipRequestDto request,
        CancellationToken ct)
    {
        // 1. Self-check RBAC
        bool isSelf = callerId == memberId;
        bool isAdminOrOwner = callerRole is "Admin" or "Owner";
        if (!isSelf && !isAdminOrOwner)
        {
            return Result<MemberDto>.Forbidden("No tiene permiso.");
        }

        // 2. Member existence
        var member = await _memberRepo.GetByIdAsync(memberId, ct);
        if (member is null)
        {
            return Result<MemberDto>.NotFound($"Socio '{memberId}' no encontrado.");
        }

        // 3. Idempotency
        if (!member.AutoRenewEnabled && member.CancelledAt.HasValue)
        {
            return Result<MemberDto>.Success(ToDto(member));
        }

        // 4. Domain cancellation
        try
        {
            member.Cancel();
        }
        catch (DomainException ex)
        {
            return Result<MemberDto>.ValidationError(ex.Message);
        }

        // 5. Persist member update
        await _memberRepo.UpdateAsync(member, ct);
        return Result<MemberDto>.Success(ToDto(member));
    }

    private static MemberDto ToDto(Member member) => new(
        member.Id,
        member.FullName,
        member.PhotoWebPUrl,
        member.Status,
        member.MembershipEndDate,
        member.AutoRenewEnabled,
        member.CancelledAt
    );
}