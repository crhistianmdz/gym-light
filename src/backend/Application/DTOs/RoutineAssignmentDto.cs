namespace GymFlow.Application.DTOs;

public record AssignRoutineRequest(
    Guid RoutineId,
    Guid MemberId
);

public record RoutineAssignmentDto(
    Guid Id,
    Guid RoutineId,
    string RoutineName,
    Guid MemberId,
    Guid AssignedByUserId,
    DateTime AssignedAt,
    bool IsActive
);