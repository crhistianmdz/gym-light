namespace GymFlow.Application.DTOs;

/// <summary>
/// Representa un evento de congelamiento de membresía retornado por la API.
/// </summary>
public record MembershipFreezeDto(
    Guid Id,
    Guid MemberId,
    DateOnly StartDate,
    DateOnly EndDate,
    int DurationDays,
    Guid CreatedByUserId,
    DateTime CreatedAt
);
