using GymFlow.Application.DTOs;
using GymFlow.Infrastructure.Persistence;

namespace GymFlow.Application.UseCases.ExerciseCatalog;

public class CreateExerciseUseCase(GymFlowDbContext db)
{
    public async Task<ExerciseCatalogItemDto> ExecuteAsync(CreateExerciseRequest request, Guid currentUserId, CancellationToken ct = default)
    {
        var exercise = Domain.Entities.ExerciseCatalog.Create(
            request.Name,
            request.Description,
            request.MediaUrl,
            request.IsCustom,
            request.IsCustom ? currentUserId : null
        );

        db.ExerciseCatalogs.Add(exercise);
        await db.SaveChangesAsync(ct);

        return new ExerciseCatalogItemDto(exercise.Id, exercise.Name, exercise.Description, exercise.MediaUrl, exercise.IsCustom);
    }
}