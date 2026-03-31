namespace GymFlow.Application.DTOs;

public record RoutineExerciseDto(
    Guid Id,
    Guid? ExerciseCatalogId,
    string? CatalogExerciseName,
    string? CustomName,
    int Order,
    int Sets,
    int Reps,
    string? Notes
);

public record RoutineDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsPublic,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<RoutineExerciseDto> Exercises
);

public record CreateRoutineRequest(
    string Name,
    string? Description,
    bool IsPublic,
    List<CreateRoutineExerciseRequest> Exercises
);

public record CreateRoutineExerciseRequest(
    Guid? ExerciseCatalogId,
    string? CustomName,
    int Order,
    int Sets,
    int Reps,
    string? Notes
);

public record UpdateRoutineRequest(
    string Name,
    string? Description,
    bool IsPublic,
    List<CreateRoutineExerciseRequest> Exercises
);