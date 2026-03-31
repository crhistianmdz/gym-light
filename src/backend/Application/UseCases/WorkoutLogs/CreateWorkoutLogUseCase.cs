using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.UseCases.WorkoutLogs;

public class CreateWorkoutLogUseCase(GymFlowDbContext db)
{
    public async Task<(WorkoutLogDto Dto, bool AlreadyProcessed)> ExecuteAsync(
        CreateWorkoutLogRequest request, Guid memberId, CancellationToken ct = default)
    {
        var existing = await db.WorkoutLogs
            .Include(w => w.Entries).ThenInclude(e => e.RoutineExercise).ThenInclude(re => re.ExerciseCatalog)
            .FirstOrDefaultAsync(w => w.ClientGuid == request.ClientGuid, ct);

        if (existing is not null) return (MapToDto(existing), true);

        var assignmentExists = await db.RoutineAssignments
            .AnyAsync(ra => ra.Id == request.AssignmentId && ra.MemberId == memberId, ct);
        if (!assignmentExists) throw new KeyNotFoundException("Assignment no encontrado o no pertenece al socio.");

        var log = WorkoutLog.Create(request.AssignmentId, memberId, request.SessionDate, request.ClientGuid);

        foreach (var entry in request.Entries)
        {
            var we = WorkoutExerciseEntry.Create(log.Id, entry.RoutineExerciseId, entry.Notes);
            if (entry.Completed) we.MarkCompleted();
            log.Entries.Add(we);
        }

        db.WorkoutLogs.Add(log);
        await db.SaveChangesAsync(ct);

        var created = await db.WorkoutLogs
            .Include(w => w.Entries).ThenInclude(e => e.RoutineExercise).ThenInclude(re => re.ExerciseCatalog)
            .FirstAsync(w => w.Id == log.Id, ct);

        return (MapToDto(created), false);
    }

    internal static WorkoutLogDto MapToDto(WorkoutLog log)
    {
        return new WorkoutLogDto(
            log.Id, log.AssignmentId, log.MemberId,
            log.SessionDate, log.ClientGuid, log.CreatedAt,
            log.Entries.Select(e => new WorkoutExerciseEntryDto(
                e.Id, e.RoutineExerciseId,
                e.RoutineExercise.ExerciseCatalog?.Name ?? e.RoutineExercise.CustomName ?? "",
                e.RoutineExercise.Sets, e.RoutineExercise.Reps,
                e.Completed, e.CompletedAt, e.Notes
            )).ToList()
        );
    }
}