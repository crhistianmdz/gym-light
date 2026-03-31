using System;
using GymFlow.Domain.Entities;
using Xunit;

namespace GymFlow.Tests.Domain;

public class RoutineTests
{
    [Fact]
    public void Routine_Create_WithValidData_ReturnsRoutine()
    {
        var userId = Guid.NewGuid();
        var routine = Routine.Create("Rutina A", "Descripción", false, userId);
        Assert.Equal("Rutina A", routine.Name);
        Assert.Equal(userId, routine.CreatedByUserId);
        Assert.False(routine.IsPublic);
        Assert.NotEqual(Guid.Empty, routine.Id);
    }

    [Fact]
    public void Routine_Create_WithEmptyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Routine.Create("", null, false, Guid.NewGuid()));
    }

    [Fact]
    public void Routine_Create_WithEmptyCreatedByUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Routine.Create("Rutina A", null, false, Guid.Empty));
    }

    [Fact]
    public void RoutineExercise_Create_WithBothCatalogIdAndCustomNameNull_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            RoutineExercise.Create(Guid.NewGuid(), null, null, 1, 3, 10, null));
    }

    [Fact]
    public void RoutineExercise_Create_WithCustomName_WhenCatalogIdNull_CreatesOk()
    {
        var re = RoutineExercise.Create(Guid.NewGuid(), null, "Press Banca", 1, 3, 10, null);
        Assert.Equal("Press Banca", re.CustomName);
        Assert.Null(re.ExerciseCatalogId);
    }

    [Fact]
    public void WorkoutLog_Create_WithEmptyClientGuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            WorkoutLog.Create(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, Guid.Empty));
    }

    [Fact]
    public void WorkoutExerciseEntry_MarkCompleted_SetsCompletedTrueAndTimestamp()
    {
        var entry = WorkoutExerciseEntry.Create(Guid.NewGuid(), Guid.NewGuid());
        Assert.False(entry.Completed);
        Assert.Null(entry.CompletedAt);

        entry.MarkCompleted();

        Assert.True(entry.Completed);
        Assert.NotNull(entry.CompletedAt);
    }
}