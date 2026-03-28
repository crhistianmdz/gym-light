namespace GymFlow.Application.DTOs;

/// <summary>
/// Resultado de validación de acceso retornado al cliente.
/// Si Allowed es false, DenialReason explica el motivo (UI feedback).
/// </summary>
public record AccessValidationDto(
    bool Allowed,
    MemberDto? Member,
    string? DenialReason
);
