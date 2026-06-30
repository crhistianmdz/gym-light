// GymFlow Lite - Schema Versioning Tests (HU-017 Phase 5.5)
// Copyright (C) 2026 GymFlow contributors
// License: AGPL v3 (see LICENSE)
//
// Integration test: starts two concurrent upgrade processes and verifies
// that only one succeeds while the other fails because the advisory lock
// is already held.
//
// Requires PostgreSQL. Set GYMFLOW_TEST_CONNECTION_STRING to run.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GymFlow.Domain.Interfaces;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Persistence.Repositories;
using GymFlow.Infrastructure.Persistence.Services;
using GymFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GymFlow.Tests.Schema;

/// <summary>
/// Verifies that the advisory lock mechanism prevents two upgrade
/// processes from running simultaneously against the same database.
///
/// Approach:
///   1. Start upgrade process A (acquires lock, begins work)
///   2. Start upgrade process B (attempts lock → fails)
///   3. Validate A succeeds, B fails with lock-related error
/// </summary>
[Collection("IntegrationTests")]
public class ConcurrentUpgradeTests : IAsyncLifetime
{
    private readonly string? _connectionString;
    private GymFlowDbContext? _context;

    public ConcurrentUpgradeTests()
    {
        _connectionString = Environment.GetEnvironmentVariable("GYMFLOW_TEST_CONNECTION_STRING");
    }

    public async Task InitializeAsync()
    {
        if (_connectionString == null)
            return;

        var options = new DbContextOptionsBuilder<GymFlowDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        _context = new GymFlowDbContext(options);
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    private void RequireConnection()
    {
        if (_connectionString == null)
            throw new InvalidOperationException(
                "GYMFLOW_TEST_CONNECTION_STRING environment variable must be set to run integration tests.");
    }

    // ── 5.5.1 Two concurrent upgrades: only one succeeds ───────────────────────

    [Fact]
    public async Task ConcurrentUpgrade_WithTwoProcesses_OnlyOneSucceeds()
    {
        RequireConnection();

        var tempDir = Path.Combine(Path.GetTempPath(), $"gymflow-concurrent-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        File.WriteAllText(Path.Combine(tempDir, "20250101000001_Migration.cs"),
            @"using Microsoft.EntityFrameworkCore.Migrations;
public partial class Migration : Migration {
    protected override void Up(MigrationBuilder m) {}
    protected override void Down(MigrationBuilder m) {}
}");

        // Process A: will acquire lock and hold it (simulated work delay)
        var schemaVersionRepoA = new SchemaVersionRepository(_context!);
        var metadataA = new SchemaMetadataService(_connectionString!);
        var policyA = new MigrationPolicy();
        var schemaLockA = new SchemaLock(_connectionString!);
        var backupHelperA = new BackupHelper(_connectionString!);
        var loggerA = Mock.Of<ILogger<SchemaUpgrader>>();

        var tcsA = new TaskCompletionSource<bool>();
        var migrationExecutorA = new Mock<IMigrationExecutor>();
        migrationExecutorA
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                tcsA.TrySetResult(true); // Signal that A is working (lock held)
                await Task.Delay(2000, ct);
            });

        var upgraderA = new SchemaUpgrader(
            schemaVersionRepoA, metadataA, policyA, backupHelperA,
            schemaLockA, migrationExecutorA.Object, loggerA);

        // Process B: separate instance, same DB
        var schemaVersionRepoB = new SchemaVersionRepository(_context!);
        var metadataB = new SchemaMetadataService(_connectionString!);
        var policyB = new MigrationPolicy();
        var schemaLockB = new SchemaLock(_connectionString!);
        var backupHelperB = new BackupHelper(_connectionString!);
        var loggerB = Mock.Of<ILogger<SchemaUpgrader>>();

        var migrationExecutorB = new Mock<IMigrationExecutor>();
        migrationExecutorB
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var upgraderB = new SchemaUpgrader(
            schemaVersionRepoB, metadataB, policyB, backupHelperB,
            schemaLockB, migrationExecutorB.Object, loggerB);

        // ── Act ─────────────────────────────────────────────────────────────────

        // Start A
        var taskA = upgraderA.UpgradeAsync(
            targetVersion: null, appliedBy: "process-A",
            migrationsDirectory: tempDir, skipBackup: true,
            dryRun: false, ct: CancellationToken.None);

        // Wait for A to acquire the lock
        await tcsA.Task;

        // Start B while A holds the lock
        var taskB = upgraderB.UpgradeAsync(
            targetVersion: null, appliedBy: "process-B",
            migrationsDirectory: tempDir, skipBackup: true,
            dryRun: false, ct: CancellationToken.None);

        var resultA = await taskA;
        var resultB = await taskB;

        // ── Assert ──────────────────────────────────────────────────────────────

        resultA.Success.Should().BeTrue(
            $"Process A should succeed. Error: {resultA.ErrorMessage}");

        resultB.Success.Should().BeFalse(
            "Process B should fail because the advisory lock was held by A");
        resultB.ErrorMessage.Should().NotBeNull();
        resultB.ErrorMessage!.ToLowerInvariant().Should()
            .ContainAny("lock", "otro proceso", "en curso",
                "error should indicate concurrent upgrade conflict");

        resultB.MigrationsApplied.Should().Be(0);

        Directory.Delete(tempDir, recursive: true);
    }

    // ── 5.5.2 SchemaLock rejects re-acquisition by same instance ───────────────

    [Fact]
    public async Task SchemaLock_AcquireTwiceOnSameInstance_Throws()
    {
        RequireConnection();

        var schemaLock = new SchemaLock(_connectionString!);

        var firstAcquired = await schemaLock.AcquireAsync();
        firstAcquired.Should().BeTrue();

        try
        {
            var act = async () => await schemaLock.AcquireAsync();
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*lock ya está adquirido*");
        }
        finally
        {
            await schemaLock.ReleaseAsync();
        }
    }

    // ── 5.5.3 SchemaLock.IsHeld detects external lock ──────────────────────────

    [Fact]
    public async Task SchemaLock_IsHeld_DetectsExternalLock()
    {
        RequireConnection();

        var lockA = new SchemaLock(_connectionString!);
        var lockB = new SchemaLock(_connectionString!);

        // Initially free
        (await lockB.IsHeldAsync()).Should().BeFalse();

        // A acquires
        await lockA.AcquireAsync();
        lockA.IsAcquired.Should().BeTrue();

        try
        {
            // B detects lock
            (await lockB.IsHeldAsync()).Should().BeTrue(
                "B should detect A holds the advisory lock");

            // B cannot acquire
            var bAcquired = await lockB.AcquireAsync();
            bAcquired.Should().BeFalse("B should fail to acquire while A holds it");
        }
        finally
        {
            await lockA.ReleaseAsync();
        }

        // Free after release
        (await lockB.IsHeldAsync()).Should().BeFalse();
    }

    // ── 5.5.4 Lock is freed after release ──────────────────────────────────────

    [Fact]
    public async Task SchemaLock_Release_FreesLockForNextProcess()
    {
        RequireConnection();

        var lockA = new SchemaLock(_connectionString!);
        var lockB = new SchemaLock(_connectionString!);

        await lockA.AcquireAsync();
        lockA.IsAcquired.Should().BeTrue();

        await lockA.ReleaseAsync();
        lockA.IsAcquired.Should().BeFalse();

        // B can now acquire
        var bAcquired = await lockB.AcquireAsync();
        bAcquired.Should().BeTrue("B should acquire after A releases");

        await lockB.ReleaseAsync();
    }

    // ── 5.5.5 Default lock ID is consistent ────────────────────────────────────

    [Fact]
    public void SchemaLock_DefaultLockId_HU017()
    {
        var schemaLock = new SchemaLock("Host=localhost;Database=test");
        schemaLock.LockId.Should().Be(1701, "default lock ID maps to HU-017");
    }

    // ── 5.5.6 SchemaLock requires connection string ─────────────────────────────

    [Fact]
    public void SchemaLock_Constructor_ThrowsOnNullConnectionString()
    {
        Action act = () => new SchemaLock(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
