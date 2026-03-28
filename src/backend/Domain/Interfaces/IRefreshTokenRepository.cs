namespace GymFlow.Domain.Interfaces;

using GymFlow.Domain.Entities;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeAsync(RefreshToken token, CancellationToken ct = default);
}