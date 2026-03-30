using GymFlow.Application.Common;
using GymFlow.Application.DTOs;
using GymFlow.Application.UseCases.Members;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using GymFlow.Domain.Interfaces;
using Moq;
using Xunit;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GymFlow.Tests.Application.UseCases;

public class CancelMembershipUseCaseTests
{
    private readonly Mock<IMemberRepository> _memberRepoMock;
    private readonly CancelMembershipUseCase _useCase;

    public CancelMembershipUseCaseTests()
    {
        _memberRepoMock = new Mock<IMemberRepository>();
        _useCase = new CancelMembershipUseCase(_memberRepoMock.Object);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static Member ActiveMember(DateOnly endDate)
        => Member.Create("Test Member", "photo.webp", endDate);

    private static CancelMembershipRequestDto AnyRequest()
        => new(Guid.NewGuid());

    // ── RBAC ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_AsAdmin_ReturnsOk()
    {
        var member = ActiveMember(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
        _memberRepoMock
            .Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await _useCase.ExecuteAsync(
            member.Id, callerId: Guid.NewGuid(), callerRole: "Admin", AnyRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(MemberStatus.Cancelled);
    }

    [Fact]
    public async Task ExecuteAsync_AsOwner_ReturnsOk()
    {
        var member = ActiveMember(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
        _memberRepoMock
            .Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await _useCase.ExecuteAsync(
            member.Id, callerId: Guid.NewGuid(), callerRole: "Owner", AnyRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_AsMemberSelf_ReturnsOk()
    {
        var member = ActiveMember(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
        _memberRepoMock
            .Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // callerId == memberId → self-cancel
        var result = await _useCase.ExecuteAsync(
            member.Id, callerId: member.Id, callerRole: "Member", AnyRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_AsMemberOther_ReturnsForbidden()
    {
        var memberId = Guid.NewGuid();
        var result = await _useCase.ExecuteAsync(
            memberId, callerId: Guid.NewGuid(), callerRole: "Member", AnyRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    // ── estados de dominio ────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenExpired_ReturnsBadRequest()
    {
        var member = ActiveMember(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        member.Expire();
        _memberRepoMock
            .Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await _useCase.ExecuteAsync(
            member.Id, callerId: Guid.NewGuid(), callerRole: "Admin", AnyRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFrozen_CancelsWithoutChangingStatus()
    {
        var member = ActiveMember(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
        member.Freeze(10);
        _memberRepoMock
            .Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await _useCase.ExecuteAsync(
            member.Id, callerId: Guid.NewGuid(), callerRole: "Admin", AnyRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(MemberStatus.Frozen);
        member.AutoRenewEnabled.Should().BeFalse();
        member.CancelledAt.Should().NotBeNull();
    }

    // ── idempotencia ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_DuplicateRequest_ReturnsOkWithoutCallingUpdate()
    {
        var member = ActiveMember(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
        member.Cancel(); // ya cancelado
        _memberRepoMock
            .Setup(r => r.GetByIdAsync(member.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await _useCase.ExecuteAsync(
            member.Id, callerId: Guid.NewGuid(), callerRole: "Admin", AnyRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // UpdateAsync no debe haberse llamado (idempotencia — no reprocesa)
        _memberRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── not found ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenMemberNotFound_Returns404()
    {
        _memberRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        var result = await _useCase.ExecuteAsync(
            Guid.NewGuid(), callerId: Guid.NewGuid(), callerRole: "Admin", AnyRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }
}
