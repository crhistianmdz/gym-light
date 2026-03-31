using System;

namespace GymFlow.Domain.Entities;

public class WorkoutExerciseEntry
{
    public Guid Id { get; private set; }
    public Guid WorkoutLogId { get; private set; }
    public Guid RoutineExerciseId { get; private set; }
    public bool Completed { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? Notes { get; private set; }

    public WorkoutLog WorkoutLog { get; private set; } = default!;
    public RoutineExercise RoutineExercise { get; private set; } = default!;

    private WorkoutExerciseEntry() { }

    public static WorkoutExerciseEntry Create(Guid workoutLogId, Guid routineExerciseId, string? notes = null)
    {
        if (workoutLogId == Guid.Empty)
            throw new ArgumentException("El ID del WorkoutLog es obligatorio.", nameof(workoutLogId));
        if (routineExerciseId == Guid.Empty)
            throw new ArgumentException("El ID del RoutineExercise es obligatorio.", nameof(routineExerciseId));
        if (notes is not null && notes.Length > 500)
            throw new ArgumentException("Las notas no pueden superar los 500 caracteres.", nameof(notes));

        return new WorkoutExerciseEntry
        {
            Id = Guid.NewGuid(),
            WorkoutLogId = workoutLogId,
            RoutineExerciseId = routineExerciseId,
            Completed = false,
            CompletedAt = null,
            Notes = notes?.Trim()
        };
    }

    public void MarkCompleted()
    {
        Completed = true;
        CompletedAt = DateTime.UtcNow;
    }

    public void UpdateNotes(string? notes)
    {
        if (notes is not null && notes.Length > 500)
            throw new ArgumentException("Las notas no pueden superar los 500 caracteres.", nameof(notes));
        Notes = notes?.Trim();
    }
}