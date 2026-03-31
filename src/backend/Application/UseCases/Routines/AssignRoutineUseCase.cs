using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.UseCases.Routines;

public class AssignRoutineUseCase(GymFlowDbContext db)
{
    public async Task<RoutineAssignmentDto> ExecuteAsync(AssignRoutineRequest request, Guid assignedByUserId, CancellationToken ct = default)
    {
        var routine = await db.Routines.FindAsync([request.RoutineId], ct)
            ?? throw new KeyNotFoundException("Rutina no encontrada.");

        var memberExists = await db.Members.AnyAsync(m => m.Id == request.MemberId, ct);
        if (!memberExists) throw new KeyNotFoundException("Socio no encontrado.");

        var assignment = RoutineAssignment.Assign(request.RoutineId, request.MemberId, assignedByUserId);
        db.RoutineAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);

        return new RoutineAssignmentDto(
            assignment.Id, assignment.RoutineId, routine.Name,
            assignment.MemberId, assignment.AssignedByUserId,
            assignment.AssignedAt, assignment.IsActive);
    }
}