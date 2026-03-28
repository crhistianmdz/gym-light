namespace GymFlow.Domain.Enums;

/// <summary>
/// Estado de la membresía de un socio.
/// Regla de negocio (PRD §4.1): Frozen y Expired bloquean el acceso.
/// </summary>
public enum MemberStatus
{
    Active,
    Frozen,
    Expired
}
