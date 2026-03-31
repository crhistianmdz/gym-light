using System;
using System.Collections.Generic;

namespace GymFlow.Domain.Entities;

public class ExerciseCatalog
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? MediaUrl { get; private set; }
    public bool IsCustom { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<RoutineExercise> RoutineExercises { get; private set; } = new List<RoutineExercise>();

    private ExerciseCatalog() { }

    public static ExerciseCatalog Create(string name, string? description, string? mediaUrl, bool isCustom, Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del ejercicio es obligatorio.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("El nombre no puede superar los 100 caracteres.", nameof(name));
        if (description is not null && description.Length > 500)
            throw new ArgumentException("La descripción no puede superar los 500 caracteres.", nameof(description));
        if (mediaUrl is not null && mediaUrl.Length > 500)
            throw new ArgumentException("La URL de media no puede superar los 500 caracteres.", nameof(mediaUrl));
        if (isCustom && (createdByUserId is null || createdByUserId == Guid.Empty))
            throw new ArgumentException("Un ejercicio personalizado requiere el ID del usuario creador.", nameof(createdByUserId));

        return new ExerciseCatalog
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            MediaUrl = mediaUrl?.Trim(),
            IsCustom = isCustom,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }
}