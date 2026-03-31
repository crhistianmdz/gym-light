using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.UseCases.Routines;

public class CreateRoutineUseCase(GymFlowDbContext db)
{
    public async Task<RoutineDto> ExecuteAsync(CreateRoutineRequest request, Guid createdByUserId, CancellationToken ct = default)
    {
        var routine = Routine.Create(request.Name, request.Description, request.IsPublic, createdByUserId);

        foreach (var ex in request.Exercises)
        {
            var routineEx = RoutineExercise.Create(
                routine.Id, ex.ExerciseCatalogId, ex.CustomName,
                ex.Order, ex.Sets, ex.Reps, ex.Notes);
            routine.Exercises.Add(routineEx);
        }

        db.Routines.Add(routine);
        await db.SaveChangesAsync(ct);

        return await MapToDtoAsync(routine.Id, db, ct);
    }

    internal static async Task<RoutineDto> MapToDtoAsync(Guid routineId, GymFlowDbContext db, CancellationToken ct)
    {
        var routine = await db.Routines
            .Include(r => r.Exercises)
                .ThenInclude(re => re.ExerciseCatalog)
            .FirstAsync(r => r.Id == routineId, ct);

        return ToDto(routine);
    }

    internal static RoutineDto ToDto(Routine routine)
    {
        return new RoutineDto(
            routine.Id,
            routine.Name,
            routine.Description,
            routine.IsPublic,
            routine.CreatedByUserId,
            routine.CreatedAt,
            routine.UpdatedAt,
            routine.Exercises
                .OrderBy(e => e.Order)
                .Select(e => new RoutineExerciseDto(
                    e.Id,
                    e.ExerciseCatalogId,
                    e.ExerciseCatalog?.Name,
                    e.CustomName,
                    e.Order, e.Sets, e.Reps, e.Notes))
                .ToList()
        );
    }
}