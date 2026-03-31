namespace GymFlow.Application.DTOs;

public record WorkoutExerciseEntryDto(
    Guid Id,
    Guid RoutineExerciseId,
    string ExerciseName,
    int Sets,
    int Reps,
    bool Completed,
    DateTime? CompletedAt,
    string? Notes
);

public record WorkoutLogDto(
    Guid Id,
    Guid AssignmentId,
    Guid MemberId,
    DateTime SessionDate,
    Guid ClientGuid,
    DateTime CreatedAt,
    List<WorkoutExerciseEntryDto> Entries
);

public record CreateWorkoutLogRequest(
    Guid AssignmentId,
    DateTime SessionDate,
    Guid ClientGuid,
    List<CreateWorkoutEntryRequest> Entries
);

public record CreateWorkoutEntryRequest(
    Guid RoutineExerciseId,
    bool Completed,
    string? Notes
);