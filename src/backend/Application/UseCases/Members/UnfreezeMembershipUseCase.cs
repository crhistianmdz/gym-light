using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Domain.Enums;
using GymFlow.Domain.Interfaces;

namespace GymFlow.Application.UseCases.Members;

/// <summary>
/// Caso de uso: Descongelar la membresía de un socio.
///
/// Flujo:
///   1. Verificar que el socio existe y está Frozen.
///   2. Obtener el congelamiento activo.
///   3. Llamar member.Unfreeze() — restaura status a Active.
///   4. Eliminar el registro de congelamiento activo.
///   5. Persistir cambios.
///
/// NOTA: El MembershipEndDate ya fue extendido al congelar y NO se revierte al descongelar.
/// Esto es intencional: el socio "pagó" por los días de congelamiento con la extensión.
/// </summary>
public class UnfreezeMembershipUseCase
{
    private readonly IMemberRepository _memberRepo;
    private readonly IMembershipFreezeRepository _freezeRepo;

    public UnfreezeMembershipUseCase(
        IMemberRepository memberRepo,
        IMembershipFreezeRepository freezeRepo)
    {
        _memberRepo = memberRepo;
        _freezeRepo = freezeRepo;
    }

    public async Task<Result<MemberDto>> ExecuteAsync(
        Guid memberId,
        CancellationToken ct = default)
    {
        // 1. Verificar que el socio existe
        var member = await _memberRepo.GetByIdAsync(memberId, ct);
        if (member is null)
            return Result<MemberDto>.NotFound(
                $"Socio con Id '{memberId}' no encontrado.");

        if (member.Status != MemberStatus.Frozen)
            return Result<MemberDto>.ValidationError(
                $"El socio no está congelado. Estado actual: '{member.Status}'.");

        // 2. Obtener congelamiento activo (puede no existir si el dato está inconsistente)
        var activeFreeze = await _freezeRepo.GetActiveAsync(memberId, ct);

        // 3. Descongelar
        try
        {
            member.Unfreeze();
        }
        catch (InvalidOperationException ex)
        {
            return Result<MemberDto>.ValidationError(ex.Message);
        }

        // 4. Eliminar registro de congelamiento activo (si existe)
        if (activeFreeze is not null)
            await _freezeRepo.DeleteAsync(activeFreeze, ct);

        // 5. Persistir
        await _memberRepo.UpdateAsync(member, ct);

        return Result<MemberDto>.Success(new MemberDto(
            Id: member.Id,
            FullName: member.FullName,
            PhotoWebPUrl: member.PhotoWebPUrl,
            Status: member.Status,
            MembershipEndDate: member.MembershipEndDate,
            AutoRenewEnabled: member.AutoRenewEnabled,
            CancelledAt: member.CancelledAt));
    }
}
