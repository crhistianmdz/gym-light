using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs.BodyMeasurements;

/// <summary>
/// Request DTO for adding a new body measurement.
/// </summary>
public record AddBodyMeasurementRequest(
    string ClientGuid,
    DateTime RecordedAt,
    decimal WeightKg,
    decimal BodyFatPct,
    decimal ChestCm,
    decimal WaistCm,
    decimal HipCm,
    decimal ArmCm,
    decimal LegCm,
    UnitSystem UnitSystem,
    string? Notes
);