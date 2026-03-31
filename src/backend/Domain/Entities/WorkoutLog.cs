using System;
using System.Collections.Generic;

namespace GymFlow.Domain.Entities;

public class WorkoutLog
{
    public Guid Id { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid MemberId { get; private set; }
    public DateTime SessionDate { get; private set; }
    public Guid ClientGuid { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public RoutineAssignment Assignment { get; private set; } = default!;
    public Member Member { get; private set; } = default!;
    public ICollection<WorkoutExerciseEntry> Entries { get; private set; } = new List<WorkoutExerciseEntry>();

    private WorkoutLog() { }

    public static WorkoutLog Create(Guid assignmentId, Guid memberId, DateTime sessionDate, Guid clientGuid)
    {
        if (assignmentId == Guid.Empty)
            throw new ArgumentException("El ID del assignment es obligatorio.", nameof(assignmentId));
        if (memberId == Guid.Empty)
            throw new ArgumentException("El ID del socio es obligatorio.", nameof(memberId));
        if (clientGuid == Guid.Empty)
            throw new ArgumentException("El ClientGuid es obligatorio para idempotencia.", nameof(clientGuid));

        return new WorkoutLog
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignmentId,
            MemberId = memberId,
            SessionDate = sessionDate,
            ClientGuid = clientGuid,
            CreatedAt = DateTime.UtcNow
        };
    }
}