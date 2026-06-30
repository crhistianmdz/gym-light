// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Application.UseCases.Schema;

using GymFlow.Application.Common;
using GymFlow.Application.DTOs.Schema;
using GymFlow.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

/// <summary>
/// Caso de uso: Ejecutar un upgrade de esquema de base de datos (HU-017).
///
/// Orquesta el SchemaUpgrader con validación de parámetros y mapeo de resultados.
/// Soporta los flags: --target, --skip-backup, --dry-run, --verbose.
///
/// Flujo:
///   1. Validar parámetros (targetVersion formato semver, appliedBy no vacío)
///   2. Delegar al SchemaUpgrader.UpgradeAsync()
///   3. Mapear UpgradeResult → UpgradeResultDto
///   4. Retornar Result estructurado
///
/// El verbose logging se habilita via ILogger configurado por el caller.
/// </summary>
public class UpgradeSchemaUseCase
{
    private readonly SchemaUpgrader _upgrader;
    private readonly ILogger<UpgradeSchemaUseCase> _logger;

    public UpgradeSchemaUseCase(
        SchemaUpgrader upgrader,
        ILogger<UpgradeSchemaUseCase> logger)
    {
        _upgrader = upgrader ?? throw new ArgumentNullException(nameof(upgrader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ejecuta el upgrade de esquema hacia la versión objetivo.
    /// </summary>
    /// <param name="targetVersion">Versión semántica objetivo (ej: "2.0.0"). Null = todas las pendientes.</param>
    /// <param name="appliedBy">Usuario que ejecuta el upgrade (email o username). Requerido para trazabilidad.</param>
    /// <param name="migrationsDirectory">Directorio donde se encuentran los archivos .cs de migración.</param>
    /// <param name="skipBackup">Si es true, omite el backup pre-upgrade. NO recomendado en producción.</param>
    /// <param name="dryRun">Si es true, valida todo pero no aplica cambios.</param>
    /// <param name="verbose">Si es true, habilita logging detallado de cada paso.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado estructurado del upgrade.</returns>
    public async Task<Result<UpgradeResultDto>> ExecuteAsync(
        string? targetVersion,
        string appliedBy,
        string migrationsDirectory,
        bool skipBackup = false,
        bool dryRun = false,
        bool verbose = false,
        CancellationToken ct = default)
    {
        // ── Validación de parámetros ────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(appliedBy))
        {
            _logger.LogError("Upgrade rechazado: appliedBy es requerido.");
            return Result<UpgradeResultDto>.ValidationError(
                "El parámetro 'appliedBy' es requerido (email o username del operador).");
        }

        if (string.IsNullOrWhiteSpace(migrationsDirectory))
        {
            _logger.LogError("Upgrade rechazado: migrationsDirectory es requerido.");
            return Result<UpgradeResultDto>.ValidationError(
                "El parámetro 'migrationsDirectory' es requerido.");
        }

        if (!Directory.Exists(migrationsDirectory))
        {
            _logger.LogError("Upgrade rechazado: directorio de migraciones no encontrado: {Dir}", migrationsDirectory);
            return Result<UpgradeResultDto>.ValidationError(
                $"Directorio de migraciones no encontrado: {migrationsDirectory}");
        }

        // Validar formato de targetVersion si se especifica
        if (targetVersion != null && !IsValidSemver(targetVersion))
        {
            _logger.LogError("Upgrade rechazado: targetVersion con formato inválido: {Version}", targetVersion);
            return Result<UpgradeResultDto>.ValidationError(
                $"Formato de versión inválido: '{targetVersion}'. Usar semver (ej: 1.2.3).");
        }

        // ── Logging verbose ─────────────────────────────────────────────────────
        if (verbose)
        {
            _logger.LogInformation(
                "Upgrade iniciado — Target: {Target}, AppliedBy: {User}, Dir: {Dir}, SkipBackup: {SkipBackup}, DryRun: {DryRun}",
                targetVersion ?? "latest", appliedBy, migrationsDirectory, skipBackup, dryRun);
        }

        // ── Delegar al SchemaUpgrader ───────────────────────────────────────────
        try
        {
            var result = await _upgrader.UpgradeAsync(
                targetVersion,
                appliedBy,
                migrationsDirectory,
                skipBackup,
                dryRun,
                ct);

            var dto = new UpgradeResultDto(
                Success: result.Success,
                TargetVersion: result.TargetVersion,
                MigrationsApplied: result.MigrationsApplied,
                BackupPath: result.BackupPath,
                Duration: FormatDuration(result.Duration),
                ErrorMessage: result.ErrorMessage,
                AppliedVersions: result.AppliedVersions
            );

            if (result.Success)
            {
                _logger.LogInformation(
                    "Upgrade completado: {Count} migraciones aplicadas en {Duration}.",
                    result.MigrationsApplied, FormatDuration(result.Duration));

                if (verbose && result.MigrationsApplied > 0)
                {
                    _logger.LogInformation(
                        "Versiones aplicadas: {Versions}",
                        string.Join(" → ", result.AppliedVersions));
                }

                return Result<UpgradeResultDto>.Success(dto);
            }

            // Fallo manejado por el upgrader (lock ocupado, pre-check fallido, migración rota...)
            _logger.LogWarning(
                "Upgrade fallido: {Error}. Migraciones aplicadas antes del fallo: {Count}.",
                result.ErrorMessage, result.AppliedVersions.Count);

            return Result<UpgradeResultDto>.Conflict(
                result.ErrorMessage ?? "Upgrade fallido por causa desconocida.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado durante el upgrade.");
            return Result<UpgradeResultDto>.InternalError(
                $"Error inesperado durante el upgrade: {ex.Message}");
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static readonly Regex SemverPattern = new(
        @"^\d+\.\d+\.\d+$", RegexOptions.Compiled);

    private static bool IsValidSemver(string version)
    {
        return SemverPattern.IsMatch(version);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMinutes >= 1)
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        if (duration.TotalSeconds >= 1)
            return $"{duration.Seconds}s {duration.Milliseconds}ms";
        return $"{duration.Milliseconds}ms";
    }
}
