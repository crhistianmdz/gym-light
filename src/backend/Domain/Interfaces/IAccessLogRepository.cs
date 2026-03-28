using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Interfaces;

/// <summary>
/// Contrato de repositorio para AccessLog.
/// ClientGuidExistsAsync es la clave de la idempotencia (RFC §4).
/// </summary>
public interface IAccessLogRepository
{
    /// <summary>
    /// Verifica si un ClientGuid ya fue procesado — corazón del IdempotencyFilter.
    /// </summary>
    Task<bool> ClientGuidExistsAsync(Guid clientGuid, CancellationToken ct = default);

    Task<AccessLog?> GetByClientGuidAsync(Guid clientGuid, CancellationToken ct = default);
    Task AddAsync(AccessLog accessLog, CancellationToken ct = default);
}
