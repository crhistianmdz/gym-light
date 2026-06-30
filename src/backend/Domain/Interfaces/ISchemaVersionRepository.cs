// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Interfaces;

/// <summary>
/// Repositorio para el registro de versiones de esquema (HU-017).
/// </summary>
public interface ISchemaVersionRepository
{
    /// <summary>
    /// Obtiene la última versión aplicada (la mayor según orden semántico).
    /// </summary>
    Task<SchemaVersion?> GetLatestVersion(CancellationToken ct = default);

    /// <summary>
    /// Obtiene las migraciones pendientes entre dos versiones (exclusivo superior).
    /// Usado por el upgrader para determinar qué migraciones aplicar.
    /// </summary>
    Task<IReadOnlyList<SchemaVersion>> GetPendingBetween(
        string fromVersion,
        string toVersion,
        CancellationToken ct = default);

    /// <summary>
    /// Registra una migración como aplicada exitosamente.
    /// </summary>
    Task RecordApplied(SchemaVersion entry, CancellationToken ct = default);

    /// <summary>
    /// Obtiene todas las migraciones aplicadas para un módulo específico.
    /// </summary>
    Task<IReadOnlyList<SchemaVersion>> GetByModule(
        string moduleName,
        CancellationToken ct = default);
}
