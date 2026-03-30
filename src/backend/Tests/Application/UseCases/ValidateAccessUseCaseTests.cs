using GymFlow.Application.UseCases.Access;
using GymFlow.Application.DTOs;
using GymFlow.Domain.Interfaces;
using Moq;
using Xunit;
using GymFlow.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;

namespace GymFlow.Tests.Application.UseCases;

public class ValidateAccessUseCaseTests
{
    private readonly Mock<IAccessLogRepository> _accessLogRepoMock;
    private readonly Mock<IMemberRepository> _memberRepoMock;
    private readonly ValidateAccessUseCase _useCase;

    public ValidateAccessUseCaseTests()
    {
        _accessLogRepoMock = new Mock<IAccessLogRepository>();
        _memberRepoMock = new Mock<IMemberRepository>();
        _useCase = new ValidateAccessUseCase(_memberRepoMock.Object, _accessLogRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenPerformedByUserIdIsEmpty_ReturnsBadRequest()
    {
        // Arrange
        var request = new CheckInRequestDto(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("PerformedByUserId is required for check-in traceability.");
    }

    [Fact]
    public async Task Handle_WhenPerformedByUserIdIsNull_ReturnsBadRequest()
    {
        // Arrange
        var request = new CheckInRequestDto(Guid.NewGuid(), Guid.NewGuid(), default);

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("PerformedByUserId is required for check-in traceability.");
    }

    [Fact]
    public async Task Handle_WhenClientGuidAlreadyExists_ReturnsExistingLog()
    {
        // Arrange
        var existingLog = AccessLog.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true);
        _accessLogRepoMock.Setup(x => x.GetByClientGuidAsync(existingLog.ClientGuid, It.IsAny<CancellationToken>())).ReturnsAsync(existingLog);

        var request = new CheckInRequestDto(Guid.NewGuid(), existingLog.ClientGuid, Guid.NewGuid());

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenMemberExpired_ReturnsDenied()
    {
        // Arrange
        var member = new Mock<Member>();
        member.Setup(m => m.CanAccess()).Returns(false);
        member.Setup(m => m.GetDenialReason()).Returns("Membership expired.");
        _memberRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(member.Object);

        var request = new CheckInRequestDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Allowed.Should().BeFalse();
        result.Value.DenialReason.Should().Be("Membership expired.");
    }

    [Fact]
    public async Task Handle_WhenMemberFrozen_ReturnsDenied()
    {
        // Arrange
        var member = new Mock<Member>();
        member.Setup(m => m.CanAccess()).Returns(false);
        member.Setup(m => m.GetDenialReason()).Returns("Membership frozen.");
        _memberRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(member.Object);

        var request = new CheckInRequestDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Allowed.Should().BeFalse();
        result.Value.DenialReason.Should().Be("Membership frozen.");
    }

    [Fact]
    public async Task Handle_WhenMemberActive_ReturnsAllowed()
    {
        // Arrange
        var member = new Mock<Member>();
        member.Setup(m => m.CanAccess()).Returns(true);

        _memberRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(member.Object);

        var request = new CheckInRequestDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Allowed.Should().BeTrue();
    }
}