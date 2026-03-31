using FluentValidation;
using GymFlow.Application.DTOs;

namespace GymFlow.Application.Validators;

public class CreateExerciseValidator : AbstractValidator<CreateExerciseRequest>
{
    public CreateExerciseValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.MediaUrl).MaximumLength(500).When(x => x.MediaUrl is not null);
    }
}