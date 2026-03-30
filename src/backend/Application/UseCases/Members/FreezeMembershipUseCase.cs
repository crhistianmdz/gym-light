using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Application.Validators;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Domain.Interfaces;

namespace GymFlow.Application.UseCases.Members;

/// <summary>
/// Caso de uso: Congelar la membresía de un socio.
///
/// Flujo:
///   1. Validar inputs (FreezeMembershipValidator).
///   2. Verificar que el socio existe y está Active.
///   3. Verificar regla HU-07 R1: máximo 4 congelamientos por año calendario.
///   4. Crear entidad MembershipFreeze (valida mínimo 7 días).
///   5. Aplicar Freeze sobre el Member (extiende EndDate, cambia status a Frozen).
///   6. Persistir ambos cambios.
///   7. Retornar MembershipFreezeDto.
///
/// Solo Admin y Owner pueden invocar este use case (validado en el Controller con [Authorize]).
/// </summary>
public class FreezeMembershipUseCase
{
    private readonly IMemberRepository _memberRepo;
    private readonly IMembershipFreezeRepository _freezeRepo;

    public FreezeMembershipUseCase(
        IMemberRepository memberRepo,
        IMembershipFreezeRepository freezeRepo)
    {
        _memberRepo = memberRepo;
        _freezeRepo = freezeRepo;
    }

    public async Task<Result<MembershipFreezeDto>> ExecuteAsync(
        FreezeMembershipDto dto,
        Guid createdByUserId,
        CancellationToken ct = default)
    {
        // 1. Validar inputs
        var validation = FreezeMembershipValidator.Validate(dto);
        if (!validation.IsValid)
            return Result<MembershipFreezeDto>.ValidationError(
                string.Join(" | ", validation.Errors));

        // 2. Verificar que el socio existe
        var member = await _memberRepo.GetByIdAsync(dto.MemberId, ct);
        if (member is null)
            return Result<MembershipFreezeDto>.NotFound(
                $"Socio con Id '{dto.MemberId}' no encontrado.");

        // Solo se puede congelar un socio Active
        if (member.Status != MemberStatus.Active)
            return Result<MembershipFreezeDto>.ValidationError(
                $"No se puede congelar un socio con estado '{member.Status}'. " +
                "Solo se pueden congelar socios con membresía activa.");

        // 3. Verificar regla HU-07 R1: máximo 4 congelamientos por año
        var currentYear = dto.StartDate.Year;
        var freezesThisYear = await _freezeRepo.GetByMemberAndYearAsync(dto.MemberId, currentYear, ct);

        if (!member.CanFreezeThisYear(freezesThisYear.Count))
            return Result<MembershipFreezeDto>.ValidationError(
                $"El socio ya alcanzó el límite de 4 congelamientos para el año {currentYear}. " +
                $"Congelamientos registrados: {freezesThisYear.Count}.");

        // 4. Crear entidad MembershipFreeze (valida mínimo 7 días internamente)
        MembershipFreeze freeze;
        try
        {
            freeze = MembershipFreeze.Create(
                dto.MemberId,
                dto.StartDate,
                dto.EndDate,
                createdByUserId);
        }
        catch (ArgumentException ex)
        {
            return Result<MembershipFreezeDto>.ValidationError(ex.Message);
        }

        // 5. Aplicar congelamiento sobre el Member
        member.Freeze(freeze.DurationDays);

        // 6. Persistir
        await _freezeRepo.AddAsync(freeze, ct);
        await _memberRepo.UpdateAsync(member, ct);

        // 7. Retornar DTO
        return Result<MembershipFreezeDto>.Success(ToDto(freeze));
    }

    private static MembershipFreezeDto ToDto(MembershipFreeze f) =>
        new(f.Id, f.MemberId, f.StartDate, f.EndDate, f.DurationDays, f.CreatedByUserId, f.CreatedAt);
}
