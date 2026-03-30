using System;
using System.Threading;
using System.Threading.Tasks;
using GymFlow.Application.DTOs.BodyMeasurements;
using GymFlow.Application.UseCases.BodyMeasurements;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Moq;
using Xunit;

namespace GymFlow.Tests.UseCases.BodyMeasurements;

public class AddBodyMeasurementUseCaseTests
{
    private readonly Mock<IBodyMeasurementRepository> _bodyMeasurementRepositoryMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly AddBodyMeasurementUseCase _useCase;

    public AddBodyMeasurementUseCaseTests()
    {
        _bodyMeasurementRepositoryMock = new Mock<IBodyMeasurementRepository>();
        _memberRepositoryMock = new Mock<IMemberRepository>();

        _useCase = new AddBodyMeasurementUseCase(
            _bodyMeasurementRepositoryMock.Object,
            _memberRepositoryMock.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_SameClientGuid_ReturnsAlreadyProcessed()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var existingMeasurement = new BodyMeasurement();

        _memberRepositoryMock.Setup(m => m.ExistsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _bodyMeasurementRepositoryMock.Setup(r => r.GetByClientGuidAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(existingMeasurement);

        var request = new AddBodyMeasurementRequest(
            "client-guid",
            DateTime.UtcNow,
            70,
            15,
            90,
            80,
            95,
            35,
            45,
            Domain.Enums.UnitSystem.Metric,
            "notes"
        );

        // Act
        var result = await _useCase.ExecuteAsync(memberId, callerId, Domain.Enums.UserRole.Member, request, CancellationToken.None);

        // Assert
        Assert.True(result.IsAlreadyProcessed);
    }

    [Fact]
    public async Task ExecuteAsync_MemberCallerForOwnMeasurement_Succeeds()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var callerId = memberId;

        _memberRepositoryMock.Setup(m => m.ExistsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _bodyMeasurementRepositoryMock.Setup(r => r.GetByClientGuidAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((BodyMeasurement?)null);

        var request = new AddBodyMeasurementRequest(
            "new-client-guid",
            DateTime.UtcNow,
            70,
            15,
            90,
            80,
            95,
            35,
            45,
            Domain.Enums.UnitSystem.Metric,
            null
        );

        // Act
        var result = await _useCase.ExecuteAsync(memberId, callerId, Domain.Enums.UserRole.Member, request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_MemberCallerForOtherMember_ReturnsForbidden()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        _memberRepositoryMock.Setup(m => m.ExistsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var request = new AddBodyMeasurementRequest(
            "client-guid",
            DateTime.UtcNow,
            70,
            15,
            90,
            80,
            95,
            35,
            45,
            Domain.Enums.UnitSystem.Metric,
            null
        );

        // Act
        var result = await _useCase.ExecuteAsync(memberId, callerId, Domain.Enums.UserRole.Member, request, CancellationToken.None);

        // Assert
        Assert.True(result.IsForbidden);
    }
}