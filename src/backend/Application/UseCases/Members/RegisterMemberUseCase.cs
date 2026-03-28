using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Application.Validators;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;

namespace GymFlow.Application.UseCases.Members;

/// <summary>
/// Caso de uso: Registrar un nuevo socio en el sistema.
///
/// Flujo:
///   1. Validar DTO (nombre, foto WebP obligatoria, fecha futura)
///   2. Persistir la foto WebP y obtener la URL pública
///   3. Crear la entidad Member (el factory ya valida foto no vacía)
///   4. Guardar en base de datos
///   5. Retornar MemberDto para que el frontend actualice IndexedDB
///
/// Reglas de negocio aplicadas:
///   - HU-02 CA-1: sin foto → error 400, no se llega a crear el Member
///   - HU-02 CA-2: el backend valida que el base64 sea WebP, no JPG/PNG
///   - PRD §4.2: foto es requisito técnico para habilitar check-in posterior
/// </summary>
public class RegisterMemberUseCase
{
    private readonly IMemberRepository _members;
    private readonly IPhotoStorageService _photoStorage;

    public RegisterMemberUseCase(
        IMemberRepository members,
        IPhotoStorageService photoStorage)
    {
        _members = members;
        _photoStorage = photoStorage;
    }

    public async Task<Result<MemberDto>> ExecuteAsync(
        CreateMemberDto request,
        CancellationToken ct = default)
    {
        // ── Paso 1: Validar ────────────────────────────────────────────────────
        var validation = CreateMemberValidator.Validate(request);
        if (!validation.IsValid)
            return Result<MemberDto>.ValidationError(string.Join(" | ", validation.Errors));

        // ── Paso 2: Persistir foto ─────────────────────────────────────────────
        // El memberId se genera aquí para usarlo como nombre de archivo
        var memberId = Guid.NewGuid();
        string photoUrl;

        try
        {
            photoUrl = await _photoStorage.SavePhotoAsync(request.PhotoWebPBase64, memberId, ct);
        }
        catch (Exception ex)
        {
            return Result<MemberDto>.InternalError($"No se pudo guardar la foto: {ex.Message}");
        }

        // ── Paso 3: Crear entidad Member ───────────────────────────────────────
        // Member.Create() re-valida que photoUrl no esté vacía (doble seguridad)
        var member = Member.Create(
            fullName: request.FullName.Trim(),
            photoWebPUrl: photoUrl,
            membershipEndDate: request.MembershipEndDate
        );

        // Sobreescribir el Id generado por Member.Create() con el que ya usamos para la foto
        // Necesitamos un método en la entidad para esto (ver Member.cs - WithId)
        member = Member.CreateWithId(memberId, request.FullName.Trim(), photoUrl, request.MembershipEndDate);

        // ── Paso 4: Persistir en DB ────────────────────────────────────────────
        await _members.AddAsync(member, ct);

        // ── Paso 5: Retornar DTO ───────────────────────────────────────────────
        return Result<MemberDto>.Success(new MemberDto(
            Id: member.Id,
            FullName: member.FullName,
            PhotoWebPUrl: member.PhotoWebPUrl,
            Status: member.Status,
            MembershipEndDate: member.MembershipEndDate
        ));
    }
}
