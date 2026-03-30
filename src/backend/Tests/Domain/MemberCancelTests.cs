using System;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Xunit;

namespace GymFlow.Domain.Tests
{
    public class MemberCancelTests
    {
        [Fact]
        public void Cancel_WhenActive_SetsStatusCancelledAndFlags()
        {
            var member = Member.Create("John Doe", "photo.webp", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
            
            member.Cancel();

            Assert.False(member.AutoRenewEnabled);
            Assert.Equal(MemberStatus.Cancelled, member.Status);
            Assert.NotNull(member.CancelledAt);
        }

        [Fact]
        public void Cancel_WhenFrozen_KeepsFrozenStatusButSetsFlags()
        {
            var member = Member.Create("John Doe", "photo.webp", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
            member.Freeze(30);

            member.Cancel();

            Assert.False(member.AutoRenewEnabled);
            Assert.Equal(MemberStatus.Frozen, member.Status);
            Assert.NotNull(member.CancelledAt);
        }

        [Fact]
        public void Cancel_WhenExpired_ThrowsDomainException()
        {
            var member = Member.Create("John Doe", "photo.webp", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
            member.Expire();

            Assert.Throws<DomainException>(() => member.Cancel());
        }

        [Fact]
        public void CanAccess_WhenCancelledWithValidEndDate_ReturnsTrue()
        {
            var member = Member.Create("John Doe", "photo.webp", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)));
            member.Cancel();

            Assert.True(member.CanAccess());
        }

        [Fact]
        public void CanAccess_WhenCancelledWithExpiredEndDate_ReturnsFalse()
        {
            var member = Member.Create("John Doe", "photo.webp", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
            member.Cancel();

            Assert.False(member.CanAccess());
        }
    }
}