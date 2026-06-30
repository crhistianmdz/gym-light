// GymFlow Lite - Schema Versioning Tests (HU-017 Phase 5.2)
// Copyright (C) 2026 GymFlow contributors
// License: AGPL v3 (see LICENSE)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using GymFlow.Infrastructure.Services;
using Xunit;

namespace GymFlow.Tests.Schema;

/// <summary>
/// Unit tests for MigrationPolicy — validates that the additive-only
/// migration policy correctly blocks destructive operations and allows
/// safe additive ones.
///
/// Policy rules (ADR-007, HU-017):
///   BLOQUEADO: DropColumn, RenameColumn, incompatible AlterColumn
///   PERMITIDO: AddColumn, CreateTable, CreateIndex
/// </summary>
public class MigrationPolicyTests
{
    private readonly MigrationPolicy _policy;
    private readonly string _tempDir;

    public MigrationPolicyTests()
    {
        _policy = new MigrationPolicy();
        _tempDir = Path.Combine(Path.GetTempPath(), $"gymflow-policy-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Creates a migration .cs file with the given content at a known path.
    /// </summary>
    private string CreateMigrationFile(string fileName, string migrationCode)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        var content = $@"using Microsoft.EntityFrameworkCore.Migrations;

public partial class TestMigration : Migration
{{
    protected override void Up(MigrationBuilder migrationBuilder)
    {{
        {migrationCode}
    }}

    protected override void Down(MigrationBuilder migrationBuilder)
    {{
    }}
}}";

        File.WriteAllText(filePath, content);
        return filePath;
    }

    // ── 5.2.1 Block DropColumn ──────────────────────────────────────────────────

    [Fact]
    public void ValidateFile_WithDropColumn_ReportsViolation()
    {
        // Arrange
        var filePath = CreateMigrationFile("20250101000001_DropColumnTest.cs",
            @"migrationBuilder.DropColumn(
                name: ""old_column"",
                table: ""members"");");

        // Act
        var violations = _policy.ValidateFile(filePath);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Operation.Should().Be("DropColumn");
        violations[0].Reason.Should().Contain("pérdida irreversible");
    }

    // ── 5.2.2 Block RenameColumn ────────────────────────────────────────────────

    [Fact]
    public void ValidateFile_WithRenameColumn_ReportsViolation()
    {
        // Arrange
        var filePath = CreateMigrationFile("20250101000002_RenameTest.cs",
            @"migrationBuilder.RenameColumn(
                name: ""old_name"",
                table: ""members"",
                newName: ""new_name"");");

        // Act
        var violations = _policy.ValidateFile(filePath);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Operation.Should().Be("RenameColumn");
        violations[0].Reason.Should().Contain("rompe referencias");
    }

    // ── 5.2.3 Block incompatible AlterColumn (type change) ──────────────────────

    [Fact]
    public void ValidateFile_WithAlterColumnTypeChange_ReportsViolation()
    {
        // Arrange
        var filePath = CreateMigrationFile("20250101000003_AlterTypeTest.cs",
            @"migrationBuilder.AlterColumn<int>(
                name: ""age"",
                table: ""members"",
                type: ""integer"",
                oldClrType: typeof(string),
                oldType: ""text"",
                nullable: false);");

        // Act
        var violations = _policy.ValidateFile(filePath);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Operation.Should().Be("AlterColumn");
        violations[0].Reason.Should().Contain("Cambio de tipo incompatible")
            .And.Contain("int")
            .And.Contain("string");
    }

    // ── 5.2.4 Block incompatible AlterColumn (maxLength reduction) ──────────────

    [Fact]
    public void ValidateFile_WithMaxLengthReduction_ReportsViolation()
    {
        // Arrange
        var filePath = CreateMigrationFile("20250101000004_MaxLengthTest.cs",
            @"migrationBuilder.AlterColumn<string>(
                name: ""description"",
                table: ""products"",
                type: ""character varying(50)"",
                maxLength: 50,
                oldClrType: typeof(string),
                oldType: ""character varying(200)"",
                oldMaxLength: 200,
                nullable: true);");

        // Act
        var violations = _policy.ValidateFile(filePath);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Operation.Should().Be("AlterColumn");
        violations[0].Reason.Should().Contain("Reducción de maxLength");
    }

    // ── 5.2.5 Block adding maxLength where none existed ─────────────────────────

    [Fact]
    public void ValidateFile_WithNewMaxLengthOnUnconstrainedColumn_ReportsViolation()
    {
        // Arrange
        var filePath = CreateMigrationFile("20250101000005_NewMaxTest.cs",
            @"migrationBuilder.AlterColumn<string>(
                name: ""notes"",
                table: ""members"",
                type: ""character varying(256)"",
                maxLength: 256,
                oldClrType: typeof(string),
                oldType: ""text"",
                oldMaxLength: null,
                nullable: true);");

        // Act
        var violations = _policy.ValidateFile(filePath);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Operation.Should().Be("AlterColumn");
        violations[0].Reason.Should().Contain("restricción de datos");
    }

    // ── 5.2.6 Allow ADD COLUMN ──────────────────────────────────────────────────

    [Fact]
    public void ValidateFile_WithAddColumn_NoViolation()
    {
        // Arrange
        var filePath = CreateMigrationFile("20250101000006_AddColumnTest.cs",
            @"migrationBuilder.AddColumn<string>(
                name: ""new_field"",
                table: ""members"",
                type: ""text"",
                nullable: true);");

        // Act
        var violations = _policy.ValidateFile(filePath);

        // Assert
        violations.Should().BeEmpty();
    }

    // ── 5.2.7 Allow CREATE TABLE ────────────────────────────────────────────────

    [Fact]
    public void ValidateFile_WithCreateTable_NoViolation()
    {
        // Arrange
        var filePath = CreateMigrationFile("20250101000007_CreateTableTest.cs",
            @"migrationBuilder.CreateTable(
                name: ""new_entity"",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: ""uuid"", nullable: false),
                    Name = table.Column<string>(type: ""text"", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(""PK_new_entity"", x => x.Id);
                });");

        // Act
        var violations = _policy.ValidateFile(filePath);

        // Assert
        violations.Should().BeEmpty();
    }

    // ── 5.2.8 Allow CREATE INDEX ────────────────────────────────────────────────

    [Fact]
    public void ValidateFile_WithCreateIndex_NoViolation()
    {
        // Arrange
        var filePath = CreateMigrationFile("20250101000008_CreateIndexTest.cs",
            @"migrationBuilder.CreateIndex(
                name: ""IX_members_email"",
                table: ""members"",
                column: ""email"",
                unique: true);");

        // Act
        var violations = _policy.ValidateFile(filePath);

        // Assert
        violations.Should().BeEmpty();
    }

    // ── 5.2.9 Allow compatible AlterColumn (increase maxLength) ─────────────────

    [Fact]
    public void ValidateFile_WithMaxLengthIncrease_NoViolation()
    {
        // Arrange
        var filePath = CreateMigrationFile("20250101000009_IncreaseMaxTest.cs",
            @"migrationBuilder.AlterColumn<string>(
                name: ""description"",
                table: ""products"",
                type: ""character varying(500)"",
                maxLength: 500,
                oldClrType: typeof(string),
                oldType: ""character varying(200)"",
                oldMaxLength: 200,
                nullable: true);");

        // Act
        var violations = _policy.ValidateFile(filePath);

        // Assert
        violations.Should().BeEmpty();
    }

    // ── 5.2.10 Allow compatible AlterColumn (same type, make nullable) ──────────

    [Fact]
    public void ValidateFile_WithSameTypeNullableChange_NoViolation()
    {
        // Arrange
        var filePath = CreateMigrationFile("20250101000010_NullableTest.cs",
            @"migrationBuilder.AlterColumn<string>(
                name: ""middle_name"",
                table: ""members"",
                type: ""text"",
                oldClrType: typeof(string),
                oldType: ""text"",
                nullable: true);");

        // Act
        var violations = _policy.ValidateFile(filePath);

        // Assert
        violations.Should().BeEmpty();
    }

    // ── 5.2.11 ValidateDirectory aggregates violations ──────────────────────────

    [Fact]
    public async Task ValidateDirectoryAsync_WithMultipleViolations_ReportsAll()
    {
        // Arrange
        CreateMigrationFile("20250101000011_DropA.cs",
            @"migrationBuilder.DropColumn(name: ""col_a"", table: ""t"");");
        CreateMigrationFile("20250101000012_DropB.cs",
            @"migrationBuilder.DropColumn(name: ""col_b"", table: ""t"");");
        CreateMigrationFile("20250101000013_Rename.cs",
            @"migrationBuilder.RenameColumn(name: ""old"", table: ""t"", newName: ""new"");");

        // Act
        var violations = await _policy.ValidateDirectoryAsync(_tempDir);

        // Assert
        violations.Should().HaveCount(3);
        violations.Should().AllSatisfy(v =>
            v.Operation.Should().BeOneOf("DropColumn", "RenameColumn"));
    }

    // ── 5.2.12 IsValidAsync shortcut ────────────────────────────────────────────

    [Fact]
    public async Task IsValidAsync_WithNoViolations_ReturnsTrue()
    {
        // Arrange
        CreateMigrationFile("20250101000014_Good.cs",
            @"migrationBuilder.AddColumn<string>(name: ""new_col"", table: ""t"", type: ""text"");");

        // Act
        var isValid = await _policy.IsValidAsync(_tempDir);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task IsValidAsync_WithViolations_ReturnsFalse()
    {
        // Arrange
        CreateMigrationFile("20250101000015_Bad.cs",
            @"migrationBuilder.DropColumn(name: ""col"", table: ""t"");");

        // Act
        var isValid = await _policy.IsValidAsync(_tempDir);

        // Assert
        isValid.Should().BeFalse();
    }

    // ── 5.2.13 Policy ignores Designer.cs files ─────────────────────────────────

    [Fact]
    public async Task ValidateDirectoryAsync_IgnoresDesignerFiles()
    {
        // Arrange
        var designerContent = @"// Auto-generated
using Microsoft.EntityFrameworkCore.Migrations;
// DropColumn is in a comment, should be ignored
public partial class TestMigration : Migration { }";

        var designerPath = Path.Combine(_tempDir, "20250101000016_Test.Designer.cs");
        File.WriteAllText(designerPath, designerContent);

        // Also create the non-designer version with a violation
        CreateMigrationFile("20250101000016_Test.cs",
            @"migrationBuilder.DropColumn(name: ""col"", table: ""t"");");

        // Act
        var violations = await _policy.ValidateDirectoryAsync(_tempDir);

        // Assert
        // Only the non-designer file should be checked
        violations.Should().HaveCount(1);
    }

    // ── 5.2.14 Null/empty directory handling ────────────────────────────────────

    [Fact]
    public async Task ValidateDirectoryAsync_WithNonexistentDirectory_ReturnsEmpty()
    {
        var violations = await _policy.ValidateDirectoryAsync("/nonexistent/path/12345");
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateFile_WithNonexistentFile_ReturnsEmpty()
    {
        var violations = _policy.ValidateFile("/nonexistent/file.cs");
        violations.Should().BeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
