// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Application.UseCases.Schema;

using GymFlow.Application.Common;
using GymFlow.Application.DTOs.Schema;
using GymFlow.Domain.Interfaces;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Caso de uso: Validar la consistencia del esquema de base de datos (HU-017).
///
/// Verifica:
///   1. Correspondencia entre schema_version y __EFMigrationsHistory
///      - Migraciones en EF history que no están en schema_version (huérfanas)
///      - Migraciones en schema_version que no están en EF history (huérfanas inversas)
///   2. Política aditiva: escanea archivos .cs de migración y detecta violaciones
///
/// La validación es de solo lectura — no modifica datos.
/// </summary>
public class ValidateSchemaUseCase
{
    private readonly GymFlowDbContext _db;
    private readonly ISchemaVersionRepository _versionRepo;
    private readonly MigrationPolicy _migrationPolicy;
    private readonly ILogger<ValidateSchemaUseCase> _logger;

    public ValidateSchemaUseCase(
        GymFlowDbContext db,
        ISchemaVersionRepository versionRepo,
        MigrationPolicy migrationPolicy,
        ILogger<ValidateSchemaUseCase> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _versionRepo = versionRepo ?? throw new ArgumentNullException(nameof(versionRepo));
        _migrationPolicy = migrationPolicy ?? throw new ArgumentNullException(nameof(migrationPolicy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ejecuta la validación completa de consistencia del esquema.
    /// </summary>
    /// <param name="migrationsDirectory">Directorio donde se encuentran los archivos .cs de migración.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado de validación con errores, warnings, huérfanos y violaciones de política.</returns>
    public async Task<Result<SchemaValidationResultDto>> ExecuteAsync(
        string migrationsDirectory,
        CancellationToken ct = default)
    {
        try
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var orphanedMigrations = new List<OrphanedMigrationDto>();
            var policyViolations = new List<MigrationPolicyViolationDto>();

            // ── Check 1: schema_version ↔ __EFMigrationsHistory ──────────────
            _logger.LogInformation("Validando correspondencia schema_version ↔ __EFMigrationsHistory...");

            var efMigrations = await GetEfMigrationsHistoryAsync(ct);
            var schemaVersions = await GetAllSchemaVersionsAsync(ct);

            var efMigrationIds = new HashSet<string>(efMigrations);
            var schemaVersionIds = new HashSet<string>(schemaVersions.Select(sv => sv.Version));

            // Migraciones en EF history pero no en schema_version
            var efOnly = efMigrationIds.Except(schemaVersionIds).ToList();
            foreach (var migrationId in efOnly)
            {
                var orphan = new OrphanedMigrationDto(
                    MigrationId: migrationId,
                    Source: "ef_history_only",
                    FilePath: FindMigrationFile(migrationId, migrationsDirectory),
                    Description: $"Migración '{migrationId}' está en __EFMigrationsHistory pero NO en schema_version."
                );
                orphanedMigrations.Add(orphan);
                warnings.Add(
                    $"Migración huérfana (ef_history_only): {migrationId}. " +
                    "Fue aplicada directamente sin registrar en schema_version. " +
                    "Ejecutar 'gymflow doctor --fix' para sincronizar.");
            }

            // Migraciones en schema_version pero no en EF history
            var svOnly = schemaVersionIds.Except(efMigrationIds).ToList();
            foreach (var version in svOnly)
            {
                var svEntry = schemaVersions.First(sv => sv.Version == version);
                var orphan = new OrphanedMigrationDto(
                    MigrationId: version,
                    Source: "schema_version_only",
                    FilePath: null,
                    Description: svEntry.Description
                );
                orphanedMigrations.Add(orphan);
                errors.Add(
                    $"Registro fantasma (schema_version_only): v{version} ({svEntry.Description}). " +
                    "Registrado en schema_version pero NO existe en __EFMigrationsHistory. " +
                    "Posible rollback manual o inserción directa. Requiere intervención manual.");
            }

            if (efOnly.Count == 0 && svOnly.Count == 0)
            {
                _logger.LogInformation("schema_version ↔ __EFMigrationsHistory: OK ✓");
            }
            else
            {
                _logger.LogWarning(
                    "Discrepancias detectadas: {EFOnly} solo en EF, {SVOnly} solo en schema_version.",
                    efOnly.Count, svOnly.Count);
            }

            // ── Check 2: Política aditiva ────────────────────────────────────
            _logger.LogInformation("Validando política aditiva en archivos de migración...");

            if (Directory.Exists(migrationsDirectory))
            {
                var violations = await _migrationPolicy.ValidateDirectoryAsync(migrationsDirectory);

                if (violations.Count > 0)
                {
                    _logger.LogWarning(
                        "Violaciones de política aditiva detectadas: {Count}.", violations.Count);

                    foreach (var v in violations)
                    {
                        policyViolations.Add(new MigrationPolicyViolationDto(
                            FilePath: v.FilePath,
                            LineNumber: v.LineNumber,
                            Operation: v.Operation,
                            Reason: v.Reason
                        ));

                        errors.Add(
                            $"{Path.GetFileName(v.FilePath)}:{v.LineNumber}: {v.Operation} — {v.Reason}");
                    }
                }
                else
                {
                    _logger.LogInformation("Política aditiva: OK ✓");
                }
            }
            else
            {
                warnings.Add(
                    $"Directorio de migraciones no encontrado: {migrationsDirectory}. " +
                    "No se pudo validar la política aditiva.");
            }

            // ── Resultado ────────────────────────────────────────────────────
            var isValid = errors.Count == 0;

            var dto = new SchemaValidationResultDto(
                IsValid: isValid,
                Errors: errors,
                Warnings: warnings,
                OrphanedMigrations: orphanedMigrations,
                PolicyViolations: policyViolations
            );

            _logger.LogInformation(
                "Validación completada: {Status}. Errores: {Errors}, Warnings: {Warnings}, Huérfanos: {Orphans}.",
                isValid ? "VÁLIDO ✓" : "INVÁLIDO ✗",
                errors.Count, warnings.Count, orphanedMigrations.Count);

            return Result<SchemaValidationResultDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la validación del esquema.");
            return Result<SchemaValidationResultDto>.InternalError(
                $"Error durante la validación del esquema: {ex.Message}");
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene los MigrationId de la tabla __EFMigrationsHistory de EF Core.
    /// </summary>
    private async Task<List<string>> GetEfMigrationsHistoryAsync(CancellationToken ct)
    {
        try
        {
            // La tabla __EFMigrationsHistory es gestionada por EF Core.
            // Usamos raw SQL porque no tiene una entidad mapeada en el DbContext.
            var connection = _db.Database.GetDbConnection();

            // Asegurar que la conexión está abierta
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync(ct);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\"";

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            var migrations = new List<string>();
            while (await reader.ReadAsync(ct))
            {
                migrations.Add(reader.GetString(0));
            }

            return migrations;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "No se pudo leer __EFMigrationsHistory. ¿La tabla existe? " +
                "Si es la primera ejecución, ejecutar las migraciones primero.");
            return [];
        }
    }

    /// <summary>
    /// Obtiene todas las versiones registradas en schema_version.
    /// Como no tenemos GetAll en el repositorio, usamos GetByModule para los módulos conocidos
    /// y deduplicamos por Version.
    /// </summary>
    private async Task<List<Domain.Entities.SchemaVersion>> GetAllSchemaVersionsAsync(CancellationToken ct)
    {
        var allVersions = new List<Domain.Entities.SchemaVersion>();
        var seen = new HashSet<string>();

        var knownModules = new[]
        {
            "core", "members", "sales", "access",
            "routines", "anthropometry", "plugins"
        };

        foreach (var module in knownModules)
        {
            try
            {
                var versions = await _versionRepo.GetByModule(module, ct);
                foreach (var v in versions)
                {
                    if (seen.Add(v.Version))
                        allVersions.Add(v);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al consultar schema_version para módulo {Module}.", module);
            }
        }

        return allVersions;
    }

    /// <summary>
    /// Busca el archivo .cs de migración correspondiente a un MigrationId de EF Core.
    /// El MigrationId tiene formato: YYYYMMDDHHmmss_Description.
    /// </summary>
    private static string? FindMigrationFile(string migrationId, string migrationsDirectory)
    {
        if (!Directory.Exists(migrationsDirectory))
            return null;

        // El MigrationId de EF es el nombre del archivo sin extensión
        var expectedFileName = $"{migrationId}.cs";
        var fullPath = Path.Combine(migrationsDirectory, expectedFileName);

        if (File.Exists(fullPath))
            return fullPath;

        // Buscar por prefijo de timestamp (puede haber variaciones en el nombre)
        var timestamp = migrationId.Split('_')[0];
        var candidates = Directory.GetFiles(migrationsDirectory, $"{timestamp}_*.cs")
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return candidates.FirstOrDefault();
    }
}
