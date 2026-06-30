// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Application.DTOs.Schema;

/// <summary>
/// Resultado de validación de consistencia del esquema (HU-017).
/// </summary>
public record SchemaValidationResultDto(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<OrphanedMigrationDto> OrphanedMigrations,
    IReadOnlyList<MigrationPolicyViolationDto> PolicyViolations
);

/// <summary>
/// Migración huérfana: presente en __EFMigrationsHistory pero no en schema_version,
/// o presente en schema_version pero no en __EFMigrationsHistory.
/// </summary>
public record OrphanedMigrationDto(
    string MigrationId,
    string Source, // "ef_history_only" | "schema_version_only"
    string? FilePath,
    string? Description
);

/// <summary>
/// Violación de política aditiva detectada en archivo de migración.
/// </summary>
public record MigrationPolicyViolationDto(
    string FilePath,
    int LineNumber,
    string Operation,
    string Reason
);
