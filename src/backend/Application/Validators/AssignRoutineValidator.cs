using FluentValidation;
using GymFlow.Application.DTOs;

namespace GymFlow.Application.Validators;

public class AssignRoutineValidator : AbstractValidator<AssignRoutineRequest>
{
    public AssignRoutineValidator()
    {
        RuleFor(x => x.RoutineId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
    }
}