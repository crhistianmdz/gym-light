using System;
using System.Collections.Generic;

namespace GymFlow.Domain.Entities;

public class RoutineExercise
{
    public Guid Id { get; private set; }
    public Guid RoutineId { get; private set; }
    public Guid? ExerciseCatalogId { get; private set; }
    public string? CustomName { get; private set; }
    public int Order { get; private set; }
    public int Sets { get; private set; }
    public int Reps { get; private set; }
    public string? Notes { get; private set; }

    public Routine Routine { get; private set; } = default!;
    public ExerciseCatalog? ExerciseCatalog { get; private set; }
    public ICollection<WorkoutExerciseEntry> Entries { get; private set; } = new List<WorkoutExerciseEntry>();

    private RoutineExercise() { }

    public static RoutineExercise Create(Guid routineId, Guid? exerciseCatalogId, string? customName, int order, int sets, int reps, string? notes)
    {
        if (exerciseCatalogId is null && string.IsNullOrWhiteSpace(customName))
            throw new ArgumentException("Se debe indicar un ejercicio del catálogo o un nombre personalizado.");
        if (customName is not null && customName.Length > 100)
            throw new ArgumentException("El nombre personalizado no puede superar los 100 caracteres.", nameof(customName));
        if (notes is not null && notes.Length > 500)
            throw new ArgumentException("Las notas no pueden superar los 500 caracteres.", nameof(notes));
        if (order < 1)
            throw new ArgumentException("El orden debe ser mayor o igual a 1.", nameof(order));
        if (sets < 1)
            throw new ArgumentException("Los sets deben ser mayor o igual a 1.", nameof(sets));
        if (reps < 1)
            throw new ArgumentException("Las repeticiones deben ser mayor o igual a 1.", nameof(reps));

        return new RoutineExercise
        {
            Id = Guid.NewGuid(),
            RoutineId = routineId,
            ExerciseCatalogId = exerciseCatalogId,
            CustomName = customName?.Trim(),
            Order = order,
            Sets = sets,
            Reps = reps,
            Notes = notes?.Trim()
        };
    }
}