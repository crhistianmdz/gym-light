namespace GymFlow.Application.DTOs;

public record AuthResponseDto(
    string AccessToken,
    Guid UserId,
    string FullName,
    string Role,
    DateTime ExpiresAt
);