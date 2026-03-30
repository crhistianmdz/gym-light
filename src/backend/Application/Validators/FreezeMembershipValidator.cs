using GymFlow.Application.DTOs;

namespace GymFlow.Application.Validators;

/// <summary>
/// Validaciones de negocio para FreezeMembershipDto.
/// HU-07: mínimo 7 días, StartDate no puede ser anterior a hoy.
/// </summary>
public static class FreezeMembershipValidator
{
    private const int MinDurationDays = 7;

    public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);

    public static ValidationResult Validate(FreezeMembershipDto dto)
    {
        var errors = new List<string>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // ── MemberId ─────────────────────────────────────────────────────────
        if (dto.MemberId == Guid.Empty)
            errors.Add("El Id del socio es obligatorio.");

        // ── StartDate ────────────────────────────────────────────────────────
        if (dto.StartDate < today)
            errors.Add("La fecha de inicio del congelamiento no puede ser anterior a hoy.");

        // ── EndDate ──────────────────────────────────────────────────────────
        if (dto.EndDate <= dto.StartDate)
            errors.Add("La fecha de fin debe ser posterior a la fecha de inicio.");

        // ── Duración mínima HU-07 R2 ─────────────────────────────────────────
        if (dto.EndDate > dto.StartDate)
        {
            var duration = dto.EndDate.DayNumber - dto.StartDate.DayNumber + 1; // inclusive
            if (duration < MinDurationDays)
                errors.Add(
                    $"La duración mínima de un congelamiento es {MinDurationDays} días. " +
                    $"El rango indicado tiene {duration} día(s).");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}
