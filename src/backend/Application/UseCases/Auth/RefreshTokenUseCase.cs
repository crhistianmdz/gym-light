namespace GymFlow.Application.UseCases.Auth;

using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;

public class RefreshTokenUseCase(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService)
{
    public async Task<Result<AuthResponseDto>> ExecuteAsync(
        string rawRefreshToken,
        CancellationToken ct = default)
    {
        var tokenHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawRefreshToken)));

        var storedToken = await refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);
        if (storedToken is null || !storedToken.IsActive())
            return Result<AuthResponseDto>.ValidationError("Refresh token inválido o expirado.");

        var user = await userRepository.GetByIdAsync(storedToken.UserId, ct);
        if (user is null || !user.IsActive)
            return Result<AuthResponseDto>.ValidationError("Usuario no encontrado o inactivo.");

        await refreshTokenRepository.RevokeAsync(storedToken, ct);

        var newAccessToken = tokenService.GenerateAccessToken(user);
        var newRawRefresh  = tokenService.GenerateRefreshToken();
        var newHash        = Convert.ToHexString(
                                 System.Security.Cryptography.SHA256.HashData(
                                     System.Text.Encoding.UTF8.GetBytes(newRawRefresh)));

        var newRefreshToken = RefreshToken.Create(
            userId:    user.Id,
            tokenHash: newHash,
            expiresAt: DateTime.UtcNow.AddDays(7));

        await refreshTokenRepository.AddAsync(newRefreshToken, ct);

        var dto = new AuthResponseDto(
            AccessToken: newAccessToken,
            UserId:      user.Id,
            FullName:    user.FullName,
            Role:        user.Role.ToString(),
            ExpiresAt:   DateTime.UtcNow.AddMinutes(15));

        return Result<AuthResponseDto>.SuccessWithExtra(dto, newRawRefresh);
    }
}