using GymFlow.Application.UseCases.Admin;
using GymFlow.Domain.Enums;
using GymFlow.Domain.Interfaces;
using Moq;
using Xunit;
using FluentAssertions;

namespace GymFlow.Tests.UseCases.Payments;

public class GetIncomeReportUseCaseTests
{
    private readonly Mock<IPaymentRepository> _paymentRepoMock;
    private readonly GetIncomeReportUseCase _useCase;

    public GetIncomeReportUseCaseTests()
    {
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _useCase = new GetIncomeReportUseCase(_paymentRepoMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_FromGreaterThanTo_ReturnsValidationError()
    {
        // Arrange
        var from = new DateOnly(2025, 12, 1);
        var to   = new DateOnly(2025, 1, 1);

        // Act
        var result = await _useCase.ExecuteAsync(from, to);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("rango");
        _paymentRepoMock.Verify(r => r.GetMonthlyIncomeAsync(
            It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NoPaymentsInRange_ReturnsTotalsAsZero()
    {
        // Arrange
        var from = new DateOnly(2025, 1, 1);
        var to   = new DateOnly(2025, 12, 31);

        _paymentRepoMock
            .Setup(r => r.GetMonthlyIncomeAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonthlyAggregateRow>());

        // Act
        var result = await _useCase.ExecuteAsync(from, to);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalIncome.Should().Be(0m);
        result.Value.ByMonth.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedPayments_PivotsToMonthlyBreakdown()
    {
        // Arrange
        var from = new DateOnly(2025, 3, 1);
        var to   = new DateOnly(2025, 3, 31);

        var rows = new List<MonthlyAggregateRow>
        {
            new(2025, 3, PaymentCategory.Membership, 500m),
            new(2025, 3, PaymentCategory.POS, 200m),
        };

        _paymentRepoMock
            .Setup(r => r.GetMonthlyIncomeAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        // Act
        var result = await _useCase.ExecuteAsync(from, to);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ByMonth.Should().HaveCount(1);
        result.Value.ByMonth[0].Membership.Should().Be(500m);
        result.Value.ByMonth[0].Pos.Should().Be(200m);
        result.Value.ByMonth[0].Total.Should().Be(700m);
        result.Value.TotalIncome.Should().Be(700m);
    }

    [Fact]
    public async Task ExecuteAsync_SameDateRange_ReturnsSuccess()
    {
        // Arrange — from == to is valid
        var date = new DateOnly(2025, 6, 15);

        _paymentRepoMock
            .Setup(r => r.GetMonthlyIncomeAsync(date, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MonthlyAggregateRow>());

        // Act
        var result = await _useCase.ExecuteAsync(date, date);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
