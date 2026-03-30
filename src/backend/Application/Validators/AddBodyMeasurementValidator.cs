using FluentValidation;
using GymFlow.Application.DTOs.BodyMeasurements;

namespace GymFlow.Application.Validators;

/// <summary>
/// Validator for AddBodyMeasurementRequest.
/// </summary>
public class AddBodyMeasurementValidator : AbstractValidator<AddBodyMeasurementRequest>
{
    public AddBodyMeasurementValidator()
    {
        RuleFor(x => x.ClientGuid)
            .NotEmpty();

        RuleFor(x => x.RecordedAt)
            .NotEmpty();

        RuleFor(x => x.WeightKg)
            .GreaterThan(0);

        RuleFor(x => x.BodyFatPct)
            .GreaterThan(0);

        RuleFor(x => x.ChestCm)
            .GreaterThan(0);

        RuleFor(x => x.WaistCm)
            .GreaterThan(0);

        RuleFor(x => x.HipCm)
            .GreaterThan(0);

        RuleFor(x => x.ArmCm)
            .GreaterThan(0);

        RuleFor(x => x.LegCm)
            .GreaterThan(0);

        RuleFor(x => x.UnitSystem)
            .IsInEnum();
    }
}