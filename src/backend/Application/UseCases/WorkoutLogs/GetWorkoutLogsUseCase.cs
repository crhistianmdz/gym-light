using GymFlow.Application.DTOs;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.UseCases.WorkoutLogs;

public class GetWorkoutLogsUseCase(GymFlowDbContext db)
{
    public async Task<List<WorkoutLogDto>> ExecuteAsync(Guid memberId, CancellationToken ct = default)
    {
        var logs = await db.WorkoutLogs
            .Include(w => w.Entries).ThenInclude(e => e.RoutineExercise).ThenInclude(re => re.ExerciseCatalog)
            .Where(w => w.MemberId == memberId)
            .OrderByDescending(w => w.SessionDate)
            .ToListAsync(ct);

        return logs.Select(CreateWorkoutLogUseCase.MapToDto).ToList();
    }
}