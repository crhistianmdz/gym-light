// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Domain.Interfaces;

/// <summary>
/// Acceso a metadatos del motor de base de datos (HU-017).
/// Proporciona información sobre el estado del servidor PostgreSQL y
/// control de concurrencia mediante advisory locks.
/// </summary>
public interface ISchemaMetadata
{
    /// <summary>
    /// Obtiene el espacio disponible en disco para el directorio de datos de PostgreSQL (en bytes).
    /// </summary>
    Task<long> GetDiskSpace(CancellationToken ct = default);

    /// <summary>
    /// Obtiene la versión del servidor PostgreSQL (ej: "16.2").
    /// </summary>
    Task<string> GetPgVersion(CancellationToken ct = default);

    /// <summary>
    /// Intenta adquirir un advisory lock de PostgreSQL con el ID especificado.
    /// Retorna true si el lock fue adquirido, false si ya está tomado.
    /// </summary>
    Task<bool> AcquireAdvisoryLock(int lockId, CancellationToken ct = default);

    /// <summary>
    /// Libera un advisory lock de PostgreSQL previamente adquirido.
    /// </summary>
    Task ReleaseAdvisoryLock(int lockId, CancellationToken ct = default);

    /// <summary>
    /// Verifica si un advisory lock con el ID especificado está actualmente tomado.
    /// </summary>
    Task<bool> IsLockHeld(int lockId, CancellationToken ct = default);
}
