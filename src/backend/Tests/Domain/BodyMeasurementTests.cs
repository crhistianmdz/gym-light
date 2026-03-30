using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace GymFlow.Tests.Domain;

/// <summary>
/// Unit tests for BodyMeasurement entity.
/// </summary>
public class BodyMeasurementTests
{
    [Fact]
    public void Create_WithAllValidFields_Succeeds()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var recordedById = Guid.NewGuid();
        var recordedAt = DateTime.UtcNow;

        // Act
        var measurement = BodyMeasurement.Create(
            memberId,
            recordedById,
            recordedAt,
            70m,
            15m,
            100m,
            80m,
            90m,
            35m,
            50m,
            UnitSystem.Metric,
            Guid.NewGuid().ToString());

        // Assert
        measurement.MemberId.Should().Be(memberId);
        measurement.RecordedById.Should().Be(recordedById);
        measurement.RecordedAt.Should().Be(recordedAt);
        measurement.WeightKg.Should().Be(70m);
    }

    [Fact]
    public void Create_WithWeightZero_ThrowsArgumentException()
    {
        // Act
        Action act = () => BodyMeasurement.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            0, 10, 10, 10, 10, 10, 10, UnitSystem.Metric, Guid.NewGuid().ToString());

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*numeric fields*");
    }

    [Fact]
    public void Create_WithNegativeBodyFat_ThrowsArgumentException()
    {
        // Act
        Action act = () => BodyMeasurement.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            70, -5, 10, 10, 10, 10, 10, UnitSystem.Metric, Guid.NewGuid().ToString());

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*numeric fields*");
    }

    [Fact]
    public void Create_WithEmptyMemberId_ThrowsArgumentException()
    {
        // Act
        Action act = () => BodyMeasurement.Create(
            Guid.Empty, Guid.NewGuid(), DateTime.UtcNow,
            70, 15, 100, 80, 90, 35, 50, UnitSystem.Metric, Guid.NewGuid().ToString());

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*MemberId*");
    }

    [Fact]
    public void Create_WithEmptyClientGuid_ThrowsArgumentException()
    {
        // Act
        Action act = () => BodyMeasurement.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            70, 15, 100, 80, 90, 35, 50, UnitSystem.Metric, "");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*ClientGuid*");
    }

    [Fact]
    public void Create_WithNullNotes_Succeeds()
    {
        // Act
        var measurement = BodyMeasurement.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
            70, 15, 100, 80, 90, 35, 50, UnitSystem.Metric, Guid.NewGuid().ToString(), null);

        // Assert
        measurement.Notes.Should().BeNull();
    }
}