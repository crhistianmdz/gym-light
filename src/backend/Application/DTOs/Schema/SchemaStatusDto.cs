// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Application.DTOs.Schema;

/// <summary>
/// Estado actual del esquema de base de datos (HU-017).
/// Incluye versión actual, migraciones pendientes, espacio en disco y estado por módulo.
/// </summary>
public record SchemaStatusDto(
    string CurrentVersion,
    DateTime? LastMigrationAt,
    string? LastMigrationDescription,
    int PendingMigrationsCount,
    long DiskSpaceBytes,
    string DiskSpaceFormatted,
    string PgVersion,
    bool IsLockHeld,
    IReadOnlyList<ModuleVersionDto> ModuleVersions
);

/// <summary>
/// Versión de esquema por módulo del sistema.
/// </summary>
public record ModuleVersionDto(
    string ModuleName,
    string Version,
    DateTime AppliedAt
);
