using GymFlow.Application.DTOs;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.UseCases.Routines;

public class GetMemberRoutinesUseCase(GymFlowDbContext db)
{
    public async Task<List<RoutineAssignmentDto>> ExecuteAsync(Guid memberId, CancellationToken ct = default)
    {
        return await db.RoutineAssignments
            .Include(ra => ra.Routine)
            .Where(ra => ra.MemberId == memberId && ra.IsActive)
            .OrderByDescending(ra => ra.AssignedAt)
            .Select(ra => new RoutineAssignmentDto(
                ra.Id, ra.RoutineId, ra.Routine.Name,
                ra.MemberId, ra.AssignedByUserId, ra.AssignedAt, ra.IsActive))
            .ToListAsync(ct);
    }
}