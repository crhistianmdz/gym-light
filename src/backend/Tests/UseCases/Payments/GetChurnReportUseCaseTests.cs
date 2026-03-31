using GymFlow.Application.UseCases.Admin;
using GymFlow.Domain.Interfaces;
using Moq;
using Xunit;
using FluentAssertions;

namespace GymFlow.Tests.UseCases.Payments;

public class GetChurnReportUseCaseTests
{
    private readonly Mock<IMemberRepository> _memberRepoMock;
    private readonly GetChurnReportUseCase _useCase;

    public GetChurnReportUseCaseTests()
    {
        _memberRepoMock = new Mock<IMemberRepository>();
        _useCase = new GetChurnReportUseCase(_memberRepoMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_YearBelow2020_ReturnsValidationError()
    {
        // Act
        var result = await _useCase.ExecuteAsync(2010);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("2020");
        _memberRepoMock.Verify(r => r.GetChurnStatsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_FutureYear_ReturnsValidationError()
    {
        // Arrange
        var futureYear = DateTime.UtcNow.Year + 1;

        // Act
        var result = await _useCase.ExecuteAsync(futureYear);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ZeroMembers_ChurnRateIsZeroNoDivisionByZero()
    {
        // Arrange
        _memberRepoMock
            .Setup(r => r.GetChurnStatsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0, 0));

        // Act
        var result = await _useCase.ExecuteAsync(DateTime.UtcNow.Year);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ChurnRate.Should().Be(0.0);
        result.Value.TotalMembers.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ValidData_CalculatesChurnRateCorrectly()
    {
        // Arrange — 10 total, 7 active, 3 not renewed → churn = 30%
        _memberRepoMock
            .Setup(r => r.GetChurnStatsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((10, 7, 3));

        // Act
        var result = await _useCase.ExecuteAsync(DateTime.UtcNow.Year);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalMembers.Should().Be(10);
        result.Value.ActiveMembers.Should().Be(7);
        result.Value.NotRenewed.Should().Be(3);
        result.Value.ChurnRate.Should().BeApproximately(30.0, precision: 0.01);
    }
}
