using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;

namespace GymFlow.Application.UseCases.Access;

/// <summary>
/// Caso de uso: Validar el acceso de un socio al gimnasio.
///
/// Flujo:
///   1. Verificar idempotencia (ClientGuid ya procesado → devolver resultado original)
///   2. Buscar el socio
///   3. Evaluar si puede acceder (status + fecha)
///   4. Registrar el AccessLog
///
/// Reglas de negocio aplicadas:
///   - PRD §4.1: Frozen y Expired deniegan el acceso
///   - PRD §4.2: Foto obligatoria (Member.Create ya lo garantiza)
///   - PRD §4.4: ClientGuid para idempotencia
/// </summary>
public class ValidateAccessUseCase
{
    private readonly IMemberRepository _members;
    private readonly IAccessLogRepository _accessLogs;

    public ValidateAccessUseCase(
        IMemberRepository members,
        IAccessLogRepository accessLogs)
    {
        _members = members;
        _accessLogs = accessLogs;
    }

    public async Task<Result<AccessValidationDto>> ExecuteAsync(
        CheckInRequestDto request,
        CancellationToken ct = default)
    {
        // Guard clause: PerformedByUserId required for traceability
        if (request.PerformedByUserId == Guid.Empty || request.PerformedByUserId == default)
            return Result<AccessValidationDto>.Failure(
                "PerformedByUserId is required for check-in traceability.",
                400);

        // ── Paso 1: Idempotencia ──────────────────────────────────────────────
        var existingLog = await _accessLogs.GetByClientGuidAsync(request.ClientGuid, ct);
        if (existingLog is not null)
        {
            // Ya procesado: retornar el resultado original sin re-ejecutar lógica
            var existingMember = await _members.GetByIdAsync(existingLog.MemberId, ct);
            return Result<AccessValidationDto>.Success(new AccessValidationDto(
                Allowed: existingLog.WasAllowed,
                Member: existingMember is not null ? MapToDto(existingMember) : null,
                DenialReason: existingLog.DenialReason
            ));
        }

        // ── Paso 2: Buscar socio ──────────────────────────────────────────────
        var member = await _members.GetByIdAsync(request.MemberId, ct);
        if (member is null)
            return Result<AccessValidationDto>.NotFound($"Socio {request.MemberId} no encontrado.");

        // ── Paso 3: Evaluar acceso ────────────────────────────────────────────
        bool allowed = member.CanAccess();
        string? denialReason = allowed ? null : member.GetDenialReason();

        // ── Paso 4: Registrar AccessLog ───────────────────────────────────────
        var log = AccessLog.Create(
            memberId: member.Id,
            clientGuid: request.ClientGuid,
            performedByUserId: request.PerformedByUserId,
            wasAllowed: allowed,
            isOffline: false,
            denialReason: denialReason
        );
        await _accessLogs.AddAsync(log, ct);

        return Result<AccessValidationDto>.Success(new AccessValidationDto(
            Allowed: allowed,
            Member: MapToDto(member),
            DenialReason: denialReason
        ));
    }

    private static MemberDto MapToDto(Domain.Entities.Member member) =>
        new(
            Id: member.Id,
            FullName: member.FullName,
            PhotoWebPUrl: member.PhotoWebPUrl,
            Status: member.Status,
            MembershipEndDate: member.MembershipEndDate
        );
}
