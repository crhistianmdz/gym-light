using GymFlow.Application.DTOs;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.UseCases.Routines;

public class GetRoutinesUseCase(GymFlowDbContext db)
{
    public async Task<List<RoutineDto>> ExecuteAsync(Guid currentUserId, bool? isPublicFilter, bool mineOnly, CancellationToken ct = default)
    {
        var query = db.Routines
            .Include(r => r.Exercises)
                .ThenInclude(re => re.ExerciseCatalog)
            .AsQueryable();

        if (mineOnly)
            query = query.Where(r => r.CreatedByUserId == currentUserId);
        else if (isPublicFilter.HasValue)
            query = query.Where(r => r.IsPublic == isPublicFilter.Value);
        else
            query = query.Where(r => r.IsPublic || r.CreatedByUserId == currentUserId);

        var routines = await query.OrderByDescending(r => r.UpdatedAt).ToListAsync(ct);
        return routines.Select(CreateRoutineUseCase.ToDto).ToList();
    }
}