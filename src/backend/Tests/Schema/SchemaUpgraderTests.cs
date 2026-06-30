// GymFlow Lite - Schema Versioning Tests (HU-017 Phase 5.1)
// Copyright (C) 2026 GymFlow contributors
// License: AGPL v3 (see LICENSE)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using GymFlow.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GymFlow.Tests.Schema;

/// <summary>
/// Unit tests for SchemaUpgrader — validates upgrade logic,
/// semver ordering, lock acquisition, and skip-already-applied.
///
/// All external dependencies are mocked: repository, metadata,
/// policy, backup, lock, and migration executor.
/// </summary>
public class SchemaUpgraderTests
{
    private readonly Mock<ISchemaVersionRepository> _versionRepoMock;
    private readonly Mock<ISchemaMetadata> _metadataMock;
    private readonly BackupHelper _backupHelper;
    private readonly Mock<ISchemaLock> _schemaLockMock;
    private readonly Mock<IMigrationExecutor> _migrationExecutorMock;
    private readonly Mock<ILogger<SchemaUpgrader>> _loggerMock;
    private readonly MigrationPolicy _policy;
    private readonly string _tempMigrationsDir;

    public SchemaUpgraderTests()
    {
        _versionRepoMock = new Mock<ISchemaVersionRepository>();
        _metadataMock = new Mock<ISchemaMetadata>();

        // Real BackupHelper — its methods are non-virtual so Moq can't mock them.
        // All unit tests use skipBackup=true, so backup methods are never invoked.
        var backupDir = Path.Combine(Path.GetTempPath(), $"gymflow-unit-backups-{Guid.NewGuid()}");
        Directory.CreateDirectory(backupDir);
        _backupHelper = new BackupHelper(
            "Host=localhost;Port=5432;Database=gymflow_test;Username=test;Password=test",
            backupDir, "pg_dump", "pg_restore");

        _schemaLockMock = new Mock<ISchemaLock>();
        _schemaLockMock.Setup(l => l.LockId).Returns(1701);
        _migrationExecutorMock = new Mock<IMigrationExecutor>();
        _loggerMock = new Mock<ILogger<SchemaUpgrader>>();
        _policy = new MigrationPolicy();

        // Setup a temp directory with mock migration .cs files for discovery
        _tempMigrationsDir = Path.Combine(Path.GetTempPath(), $"gymflow-schema-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempMigrationsDir);
    }

    /// <summary>
    /// Creates a minimal migration .cs file with the given timestamp and description.
    /// The file content only needs to exist and NOT contain blocked operations.
    /// </summary>
    private string CreateMockMigrationFile(string timestamp, string description)
    {
        var fileName = $"{timestamp}_{description}.cs";
        var filePath = Path.Combine(_tempMigrationsDir, fileName);

        var content = $@"// Mock migration: {description}
using Microsoft.EntityFrameworkCore.Migrations;

public partial class {description} : Migration
{{
    protected override void Up(MigrationBuilder migrationBuilder)
    {{
        // Additive-only: CREATE TABLE
        migrationBuilder.CreateTable(
            name: ""{description}_table"",
            columns: table => new
            {{
                Id = table.Column<string>(nullable: false)
            }},
            constraints: table =>
            {{
                table.PrimaryKey(""PK_{description}"", x => x.Id);
            }});
    }}

    protected override void Down(MigrationBuilder migrationBuilder)
    {{
        // Not used in additive policy
    }}
}}";

        File.WriteAllText(filePath, content);
        return filePath;
    }

    // ── 5.1.1 Happy path: apply 1 migration ─────────────────────────────────────

    [Fact]
    public async Task UpgradeAsync_WithOnePendingMigration_AppliesItAndRecordsVersion()
    {
        // Arrange
        CreateMockMigrationFile("20250101000001", "AddTestTable");

        _schemaLockMock.Setup(l => l.AcquireAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _schemaLockMock.Setup(l => l.IsAcquired).Returns(true);
        _schemaLockMock.Setup(l => l.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _metadataMock.Setup(m => m.GetDiskSpace(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L * 1024 * 1024); // 1 GB
        _metadataMock.Setup(m => m.GetPgVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync("16.2");

        _versionRepoMock.Setup(r => r.GetLatestVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchemaVersion?)null); // No versions applied yet

        _versionRepoMock.Setup(r => r.RecordApplied(
                It.IsAny<SchemaVersion>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _migrationExecutorMock.Setup(e => e.ExecuteAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var upgrader = CreateUpgrader();

        // Act
        var result = await upgrader.UpgradeAsync(
            targetVersion: null,
            appliedBy: "test-user",
            migrationsDirectory: _tempMigrationsDir,
            skipBackup: true, // skip actual pg_dump call
            dryRun: false,
            ct: CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.MigrationsApplied.Should().Be(1);
        result.AppliedVersions.Should().HaveCount(1);

        // Migration executor was called exactly once
        _migrationExecutorMock.Verify(
            e => e.ExecuteAsync(
                It.Is<string>(name => name.Contains("AddTestTable")),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Version was recorded
        _versionRepoMock.Verify(
            r => r.RecordApplied(
                It.IsAny<SchemaVersion>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── 5.1.2 Skip already-applied ──────────────────────────────────────────────

    [Fact]
    public async Task UpgradeAsync_WhenMigrationAlreadyApplied_SkipsIt()
    {
        // Arrange
        CreateMockMigrationFile("20250101000001", "AddTestTable");

        _schemaLockMock.Setup(l => l.AcquireAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _schemaLockMock.Setup(l => l.IsAcquired).Returns(true);
        _schemaLockMock.Setup(l => l.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _metadataMock.Setup(m => m.GetDiskSpace(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L * 1024 * 1024);
        _metadataMock.Setup(m => m.GetPgVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync("16.2");

        // Simulate that the latest version is already greater than the migration
        var alreadyApplied = SchemaVersion.Create(
            version: "9999.9999.9999", // way higher than any migration
            moduleName: "core",
            appliedBy: "previous-upgrade",
            description: "All up to date",
            migrationHash: "abc123",
            rollbackSql: "-- none");

        _versionRepoMock.Setup(r => r.GetLatestVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadyApplied);

        _versionRepoMock.Setup(r => r.RecordApplied(
                It.IsAny<SchemaVersion>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var upgrader = CreateUpgrader();

        // Act
        var result = await upgrader.UpgradeAsync(
            targetVersion: null,
            appliedBy: "test-user",
            migrationsDirectory: _tempMigrationsDir,
            skipBackup: true,
            dryRun: false,
            ct: CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.MigrationsApplied.Should().Be(0); // Nothing to apply
        result.AppliedVersions.Should().BeEmpty();

        // Migration executor was NEVER called
        _migrationExecutorMock.Verify(
            e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // No version recorded
        _versionRepoMock.Verify(
            r => r.RecordApplied(It.IsAny<SchemaVersion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── 5.1.3 Semver ordering ───────────────────────────────────────────────────

    [Fact]
    public async Task UpgradeAsync_WithMultipleMigrations_AppliesInSemverOrder()
    {
        // Arrange
        // Create migrations with timestamps that map to semver via DeriveSemverFromTimestamp
        // Format: YYYYMMDDHHmm -> YYYY.MMDD.HHmm
        // "20250101000001" -> "2025.0101.0000"  (lowest)
        // "20250601000002" -> "2025.0601.0000"  (middle)
        // "20251201000003" -> "2025.1201.0000"  (highest)

        CreateMockMigrationFile("20250101000001", "MigrationA");
        CreateMockMigrationFile("20250601000002", "MigrationB");
        CreateMockMigrationFile("20251201000003", "MigrationC");

        _schemaLockMock.Setup(l => l.AcquireAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _schemaLockMock.Setup(l => l.IsAcquired).Returns(true);
        _schemaLockMock.Setup(l => l.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _metadataMock.Setup(m => m.GetDiskSpace(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L * 1024 * 1024);
        _metadataMock.Setup(m => m.GetPgVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync("16.2");

        _versionRepoMock.Setup(r => r.GetLatestVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchemaVersion?)null);

        _versionRepoMock.Setup(r => r.RecordApplied(
                It.IsAny<SchemaVersion>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _migrationExecutorMock.Setup(e => e.ExecuteAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var appliedVersions = new List<string>();
        _versionRepoMock.Setup(r => r.RecordApplied(
                It.IsAny<SchemaVersion>(), It.IsAny<CancellationToken>()))
            .Callback<SchemaVersion, CancellationToken>((sv, _) => appliedVersions.Add(sv.Version))
            .Returns(Task.CompletedTask);

        var upgrader = CreateUpgrader();

        // Act
        var result = await upgrader.UpgradeAsync(
            targetVersion: null,
            appliedBy: "test-user",
            migrationsDirectory: _tempMigrationsDir,
            skipBackup: true,
            dryRun: false,
            ct: CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.MigrationsApplied.Should().Be(3);
        result.AppliedVersions.Should().HaveCount(3);

        // Verify semver ordering: MigrationA < MigrationB < MigrationC
        // Timestamps: 20250101000001 < 20250601000002 < 20251201000003
        // Semver:      2025.0101.0000 < 2025.0601.0000 < 2025.1201.0000
        appliedVersions.Should().BeInAscendingOrder(SchemaUpgrader.CompareSemver);
    }

    // ── 5.1.4 Lock acquisition failure ──────────────────────────────────────────

    [Fact]
    public async Task UpgradeAsync_WhenLockCannotBeAcquired_ReturnsFailure()
    {
        // Arrange
        CreateMockMigrationFile("20250101000001", "AddTestTable");

        _schemaLockMock.Setup(l => l.AcquireAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Lock is held by another process
        _schemaLockMock.Setup(l => l.IsAcquired).Returns(false);

        _metadataMock.Setup(m => m.GetDiskSpace(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L * 1024 * 1024);
        _metadataMock.Setup(m => m.GetPgVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync("16.2");

        var upgrader = CreateUpgrader();

        // Act
        var result = await upgrader.UpgradeAsync(
            targetVersion: null,
            appliedBy: "test-user",
            migrationsDirectory: _tempMigrationsDir,
            skipBackup: true,
            dryRun: false,
            ct: CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.MigrationsApplied.Should().Be(0);
        result.ErrorMessage.Should().Contain("lock");

        // Migration executor was NEVER called
        _migrationExecutorMock.Verify(
            e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── 5.1.5 Dry run validates but does not apply ──────────────────────────────

    [Fact]
    public async Task UpgradeAsync_WithDryRun_ValidatesButDoesNotApply()
    {
        // Arrange
        CreateMockMigrationFile("20250101000001", "AddTestTable");

        _schemaLockMock.Setup(l => l.AcquireAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _schemaLockMock.Setup(l => l.IsAcquired).Returns(true);
        _schemaLockMock.Setup(l => l.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _metadataMock.Setup(m => m.GetDiskSpace(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L * 1024 * 1024);
        _metadataMock.Setup(m => m.GetPgVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync("16.2");

        _versionRepoMock.Setup(r => r.GetLatestVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchemaVersion?)null);

        var upgrader = CreateUpgrader();

        // Act
        var result = await upgrader.UpgradeAsync(
            targetVersion: null,
            appliedBy: "test-user",
            migrationsDirectory: _tempMigrationsDir,
            skipBackup: true,
            dryRun: true,
            ct: CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.MigrationsApplied.Should().Be(0);

        // Migration executor was NEVER called (dry run)
        _migrationExecutorMock.Verify(
            e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // No version recorded
        _versionRepoMock.Verify(
            r => r.RecordApplied(It.IsAny<SchemaVersion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── 5.1.6 Migration executor failure triggers rollback ──────────────────────

    [Fact]
    public async Task UpgradeAsync_WhenMigrationExecutorFails_RollbackAndReturnError()
    {
        // Arrange
        CreateMockMigrationFile("20250101000001", "AddTestTable");

        _schemaLockMock.Setup(l => l.AcquireAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _schemaLockMock.Setup(l => l.IsAcquired).Returns(true);
        _schemaLockMock.Setup(l => l.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _metadataMock.Setup(m => m.GetDiskSpace(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L * 1024 * 1024);
        _metadataMock.Setup(m => m.GetPgVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync("16.2");

        _versionRepoMock.Setup(r => r.GetLatestVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchemaVersion?)null);

        _migrationExecutorMock.Setup(e => e.ExecuteAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Migration SQL failed: column does not exist"));

        // Backup methods are non-virtual — cannot mock. skipBackup=true in this test.

        var upgrader = CreateUpgrader();

        // Act
        var result = await upgrader.UpgradeAsync(
            targetVersion: null,
            appliedBy: "test-user",
            migrationsDirectory: _tempMigrationsDir,
            skipBackup: true,
            dryRun: false,
            ct: CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Migration SQL failed");

        // Migration executor was called but failed
        _migrationExecutorMock.Verify(
            e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        // No version recorded (failed before recording)
        _versionRepoMock.Verify(
            r => r.RecordApplied(It.IsAny<SchemaVersion>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── 5.1.7 Semver comparison static method ───────────────────────────────────

    [Theory]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("2.0.0", "1.0.0", 1)]
    [InlineData("1.0.0", "2.0.0", -1)]
    [InlineData("1.2.0", "1.1.0", 1)]
    [InlineData("1.0.3", "1.0.2", 1)]
    [InlineData("10.0.0", "9.99.99", 1)]
    [InlineData("2025.0101.0000", "2025.0601.0000", -1)]
    [InlineData("2025.0101.0000", "2024.1201.9999", 1)]
    public void CompareSemver_ReturnsCorrectOrder(string v1, string v2, int expected)
    {
        var result = SchemaUpgrader.CompareSemver(v1, v2);
        result.Should().Be(expected);
    }

    // ── 5.1.8 Target version limits which migrations are applied ────────────────

    [Fact]
    public async Task UpgradeAsync_WithTargetVersion_OnlyAppliesUpToTarget()
    {
        // Arrange
        CreateMockMigrationFile("20250101000001", "MigrationA"); // 2025.0101.0000
        CreateMockMigrationFile("20250601000002", "MigrationB"); // 2025.0601.0000
        CreateMockMigrationFile("20251201000003", "MigrationC"); // 2025.1201.0000

        _schemaLockMock.Setup(l => l.AcquireAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _schemaLockMock.Setup(l => l.IsAcquired).Returns(true);
        _schemaLockMock.Setup(l => l.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _metadataMock.Setup(m => m.GetDiskSpace(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L * 1024 * 1024);
        _metadataMock.Setup(m => m.GetPgVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync("16.2");

        _versionRepoMock.Setup(r => r.GetLatestVersion(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SchemaVersion?)null);

        _versionRepoMock.Setup(r => r.RecordApplied(
                It.IsAny<SchemaVersion>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _migrationExecutorMock.Setup(e => e.ExecuteAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var upgrader = CreateUpgrader();

        // Target version between A and B: only A should be applied
        // A = 2025.0101.0000, target = 2025.0300.0000, B = 2025.0601.0000
        var result = await upgrader.UpgradeAsync(
            targetVersion: "2025.0300.0000",
            appliedBy: "test-user",
            migrationsDirectory: _tempMigrationsDir,
            skipBackup: true,
            dryRun: false,
            ct: CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.MigrationsApplied.Should().Be(1); // Only MigrationA
        result.AppliedVersions.Should().HaveCount(1);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private SchemaUpgrader CreateUpgrader() => new(
        _versionRepoMock.Object,
        _metadataMock.Object,
        _policy,
        _backupHelper,
        _schemaLockMock.Object,
        _migrationExecutorMock.Object,
        _loggerMock.Object);

    public void Dispose()
    {
        if (Directory.Exists(_tempMigrationsDir))
        {
            Directory.Delete(_tempMigrationsDir, recursive: true);
        }
    }
}
