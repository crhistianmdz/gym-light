using System;
using System.Collections.Generic;

namespace GymFlow.Domain.Entities;

public class Routine
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsPublic { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ICollection<RoutineExercise> Exercises { get; private set; } = new List<RoutineExercise>();
    public ICollection<RoutineAssignment> Assignments { get; private set; } = new List<RoutineAssignment>();
    public AppUser CreatedBy { get; private set; } = default!;

    private Routine() { }

    public static Routine Create(string name, string? description, bool isPublic, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de la rutina es obligatorio.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("El nombre no puede superar los 200 caracteres.", nameof(name));
        if (description is not null && description.Length > 1000)
            throw new ArgumentException("La descripción no puede superar los 1000 caracteres.", nameof(description));
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("El ID del usuario creador es obligatorio.", nameof(createdByUserId));

        var now = DateTime.UtcNow;
        return new Routine
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            IsPublic = isPublic,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string name, string? description, bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de la rutina es obligatorio.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("El nombre no puede superar los 200 caracteres.", nameof(name));
        if (description is not null && description.Length > 1000)
            throw new ArgumentException("La descripción no puede superar los 1000 caracteres.", nameof(description));

        Name = name.Trim();
        Description = description?.Trim();
        IsPublic = isPublic;
        UpdatedAt = DateTime.UtcNow;
    }
}