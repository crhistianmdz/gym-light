namespace GymFlow.Domain.Entities;

/// <summary>
/// Registra un evento de congelamiento de membresía.
///
/// Reglas de negocio (PRD §3 Fase 2 / HU-07):
///   - Máximo 4 eventos por año calendario por socio.
///   - Duración mínima de 7 días por evento.
///   - El EndDate del socio se extiende automáticamente al aplicar el congelamiento.
/// </summary>
public class MembershipFreeze
{
    public Guid Id { get; private set; }

    /// <summary>Socio congelado.</summary>
    public Guid MemberId { get; private set; }

    /// <summary>Fecha de inicio del congelamiento (inclusive).</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Fecha de fin del congelamiento (inclusive).</summary>
    public DateOnly EndDate { get; private set; }

    /// <summary>
    /// Duración efectiva en días del congelamiento.
    /// Se calcula al crear para garantizar consistencia incluso si las fechas se editan por migración.
    /// </summary>
    public int DurationDays { get; private set; }

    /// <summary>AppUser que aplicó el congelamiento (Admin u Owner).</summary>
    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Propiedad de navegación EF Core
    public Member Member { get; private set; } = null!;

    // EF Core constructor
    private MembershipFreeze() { }

    /// <summary>
    /// Factory method. Valida las reglas de negocio HU-07 antes de crear la instancia.
    /// </summary>
    /// <param name="memberId">Id del socio a congelar.</param>
    /// <param name="startDate">Inicio del congelamiento.</param>
    /// <param name="endDate">Fin del congelamiento (mín. startDate + 6 días).</param>
    /// <param name="createdByUserId">Admin u Owner que aplica el congelamiento.</param>
    /// <exception cref="ArgumentException">Si la duración es menor a 7 días.</exception>
    public static MembershipFreeze Create(
        Guid memberId,
        DateOnly startDate,
        DateOnly endDate,
        Guid createdByUserId)
    {
        var duration = endDate.DayNumber - startDate.DayNumber + 1; // inclusive

        if (duration < 7)
            throw new ArgumentException(
                $"La duración mínima de un congelamiento es 7 días. Duración calculada: {duration} días.",
                nameof(endDate));

        if (startDate > endDate)
            throw new ArgumentException(
                "La fecha de inicio no puede ser posterior a la fecha de fin.",
                nameof(startDate));

        return new MembershipFreeze
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            StartDate = startDate,
            EndDate = endDate,
            DurationDays = duration,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
