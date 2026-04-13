using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Interfaces;

/// <summary>
/// Contrato de repositorio para la entidad Member.
/// Implementación en Infrastructure/Persistence.
/// </summary>
public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(Guid memberId, CancellationToken ct = default);
    Task<IReadOnlyList<Member>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(Member member, CancellationToken ct = default);
    Task UpdateAsync(Member member, CancellationToken ct = default);
    Task<(int TotalMembers, int ActiveMembers, int NotRenewed)> GetChurnStatsAsync(int year, CancellationToken ct = default);
}