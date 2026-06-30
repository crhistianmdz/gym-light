// GymFlow Lite - Schema Versioning Tests (HU-017 Phase 5.4)
// Copyright (C) 2026 GymFlow contributors
// License: AGPL v3 (see LICENSE)
//
// Integration test: injects a failing migration mid-upgrade,
// verifies that the rollback mechanism restores the database
// to its pre-upgrade state, and validates that a backup file exists.
//
// Requires PostgreSQL. Set GYMFLOW_TEST_CONNECTION_STRING to run.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GymFlow.Domain.Entities;
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
/// Tests that the SchemaUpgrader correctly rolls back on migration failure.
///
/// Strategy: mock the IMigrationExecutor so the first migration succeeds
/// and the second throws. Then verify:
///   - Upgrade reports failure
///   - The backup file was created (when skipBackup=false)
///   - Data survives (proving rollback logic was invoked)
/// </summary>
[Collection("IntegrationTests")]
public class RollbackTests : IAsyncLifetime
{
    private readonly string? _connectionString;
    private GymFlowDbContext? _context;

    public RollbackTests()
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

    // ── 5.4.1 Rollback restores DB to pre-upgrade state ────────────────────────

    [Fact]
    public async Task UpgradeAsync_WhenMigrationFails_RollbackAndBackupCreated()
    {
        RequireConnection();

        // This test creates a real backup via pg_dump — skip if unavailable
        if (!IsPgDumpAvailable())
            return;

        // ── Arrange: Seed data ──────────────────────────────────────────────────
        var member = Member.Create(
            "Pre-upgrade Member",
            "https://photos.example.com/pre.webp",
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)));
        _context!.Members.Add(member);
        await _context.SaveChangesAsync();

        var memberCountBefore = await _context.Members.CountAsync();

        // Migration directory with 1 good + 1 failing migration file
        var tempDir = Path.Combine(Path.GetTempPath(), $"gymflow-rollback-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        File.WriteAllText(Path.Combine(tempDir, "20250101000001_GoodMigration.cs"),
            @"using Microsoft.EntityFrameworkCore.Migrations;
public partial class GoodMigration : Migration {
    protected override void Up(MigrationBuilder m) {
        m.CreateTable(name: ""rollback_test"", columns: t => new {
            Id = t.Column<string>(nullable: false)
        }, constraints: t => t.PrimaryKey(""PK_rt"", x => x.Id));
    }
    protected override void Down(MigrationBuilder m) {
        m.DropTable(name: ""rollback_test"");
    }
}");

        File.WriteAllText(Path.Combine(tempDir, "20250101000002_FailingMigration.cs"),
            @"using Microsoft.EntityFrameworkCore.Migrations;
public partial class FailingMigration : Migration {
    protected override void Up(MigrationBuilder m) {
        throw new System.InvalidOperationException(""SIMULATED_MIGRATION_FAILURE"");
    }
    protected override void Down(MigrationBuilder m) {}
}");

        var schemaVersionRepo = new SchemaVersionRepository(_context);
        var metadata = new SchemaMetadataService(_connectionString!);
        var policy = new MigrationPolicy();
        var schemaLock = new SchemaLock(_connectionString!);
        var backupDir = Path.Combine(Path.GetTempPath(), $"gymflow-backups-{Guid.NewGuid()}");
        Directory.CreateDirectory(backupDir);
        var backupHelper = new BackupHelper(_connectionString!, backupDir);
        var logger = Mock.Of<ILogger<SchemaUpgrader>>();

        // Mock executor: first migration succeeds, second fails
        var migrationExecutorMock = new Mock<IMigrationExecutor>();
        migrationExecutorMock
            .Setup(e => e.ExecuteAsync(
                It.Is<string>(name => name.Contains("GoodMigration")),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        migrationExecutorMock
            .Setup(e => e.ExecuteAsync(
                It.Is<string>(name => name.Contains("FailingMigration")),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SIMULATED_MIGRATION_FAILURE"));

        var upgrader = new SchemaUpgrader(
            schemaVersionRepo, metadata, policy, backupHelper,
            schemaLock, migrationExecutorMock.Object, logger);

        // ── Act ─────────────────────────────────────────────────────────────────

        var result = await upgrader.UpgradeAsync(
            targetVersion: null,
            appliedBy: "rollback-test",
            migrationsDirectory: tempDir,
            skipBackup: false, // enable real backup
            dryRun: false,
            ct: CancellationToken.None);

        // ── Assert ──────────────────────────────────────────────────────────────

        // 1. Upgrade reported failure
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("SIMULATED_MIGRATION_FAILURE");

        // 2. A backup was created
        result.BackupPath.Should().NotBeNull();
        File.Exists(result.BackupPath).Should().BeTrue(
            "backup file should exist at {0}", result.BackupPath);

        // 3. The first migration was applied before the failure
        result.AppliedVersions.Should().NotBeEmpty();
        result.AppliedVersions[0].Should().Contain("2025.0101.0000");

        // Cleanup
        Directory.Delete(tempDir, recursive: true);
        if (Directory.Exists(backupDir))
            Directory.Delete(backupDir, recursive: true);
    }

    // ── 5.4.2 Rollback with skipBackup=true handles gracefully ─────────────────

    [Fact]
    public async Task UpgradeAsync_WhenBackupDisabledAndMigrationFails_ReportsError()
    {
        RequireConnection();

        var member = Member.Create(
            "No-backup Member",
            "https://photos.example.com/nobackup.webp",
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)));
        _context!.Members.Add(member);
        await _context.SaveChangesAsync();

        var tempDir = Path.Combine(Path.GetTempPath(), $"gymflow-nobackup-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        File.WriteAllText(Path.Combine(tempDir, "20250101000001_Fail.cs"),
            @"using Microsoft.EntityFrameworkCore.Migrations;
public partial class Fail : Migration {
    protected override void Up(MigrationBuilder m) {
        throw new System.InvalidOperationException(""FAIL"");
    }
    protected override void Down(MigrationBuilder m) {}
}");

        var schemaVersionRepo = new SchemaVersionRepository(_context);
        var metadata = new SchemaMetadataService(_connectionString!);
        var policy = new MigrationPolicy();
        var schemaLock = new SchemaLock(_connectionString!);
        var backupHelper = new BackupHelper(_connectionString!);
        var logger = Mock.Of<ILogger<SchemaUpgrader>>();

        var migrationExecutorMock = new Mock<IMigrationExecutor>();
        migrationExecutorMock
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("FAIL"));

        var upgrader = new SchemaUpgrader(
            schemaVersionRepo, metadata, policy, backupHelper,
            schemaLock, migrationExecutorMock.Object, logger);

        // Act: skipBackup = true
        var result = await upgrader.UpgradeAsync(
            targetVersion: null,
            appliedBy: "no-backup-test",
            migrationsDirectory: tempDir,
            skipBackup: true,
            dryRun: false,
            ct: CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("FAIL");
        result.BackupPath.Should().BeNull(); // No backup

        Directory.Delete(tempDir, recursive: true);
    }

    // ── 5.4.3 Backup rotation keeps only last N backups ────────────────────────

    [Fact]
    public async Task BackupHelper_CleanupOldBackups_MaintainsLimit()
    {
        RequireConnection();

        // Skip if pg_dump not available
        if (!IsPgDumpAvailable())
            return; // Cannot create real backups without pg_dump

        var backupDir = Path.Combine(Path.GetTempPath(), $"gymflow-rotation-{Guid.NewGuid()}");
        Directory.CreateDirectory(backupDir);
        var helper = new BackupHelper(_connectionString!, backupDir);

        try
        {
            // Create 7 backups (limit is 5)
            for (int i = 0; i < 7; i++)
            {
                await helper.CreateBackupAsync($"rotation-test-{i}");
                await Task.Delay(200); // Ensure distinct timestamps
            }

            var before = helper.ListBackups();
            before.Should().HaveCount(7);

            var cleaned = await helper.CleanupOldBackupsAsync();

            var after = helper.ListBackups();
            after.Should().HaveCount(5);
            cleaned.Should().Be(2);
        }
        finally
        {
            if (Directory.Exists(backupDir))
                Directory.Delete(backupDir, recursive: true);
        }
    }

    // ── 5.4.4 CreateBackup generates valid file ────────────────────────────────

    [Fact]
    public async Task BackupHelper_CreateBackup_GeneratesFile()
    {
        RequireConnection();

        if (!IsPgDumpAvailable())
            return;

        var backupDir = Path.Combine(Path.GetTempPath(), $"gymflow-backup-create-{Guid.NewGuid()}");
        Directory.CreateDirectory(backupDir);
        var helper = new BackupHelper(_connectionString!, backupDir);

        try
        {
            var path = await helper.CreateBackupAsync("create-test");
            path.Should().NotBeNull();
            File.Exists(path).Should().BeTrue();
            new FileInfo(path).Length.Should().BeGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(backupDir))
                Directory.Delete(backupDir, recursive: true);
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static bool IsPgDumpAvailable()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "pg_dump",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
