// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Domain.Entities;

/// <summary>
/// Registro de migración de esquema aplicada (HU-017).
/// Cada fila representa una migración ejecutada exitosamente en la base de datos.
/// El Version es la PK y sigue formato semántico (semver: major.minor.patch).
/// </summary>
public class SchemaVersion
{
    /// <summary>Versión semántica aplicada (PK). Ej: "1.0.0", "2.3.1".</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Módulo o subsistema al que pertenece la migración.</summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>Timestamp UTC de cuando se aplicó la migración.</summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>Usuario que ejecutó la migración (email o username).</summary>
    public string AppliedBy { get; set; } = string.Empty;

    /// <summary>Descripción legible de la migración.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Hash del archivo .cs de migración para detección de modificaciones.</summary>
    public string MigrationHash { get; set; } = string.Empty;

    /// <summary>SQL de rollback para deshacer la migración si es necesario.</summary>
    public string RollbackSql { get; set; } = string.Empty;

    public static SchemaVersion Create(
        string version,
        string moduleName,
        string appliedBy,
        string description,
        string migrationHash,
        string rollbackSql)
    {
        return new SchemaVersion
        {
            Version = version,
            ModuleName = moduleName,
            AppliedBy = appliedBy,
            Description = description,
            MigrationHash = migrationHash,
            RollbackSql = rollbackSql,
            AppliedAt = DateTime.UtcNow
        };
    }
}
