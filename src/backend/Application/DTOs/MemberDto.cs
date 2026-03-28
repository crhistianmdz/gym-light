using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs;

/// <summary>
/// Datos de un socio expuestos hacia el cliente (UI y caché local).
/// Incluye PhotoWebPUrl para verificación visual en check-in offline (HU-01 CA-2).
/// </summary>
public record MemberDto(
    Guid Id,
    string FullName,
    string PhotoWebPUrl,
    MemberStatus Status,
    DateOnly MembershipEndDate
);
