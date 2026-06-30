// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Application.UseCases.Schema;

using GymFlow.Application.Common;
using GymFlow.Application.DTOs.Schema;
using GymFlow.Domain.Interfaces;
using GymFlow.Infrastructure.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Caso de uso: Obtener el estado actual del esquema de base de datos (HU-017).
///
/// Consulta:
///   - schema_version: versión actual, última migración, versiones por módulo
///   - Sistema de archivos: migraciones pendientes (scan del directorio)
///   - PostgreSQL: espacio en disco, versión del motor, estado del advisory lock
///
/// Útil para: CLI `status`, dashboard de administración, monitoreo pre-upgrade.
/// </summary>
public class GetSchemaStatusUseCase
{
    private readonly ISchemaVersionRepository _versionRepo;
    private readonly ISchemaMetadata _metadata;
    private readonly ILogger<GetSchemaStatusUseCase> _logger;

    public GetSchemaStatusUseCase(
        ISchemaVersionRepository versionRepo,
        ISchemaMetadata metadata,
        ILogger<GetSchemaStatusUseCase> logger)
    {
        _versionRepo = versionRepo ?? throw new ArgumentNullException(nameof(versionRepo));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtiene el estado completo del esquema de base de datos.
    /// </summary>
    /// <param name="migrationsDirectory">Directorio donde se encuentran los archivos .cs de migración.</param>
    /// <param name="lockId">ID del advisory lock a verificar (default: 1701, HU-017).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Estado del esquema con versión actual, pendientes, espacio y módulos.</returns>
    public async Task<Result<SchemaStatusDto>> ExecuteAsync(
        string migrationsDirectory,
        int lockId = 1701,
        CancellationToken ct = default)
    {
        try
        {
            // ── Versión actual ──────────────────────────────────────────────
            var latestVersion = await _versionRepo.GetLatestVersion(ct);

            // ── Versiones por módulo ────────────────────────────────────────
            var moduleVersions = await GetModuleVersionsAsync(ct);

            // ── Migraciones pendientes ──────────────────────────────────────
            var currentVersion = latestVersion?.Version ?? "0.0.0";
            var pendingCount = CountPendingMigrations(migrationsDirectory, currentVersion);

            // ── Espacio en disco ────────────────────────────────────────────
            long diskSpaceBytes;
            try
            {
                diskSpaceBytes = await _metadata.GetDiskSpace(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo obtener el espacio en disco.");
                diskSpaceBytes = -1;
            }

            // ── Versión PostgreSQL ──────────────────────────────────────────
            string pgVersion;
            try
            {
                pgVersion = await _metadata.GetPgVersion(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo obtener la versión de PostgreSQL.");
                pgVersion = "unknown";
            }

            // ── Estado del advisory lock ────────────────────────────────────
            bool isLockHeld;
            try
            {
                isLockHeld = await _metadata.IsLockHeld(lockId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo verificar el advisory lock.");
                isLockHeld = false;
            }

            // ── Construir DTO ───────────────────────────────────────────────
            var dto = new SchemaStatusDto(
                CurrentVersion: currentVersion,
                LastMigrationAt: latestVersion?.AppliedAt,
                LastMigrationDescription: latestVersion?.Description,
                PendingMigrationsCount: pendingCount,
                DiskSpaceBytes: diskSpaceBytes,
                DiskSpaceFormatted: FormatBytes(diskSpaceBytes),
                PgVersion: pgVersion,
                IsLockHeld: isLockHeld,
                ModuleVersions: moduleVersions
            );

            _logger.LogInformation(
                "Estado del esquema: v{Version}, {Pending} pendientes, {Space} libres, PG {PgVer}.",
                currentVersion, pendingCount, FormatBytes(diskSpaceBytes), pgVersion);

            return Result<SchemaStatusDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el estado del esquema.");
            return Result<SchemaStatusDto>.InternalError(
                $"Error al obtener el estado del esquema: {ex.Message}");
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene la última versión aplicada por cada módulo del sistema.
    /// </summary>
    private async Task<IReadOnlyList<ModuleVersionDto>> GetModuleVersionsAsync(CancellationToken ct)
    {
        var modules = new List<ModuleVersionDto>();

        // Módulos conocidos del sistema
        var knownModules = new[]
        {
            "core", "members", "sales", "access",
            "routines", "anthropometry", "plugins"
        };

        foreach (var moduleName in knownModules)
        {
            try
            {
                var versions = await _versionRepo.GetByModule(moduleName, ct);
                var latest = versions
                    .OrderByDescending(v => v.AppliedAt)
                    .FirstOrDefault();

                if (latest != null)
                {
                    modules.Add(new ModuleVersionDto(
                        ModuleName: moduleName,
                        Version: latest.Version,
                        AppliedAt: latest.AppliedAt
                    ));
                }
                else
                {
                    modules.Add(new ModuleVersionDto(
                        ModuleName: moduleName,
                        Version: "—",
                        AppliedAt: DateTime.MinValue
                    ));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al consultar versiones del módulo {Module}.", moduleName);
            }
        }

        return modules;
    }

    /// <summary>
    /// Cuenta las migraciones pendientes escaneando el directorio de archivos .cs
    /// y comparando versiones semánticas con la versión actual.
    /// </summary>
    private static int CountPendingMigrations(string migrationsDirectory, string currentVersion)
    {
        if (!Directory.Exists(migrationsDirectory))
            return 0;

        var migrationFiles = Directory.GetFiles(migrationsDirectory, "*.cs")
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var pending = 0;
        foreach (var file in migrationFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var parts = fileName.Split('_', 2);
            if (parts.Length < 2)
                continue;

            var timestamp = parts[0];
            if (timestamp.Length < 12)
                continue;

            var fileVersion = $"{timestamp[..4]}.{timestamp[4..8]}.{timestamp[8..12]}";

            if (SchemaUpgrader.CompareSemver(fileVersion, currentVersion) > 0)
                pending++;
        }

        return pending;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0)
            return "N/A";

        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {suffixes[order]}";
    }
}
