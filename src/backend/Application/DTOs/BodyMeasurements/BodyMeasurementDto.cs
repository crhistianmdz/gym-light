using GymFlow.Domain.Enums;

namespace GymFlow.Application.DTOs.BodyMeasurements;

/// <summary>
/// DTO for returning body measurement details.
/// </summary>
public record BodyMeasurementDto(
    Guid Id,
    Guid MemberId,
    Guid RecordedById,
    DateTime RecordedAt,
    decimal WeightKg,
    decimal BodyFatPct,
    decimal ChestCm,
    decimal WaistCm,
    decimal HipCm,
    decimal ArmCm,
    decimal LegCm,
    UnitSystem UnitSystem,
    string? Notes,
    string ClientGuid
);