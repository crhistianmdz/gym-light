namespace GymFlow.Application.DTOs;

/// <summary>
/// Request para congelar la membresía de un socio.
/// Validado por FreezeMembershipValidator (FluentValidation).
///
/// HU-07: mínimo 7 días, máximo 4 congelamientos por año calendario.
/// </summary>
public record FreezeMembershipDto(
    Guid MemberId,
    DateOnly StartDate,
    DateOnly EndDate
);
