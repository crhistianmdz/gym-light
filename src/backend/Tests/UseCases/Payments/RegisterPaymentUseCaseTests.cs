using GymFlow.Application.DTOs.Metrics;
using GymFlow.Application.UseCases.Admin;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Domain.Interfaces;
using Moq;
using Xunit;
using FluentAssertions;

namespace GymFlow.Tests.UseCases.Payments;

public class RegisterPaymentUseCaseTests
{
    private readonly Mock<IPaymentRepository> _paymentRepoMock;
    private readonly Mock<IMemberRepository> _memberRepoMock;
    private readonly RegisterPaymentUseCase _useCase;

    public RegisterPaymentUseCaseTests()
    {
        _paymentRepoMock = new Mock<IPaymentRepository>();
        _memberRepoMock = new Mock<IMemberRepository>();
        _useCase = new RegisterPaymentUseCase(_paymentRepoMock.Object, _memberRepoMock.Object);
    }

    private static RegisterPaymentRequest ValidRequest(decimal amount = 100m) =>
        new(
            MemberId: null,
            Amount: amount,
            Category: "Membership",
            ClientGuid: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            Notes: null,
            SaleId: null
        );

    [Fact]
    public async Task ExecuteAsync_AmountZero_ReturnsValidationError()
    {
        // Arrange
        var request = ValidRequest(amount: 0m);

        // Act
        var result = await _useCase.ExecuteAsync(request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("monto");
        _paymentRepoMock.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NegativeAmount_ReturnsValidationError()
    {
        // Arrange
        var request = ValidRequest(amount: -50m);

        // Act
        var result = await _useCase.ExecuteAsync(request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeFalse();
        _paymentRepoMock.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ClientGuidAlreadyExists_ReturnsSuccessWithoutAddingNew()
    {
        // Arrange
        var request = ValidRequest();
        var existingPayment = Payment.Create(null, 100m, PaymentCategory.Membership, Guid.NewGuid(), request.ClientGuid);

        _paymentRepoMock
            .Setup(r => r.ClientGuidExistsAsync(request.ClientGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _paymentRepoMock
            .Setup(r => r.GetByClientGuidAsync(request.ClientGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);

        // Act
        var result = await _useCase.ExecuteAsync(request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        _paymentRepoMock.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_AddsPaymentAndReturnsSuccess()
    {
        // Arrange
        var request = ValidRequest(amount: 250m);

        _paymentRepoMock
            .Setup(r => r.ClientGuidExistsAsync(request.ClientGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _paymentRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(request, Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Amount.Should().Be(250m);
        _paymentRepoMock.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
