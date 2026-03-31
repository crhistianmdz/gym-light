using System;
using System.Collections.Generic;

namespace GymFlow.Domain.Entities;

public class RoutineAssignment
{
    public Guid Id { get; private set; }
    public Guid RoutineId { get; private set; }
    public Guid MemberId { get; private set; }
    public Guid AssignedByUserId { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public bool IsActive { get; private set; }

    public Routine Routine { get; private set; } = default!;
    public Member Member { get; private set; } = default!;
    public AppUser AssignedBy { get; private set; } = default!;
    public ICollection<WorkoutLog> WorkoutLogs { get; private set; } = new List<WorkoutLog>();

    private RoutineAssignment() { }

    public static RoutineAssignment Assign(Guid routineId, Guid memberId, Guid assignedByUserId)
    {
        if (routineId == Guid.Empty)
            throw new ArgumentException("El ID de la rutina es obligatorio.", nameof(routineId));
        if (memberId == Guid.Empty)
            throw new ArgumentException("El ID del socio es obligatorio.", nameof(memberId));
        if (assignedByUserId == Guid.Empty)
            throw new ArgumentException("El ID del usuario asignador es obligatorio.", nameof(assignedByUserId));

        return new RoutineAssignment
        {
            Id = Guid.NewGuid(),
            RoutineId = routineId,
            MemberId = memberId,
            AssignedByUserId = assignedByUserId,
            AssignedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Deactivate() => IsActive = false;
}