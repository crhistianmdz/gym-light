using FluentValidation;
using GymFlow.Application.DTOs;

namespace GymFlow.Application.Validators;

public class CreateWorkoutLogValidator : AbstractValidator<CreateWorkoutLogRequest>
{
    public CreateWorkoutLogValidator()
    {
        RuleFor(x => x.AssignmentId).NotEmpty();
        RuleFor(x => x.ClientGuid).NotEmpty();
        RuleFor(x => x.Entries).NotNull().NotEmpty();
        RuleForEach(x => x.Entries).ChildRules(e =>
        {
            e.RuleFor(x => x.RoutineExerciseId).NotEmpty();
            e.RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
        });
    }
}