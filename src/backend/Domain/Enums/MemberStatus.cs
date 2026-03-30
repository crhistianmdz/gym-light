namespace GymFlow.Domain.Enums;

/// <summary>
/// Estado de la membresía de un socio.
/// Regla de negocio (PRD §4.1): Frozen y Expired bloquean el acceso.
/// </summary>
public enum MemberStatus
{
    Active    = 0,
    Frozen    = 1,
    Expired   = 2,
    Cancelled = 3
}
