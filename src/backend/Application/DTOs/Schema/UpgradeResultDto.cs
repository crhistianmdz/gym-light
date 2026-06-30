// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Application.DTOs.Schema;

/// <summary>
/// Resultado estructurado de una operación de upgrade de esquema (HU-017).
/// </summary>
public record UpgradeResultDto(
    bool Success,
    string? TargetVersion,
    int MigrationsApplied,
    string? BackupPath,
    string Duration,
    string? ErrorMessage,
    IReadOnlyList<string> AppliedVersions
);
