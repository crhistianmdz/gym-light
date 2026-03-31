namespace GymFlow.Application.DTOs;

public record ExerciseCatalogItemDto(
    Guid Id,
    string Name,
    string? Description,
    string? MediaUrl,
    bool IsCustom
);

public record CreateExerciseRequest(
    string Name,
    string? Description,
    string? MediaUrl,
    bool IsCustom
);