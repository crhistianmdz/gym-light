namespace GymFlow.Application.UseCases.Auth;

using GymFlow.Application.Common;
using GymFlow.Domain.Interfaces;

public class LogoutUseCase(IRefreshTokenRepository refreshTokenRepository)
{
    public async Task<Result<bool>> ExecuteAsync(
        string rawRefreshToken,
        CancellationToken ct = default)
    {
        var tokenHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawRefreshToken)));

        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);
        if (storedToken is not null && storedToken.IsActive())
            await refreshTokenRepository.RevokeAsync(storedToken, ct);

        return Result<bool>.Success(true);
    }
}