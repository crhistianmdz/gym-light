using FluentValidation;
using GymFlow.Application.DTOs;

namespace GymFlow.Application.Validators;

public class CreateRoutineValidator : AbstractValidator<CreateRoutineRequest>
{
    public CreateRoutineValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
        RuleFor(x => x.Exercises).NotNull();
        RuleForEach(x => x.Exercises).ChildRules(ex =>
        {
            ex.RuleFor(e => e).Must(e => e.ExerciseCatalogId.HasValue || !string.IsNullOrWhiteSpace(e.CustomName))
                .WithMessage("Cada ejercicio debe tener un ID del catálogo o un nombre personalizado.");
            ex.RuleFor(e => e.CustomName).MaximumLength(100).When(e => e.CustomName is not null);
            ex.RuleFor(e => e.Order).GreaterThanOrEqualTo(1);
            ex.RuleFor(e => e.Sets).GreaterThanOrEqualTo(1);
            ex.RuleFor(e => e.Reps).GreaterThanOrEqualTo(1);
            ex.RuleFor(e => e.Notes).MaximumLength(500).When(e => e.Notes is not null);
        });
    }
}