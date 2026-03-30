using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GymFlow.Application.DTOs.BodyMeasurements;
using GymFlow.Application.UseCases.BodyMeasurements;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Moq;
using Xunit;

namespace GymFlow.Tests.UseCases.BodyMeasurements;

public class GetBodyMeasurementsUseCaseTests
{
    private readonly Mock<IBodyMeasurementRepository> _bodyMeasurementRepositoryMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly GetBodyMeasurementsUseCase _useCase;

    public GetBodyMeasurementsUseCaseTests()
    {
        _bodyMeasurementRepositoryMock = new Mock<IBodyMeasurementRepository>();
        _memberRepositoryMock = new Mock<IMemberRepository>();

        _useCase = new GetBodyMeasurementsUseCase(
            _bodyMeasurementRepositoryMock.Object,
            _memberRepositoryMock.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_MemberGetsOwn_ReturnsList()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var callerId = memberId;

        _memberRepositoryMock.Setup(m => m.ExistsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _bodyMeasurementRepositoryMock.Setup(r => r.GetByMemberIdAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<BodyMeasurement>());

        // Act
        var result = await _useCase.ExecuteAsync(memberId, callerId, Domain.Enums.UserRole.Member, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task ExecuteAsync_MemberGetsOther_ReturnsForbidden()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var callerId = Guid.NewGuid();

        _memberRepositoryMock.Setup(m => m.ExistsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _useCase.ExecuteAsync(memberId, callerId, Domain.Enums.UserRole.Member, CancellationToken.None);

        // Assert
        Assert.True(result.IsForbidden);
    }

    [Fact]
    public async Task ExecuteAsync_MemberNotFound_ReturnsNotFound()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var callerId = memberId;

        _memberRepositoryMock.Setup(m => m.ExistsAsync(memberId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _useCase.ExecuteAsync(memberId, callerId, Domain.Enums.UserRole.Member, CancellationToken.None);

        // Assert
        Assert.True(result.IsNotFound);
    }
}