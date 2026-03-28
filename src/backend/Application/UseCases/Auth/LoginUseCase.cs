namespace GymFlow.Application.UseCases.Auth;

using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;

public class LoginUseCase(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ITokenService tokenService,
    IPasswordHasher passwordHasher)
{
    public async Task<Result<AuthResponseDto>> ExecuteAsync(
        LoginRequestDto request,
        CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive)
            return Result<AuthResponseDto>.ValidationError("Credenciales inválidas.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResponseDto>.ValidationError("Credenciales inválidas.");

        var accessToken = tokenService.GenerateAccessToken(user);
        var rawRefresh  = tokenService.GenerateRefreshToken();
        var tokenHash   = Convert.ToHexString(
                              System.Security.Cryptography.SHA256.HashData(
                                  System.Text.Encoding.UTF8.GetBytes(rawRefresh)));

        var refreshToken = RefreshToken.Create(
            userId:    user.Id,
            tokenHash: tokenHash,
            expiresAt: DateTime.UtcNow.AddDays(7));

        await refreshTokenRepository.AddAsync(refreshToken, ct);

        var dto = new AuthResponseDto(
            AccessToken: accessToken,
            UserId:      user.Id,
            FullName:    user.FullName,
            Role:        user.Role.ToString(),
            ExpiresAt:   DateTime.UtcNow.AddMinutes(15));

        return Result<AuthResponseDto>.SuccessWithExtra(dto, rawRefresh);
    }
}