using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.UseCases.Routines;

public class UpdateRoutineUseCase(GymFlowDbContext db)
{
    public async Task<RoutineDto?> ExecuteAsync(Guid routineId, UpdateRoutineRequest request, Guid currentUserId, bool isAdminOrOwner, CancellationToken ct = default)
    {
        var routine = await db.Routines
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(r => r.Id == routineId, ct);

        if (routine is null) return null;

        if (!isAdminOrOwner && routine.CreatedByUserId != currentUserId)
            throw new UnauthorizedAccessException("Solo puedes editar tus propias rutinas.");

        routine.Update(request.Name, request.Description, request.IsPublic);

        db.RoutineExercises.RemoveRange(routine.Exercises);
        routine.Exercises.Clear();

        foreach (var ex in request.Exercises)
        {
            var routineEx = RoutineExercise.Create(
                routine.Id, ex.ExerciseCatalogId, ex.CustomName,
                ex.Order, ex.Sets, ex.Reps, ex.Notes);
            routine.Exercises.Add(routineEx);
        }

        await db.SaveChangesAsync(ct);
        return await CreateRoutineUseCase.MapToDtoAsync(routine.Id, db, ct);
    }
}