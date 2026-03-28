namespace GymFlow.Application.Interfaces;

using System.Security.Claims;
using GymFlow.Domain.Entities;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}