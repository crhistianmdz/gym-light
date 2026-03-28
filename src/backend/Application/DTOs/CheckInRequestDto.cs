namespace GymFlow.Application.DTOs;

/// <summary>
/// Request de check-in enviado por la recepción.
/// ClientGuid es generado en el cliente (UUID v4) para garantizar idempotencia (PRD §4.4).
/// </summary>
public record CheckInRequestDto(
    Guid MemberId,

    /// <summary>
    /// UUID v4 generado en el frontend. Enviado también en el header X-Client-Guid.
    /// </summary>
    Guid ClientGuid,

    /// <summary>
    /// ID del recepcionista autenticado que ejecuta el check-in (trazabilidad HU-06).
    /// </summary>
    Guid PerformedByUserId
);
