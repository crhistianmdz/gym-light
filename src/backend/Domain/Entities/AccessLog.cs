namespace GymFlow.Domain.Entities;

/// <summary>
/// Registro de cada intento de acceso al gimnasio.
/// ClientGuid garantiza idempotencia en la sincronización offline (PRD §4.4, RFC §4).
/// </summary>
public class AccessLog
{
    public Guid Id { get; private set; }
    public Guid MemberId { get; private set; }
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// UUID v4 generado en el cliente. Usado por IdempotencyFilter para detectar duplicados.
    /// </summary>
    public Guid ClientGuid { get; private set; }

    /// <summary>
    /// Indica si el check-in fue registrado localmente por falta de red.
    /// </summary>
    public bool IsOffline { get; private set; }

    /// <summary>
    /// Usuario (Recepcionista/Admin) que ejecutó el check-in (PRD §4 — Auditoría HU-06).
    /// </summary>
    public Guid PerformedByUserId { get; private set; }

    public bool WasAllowed { get; private set; }
    public string? DenialReason { get; private set; }

    // Navegación EF Core
    public Member Member { get; private set; } = null!;

    // EF Core constructor
    private AccessLog() { }

    public static AccessLog Create(
        Guid memberId,
        Guid clientGuid,
        Guid performedByUserId,
        bool wasAllowed,
        bool isOffline = false,
        string? denialReason = null)
    {
        return new AccessLog
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            ClientGuid = clientGuid,
            PerformedByUserId = performedByUserId,
            WasAllowed = wasAllowed,
            IsOffline = isOffline,
            DenialReason = denialReason,
            Timestamp = DateTime.UtcNow
        };
    }
}
