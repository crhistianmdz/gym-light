namespace GymFlow.Application.Validators;

/// <summary>
/// Validaciones de negocio para CreateMemberDto.
/// Se aplican antes de ejecutar cualquier lógica en el Use Case.
/// </summary>
public static class CreateMemberValidator
{
    private const string WebPPrefix = "data:image/webp;base64,";
    private const int MaxFullNameLength = 200;

    public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);

    public static ValidationResult Validate(DTOs.CreateMemberDto dto)
    {
        var errors = new List<string>();

        // ── FullName ──────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(dto.FullName))
            errors.Add("El nombre del socio es obligatorio.");
        else if (dto.FullName.Trim().Length > MaxFullNameLength)
            errors.Add($"El nombre no puede superar {MaxFullNameLength} caracteres.");

        // ── PhotoWebPBase64 (HU-02 CA-1 y CA-2) ──────────────────────────────
        if (string.IsNullOrWhiteSpace(dto.PhotoWebPBase64))
            errors.Add("La foto del socio es obligatoria.");
        else if (!dto.PhotoWebPBase64.StartsWith(WebPPrefix, StringComparison.OrdinalIgnoreCase))
            errors.Add("La foto debe estar en formato WebP (data:image/webp;base64,...).");

        // ── MembershipEndDate ─────────────────────────────────────────────────
        if (dto.MembershipEndDate <= DateOnly.FromDateTime(DateTime.UtcNow))
            errors.Add("La fecha de vencimiento debe ser posterior a hoy.");

        return new ValidationResult(errors.Count == 0, errors);
    }
}
