using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Interfaces;

/// <summary>
/// Contrato de repositorio para la entidad MembershipFreeze.
/// Implementación en Infrastructure/Persistence.
/// </summary>
public interface IMembershipFreezeRepository
{
    /// <summary>
    /// Retorna todos los congelamientos de un socio en un año calendario específico.
    /// Usado para validar la regla HU-07: máximo 4 por año.
    /// </summary>
    Task<IReadOnlyList<MembershipFreeze>> GetByMemberAndYearAsync(
        Guid memberId,
        int year,
        CancellationToken ct = default);

    /// <summary>
    /// Retorna el historial completo de congelamientos de un socio.
    /// </summary>
    Task<IReadOnlyList<MembershipFreeze>> GetByMemberAsync(
        Guid memberId,
        CancellationToken ct = default);

    /// <summary>
    /// Retorna el congelamiento activo de un socio (si existe).
    /// Un congelamiento es activo si StartDate &lt;= hoy &lt;= EndDate.
    /// </summary>
    Task<MembershipFreeze?> GetActiveAsync(
        Guid memberId,
        CancellationToken ct = default);

    Task AddAsync(MembershipFreeze freeze, CancellationToken ct = default);
    Task DeleteAsync(MembershipFreeze freeze, CancellationToken ct = default);
}
