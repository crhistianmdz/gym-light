using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Represents a body measurement taken for a member.
/// </summary>
public class BodyMeasurement
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The ID of the member this measurement belongs to.
    /// </summary>
    public Guid MemberId { get; private set; }

    /// <summary>
    /// The ID of the user who recorded this measurement.
    /// </summary>
    public Guid RecordedById { get; private set; }

    /// <summary>
    /// The date and time the measurement was recorded (UTC).
    /// </summary>
    public DateTime RecordedAt { get; private set; }

    /// <summary>
    /// Weight in kilograms (or pounds if UnitSystem is Imperial).
    /// </summary>
    public decimal WeightKg { get; private set; }

    /// <summary>
    /// Body fat percentage.
    /// </summary>
    public decimal BodyFatPct { get; private set; }

    /// <summary>
    /// Chest measurement in centimeters (or inches if Imperial).
    /// </summary>
    public decimal ChestCm { get; private set; }

    /// <summary>
    /// Waist measurement in centimeters (or inches if Imperial).
    /// </summary>
    public decimal WaistCm { get; private set; }

    /// <summary>
    /// Hip measurement in centimeters (or inches if Imperial).
    /// </summary>
    public decimal HipCm { get; private set; }

    /// <summary>
    /// Arm measurement (biceps) in centimeters (or inches if Imperial).
    /// </summary>
    public decimal ArmCm { get; private set; }

    /// <summary>
    /// Leg measurement (thighs) in centimeters (or inches if Imperial).
    /// </summary>
    public decimal LegCm { get; private set; }

    /// <summary>
    /// Unit system (Metric or Imperial).
    /// </summary>
    public UnitSystem UnitSystem { get; private set; }

    /// <summary>
    /// Unique client-side operation identifier for idempotency.
    /// </summary>
    public string ClientGuid { get; private set; } = string.Empty;

    /// <summary>
    /// Optional notes or remarks about this measurement.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private BodyMeasurement() { }

    /// <summary>
    /// Factory method to create a new BodyMeasurement instance.
    /// </summary>
    /// <param name="memberId">The ID of the member.</param>
    /// <param name="recordedById">The ID of the user who recorded the measurement.</param>
    /// <param name="recordedAt">The date and time of the measurement (UTC).</param>
    /// <param name="weightKg">Weight in kilograms.</param>
    /// <param name="bodyFatPct">Body fat percentage.</param>
    /// <param name="chestCm">Chest size in centimeters.</param>
    /// <param name="waistCm">Waist size in centimeters.</param>
    /// <param name="hipCm">Hip size in centimeters.</param>
    /// <param name="armCm">Arm size in centimeters.</param>
    /// <param name="legCm">Leg size in centimeters.</param>
    /// <param name="unitSystem">Unit system (Metric/Imperial).</param>
    /// <param name="clientGuid">Client-side unique identifier.</param>
    /// <param name="notes">Optional notes.</param>
    /// <returns>A validated BodyMeasurement instance.</returns>
    /// <exception cref="ArgumentException">Thrown if any numeric field is less than or equal to zero, a GUID is empty, or clientGuid is null/empty.</exception>
    public static BodyMeasurement Create(
        Guid memberId,
        Guid recordedById,
        DateTime recordedAt,
        decimal weightKg,
        decimal bodyFatPct,
        decimal chestCm,
        decimal waistCm,
        decimal hipCm,
        decimal armCm,
        decimal legCm,
        UnitSystem unitSystem,
        string clientGuid,
        string? notes = null)
    {
        if (memberId == Guid.Empty)
            throw new ArgumentException("MemberId cannot be empty.", nameof(memberId));

        if (recordedById == Guid.Empty)
            throw new ArgumentException("RecordedById cannot be empty.", nameof(recordedById));

        if (recordedAt == default)
            throw new ArgumentException("RecordedAt is required.", nameof(recordedAt));

        if (weightKg <= 0 || bodyFatPct <= 0 || chestCm <= 0 || waistCm <= 0 || hipCm <= 0 || armCm <= 0 || legCm <= 0)
            throw new ArgumentException("All numeric fields must be greater than zero.");

        if (string.IsNullOrWhiteSpace(clientGuid))
            throw new ArgumentException("ClientGuid is required.", nameof(clientGuid));

        return new BodyMeasurement
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            RecordedById = recordedById,
            RecordedAt = recordedAt,
            WeightKg = weightKg,
            BodyFatPct = bodyFatPct,
            ChestCm = chestCm,
            WaistCm = waistCm,
            HipCm = hipCm,
            ArmCm = armCm,
            LegCm = legCm,
            UnitSystem = unitSystem,
            ClientGuid = clientGuid,
            Notes = notes
        };
    }
}