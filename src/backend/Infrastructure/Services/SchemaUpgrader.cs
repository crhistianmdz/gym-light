// GymFlow Lite - Schema Versioning (HU-017)
// Copyright (C) 2026 GymFlow contributors
// License: AGPL v3 (see LICENSE)

using System.Diagnostics;
using System.IO;
using GymFlow.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Orquestador del proceso de upgrade de esquema (HU-017).
/// 
/// Implementa el algoritmo completo de upgrade:
/// 1. Adquirir advisory lock (exclusión mutua entre procesos)
/// 2. Pre-checks (validar política de migraciones, espacio en disco, versión PG)
/// 3. Backup completo de la base de datos
/// 4. Aplicar migraciones pendientes en orden semántico
/// 5. Registrar cada migración en schema_version
/// 6. Liberar advisory lock
/// 
/// En caso de fallo en cualquier paso:
/// - Rollback de transacción actual
/// - Restauración del backup
/// - Liberación del lock
/// </summary>
public class SchemaUpgrader
{
    private readonly ISchemaVersionRepository _versionRepo;
    private readonly ISchemaMetadata _metadata;
    private readonly MigrationPolicy _migrationPolicy;
    private readonly BackupHelper _backupHelper;
    private readonly ISchemaLock _schemaLock;
    private readonly IMigrationExecutor _migrationExecutor;
    private readonly ILogger<SchemaUpgrader> _logger;

    // Umbrales de seguridad
    private const long MinimumDiskSpaceBytes = 100L * 1024 * 1024; // 100 MB
    private const string MinimumPgVersion = "14.0";

    /// <param name="versionRepo">Repositorio para tracking de versiones de esquema.</param>
    /// <param name="metadata">Acceso a metadatos del servidor PostgreSQL.</param>
    /// <param name="migrationPolicy">Validador de política aditiva.</param>
    /// <param name="backupHelper">Helper de backup/restore.</param>
    /// <param name="schemaLock">Control de concurrencia via advisory lock.</param>
    /// <param name="migrationExecutor">Ejecutor de migraciones EF Core.</param>
    /// <param name="logger">Logger estructurado.</param>
    public SchemaUpgrader(
        ISchemaVersionRepository versionRepo,
        ISchemaMetadata metadata,
        MigrationPolicy migrationPolicy,
        BackupHelper backupHelper,
        ISchemaLock schemaLock,
        IMigrationExecutor migrationExecutor,
        ILogger<SchemaUpgrader> logger)
    {
        _versionRepo = versionRepo ?? throw new ArgumentNullException(nameof(versionRepo));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _migrationPolicy = migrationPolicy ?? throw new ArgumentNullException(nameof(migrationPolicy));
        _backupHelper = backupHelper ?? throw new ArgumentNullException(nameof(backupHelper));
        _schemaLock = schemaLock ?? throw new ArgumentNullException(nameof(schemaLock));
        _migrationExecutor = migrationExecutor ?? throw new ArgumentNullException(nameof(migrationExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resultado del proceso de upgrade.
    /// </summary>
    public record UpgradeResult(
        bool Success,
        string? TargetVersion,
        int MigrationsApplied,
        string? BackupPath,
        TimeSpan Duration,
        string? ErrorMessage,
        IReadOnlyList<string> AppliedVersions
    );

    /// <summary>
    /// Ejecuta el upgrade completo de esquema hacia la versión objetivo.
    /// 
    /// Si targetVersion es null, aplica TODAS las migraciones pendientes.
    /// </summary>
    /// <param name="targetVersion">Versión semántica objetivo (ej: "2.0.0"). Null = todas las pendientes.</param>
    /// <param name="appliedBy">Usuario que ejecuta el upgrade (email o username).</param>
    /// <param name="migrationsDirectory">Directorio de archivos de migración .cs.</param>
    /// <param name="skipBackup">Si es true, omite el backup pre-upgrade (NO recomendado en producción).</param>
    /// <param name="dryRun">Si es true, valida todo pero no aplica cambios.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Resultado estructurado del upgrade.</returns>
    public async Task<UpgradeResult> UpgradeAsync(
        string? targetVersion,
        string appliedBy,
        string migrationsDirectory,
        bool skipBackup = false,
        bool dryRun = false,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        string? backupPath = null;
        var appliedVersions = new List<string>();
        var migrationsApplied = 0;

        // ── Paso 0: Validar parámetros ──────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(appliedBy))
            throw new ArgumentException("appliedBy es requerido (email o username del operador).", nameof(appliedBy));

        if (!Directory.Exists(migrationsDirectory))
            throw new DirectoryNotFoundException($"Directorio de migraciones no encontrado: {migrationsDirectory}");

        try
        {
            // ── Paso 1: Adquirir advisory lock ──────────────────────────────────
            _logger.LogInformation("Intentando adquirir advisory lock (ID={LockId})...", _schemaLock.LockId);

            var lockAcquired = await _schemaLock.AcquireAsync(ct);
            if (!lockAcquired)
            {
                return new UpgradeResult(false, targetVersion, 0, null, sw.Elapsed,
                    "No se pudo adquirir el lock. Otro proceso de upgrade está en curso.",
                    appliedVersions);
            }

            _logger.LogInformation("Advisory lock adquirido. Iniciando upgrade.");

            try
            {
                // ── Paso 2: Pre-checks ──────────────────────────────────────────
                var preCheckResult = await RunPreChecksAsync(migrationsDirectory, ct);
                if (!preCheckResult.Success)
                {
                    return new UpgradeResult(false, targetVersion, 0, null, sw.Elapsed,
                        preCheckResult.ErrorMessage!, appliedVersions);
                }

                if (dryRun)
                {
                    _logger.LogInformation("DRY RUN: validación completada sin errores. No se aplicaron cambios.");
                    return new UpgradeResult(true, targetVersion, 0, null, sw.Elapsed,
                        null, appliedVersions);
                }

                // ── Paso 3: Backup ──────────────────────────────────────────────
                if (!skipBackup)
                {
                    _logger.LogInformation("Creando backup pre-upgrade...");
                    try
                    {
                        backupPath = await _backupHelper.CreateBackupAsync(
                            description: $"pre-upgrade-{targetVersion ?? "latest"}",
                            ct: ct);

                        _logger.LogInformation("Backup creado: {BackupPath}", backupPath);

                        // Rotación de backups antiguos (async fire-and-forget después del upgrade)
                        _ = Task.Run(() => _backupHelper.CleanupOldBackupsAsync(CancellationToken.None), CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Falló la creación del backup: {ex.Message}. " +
                            "El upgrade se cancela para evitar pérdida de datos.", ex);
                    }
                }
                else
                {
                    _logger.LogWarning("BACKUP OMITIDO (skipBackup=true). No hay punto de restauración.");
                }

                // ── Paso 4: Aplicar migraciones pendientes ──────────────────────
                var currentVersion = await GetCurrentVersionAsync(ct);

                _logger.LogInformation("Versión actual: {CurrentVersion}. Target: {TargetVersion}",
                    currentVersion, targetVersion ?? "latest");

                var pendingMigrations = await DiscoverPendingMigrationsAsync(
                    migrationsDirectory, currentVersion, targetVersion, ct);

                _logger.LogInformation("Migraciones pendientes: {Count}", pendingMigrations.Count);

                foreach (var migration in pendingMigrations)
                {
                    ct.ThrowIfCancellationRequested();

                    _logger.LogInformation("Aplicando migración: {Version} ({Description})",
                        migration.Version, migration.Description);

                    try
                    {
                        await ApplyMigrationAsync(migration, appliedBy, ct);
                        appliedVersions.Add(migration.Version);
                        migrationsApplied++;

                        _logger.LogInformation("Migración aplicada: {Version} ✓", migration.Version);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Falló la migración {Version}. Iniciando rollback...",
                            migration.Version);

                        // Rollback: restaurar backup
                        await RollbackAsync(backupPath, appliedVersions, ct);

                        return new UpgradeResult(false, targetVersion, migrationsApplied,
                            backupPath, sw.Elapsed,
                            $"Falló la migración {migration.Version}: {ex.Message}. Se restauró el backup.",
                            appliedVersions);
                    }
                }

                // ── Paso 5: Rotación de backups post-upgrade exitoso ────────────
                if (!skipBackup)
                {
                    var cleaned = await _backupHelper.CleanupOldBackupsAsync(ct);
                    if (cleaned > 0)
                        _logger.LogInformation("Rotación: {Count} backups antiguos eliminados.", cleaned);
                }

                _logger.LogInformation(
                    "Upgrade completado exitosamente. {Count} migraciones aplicadas en {Duration}.",
                    migrationsApplied, sw.Elapsed);

                return new UpgradeResult(true, targetVersion, migrationsApplied,
                    backupPath, sw.Elapsed, null, appliedVersions);
            }
            finally
            {
                // ── Paso 6: Liberar advisory lock (SIEMPRE, incluso en error) ──
                if (_schemaLock.IsAcquired)
                {
                    _logger.LogInformation("Liberando advisory lock...");
                    await _schemaLock.ReleaseAsync(ct);
                    _logger.LogInformation("Advisory lock liberado.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Upgrade cancelado por el usuario.");
            return new UpgradeResult(false, targetVersion, migrationsApplied,
                backupPath, sw.Elapsed, "Upgrade cancelado por el usuario.", appliedVersions);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error inesperado durante el upgrade.");
            return new UpgradeResult(false, targetVersion, migrationsApplied,
                backupPath, sw.Elapsed, $"Error inesperado: {ex.Message}", appliedVersions);
        }
    }

    // ── Pre-checks ──────────────────────────────────────────────────────────────

    private record PreCheckResult(bool Success, string? ErrorMessage);

    private async Task<PreCheckResult> RunPreChecksAsync(string migrationsDirectory, CancellationToken ct)
    {
        // 1. Validar política aditiva
        _logger.LogInformation("Pre-check: validando política aditiva...");
        var violations = await _migrationPolicy.ValidateDirectoryAsync(migrationsDirectory);
        if (violations.Count > 0)
        {
            var violationList = string.Join("\n  - ", violations.Select(v =>
                $"{Path.GetFileName(v.FilePath)}:{v.LineNumber} — {v.Operation}: {v.Reason}"));

            return new PreCheckResult(false,
                $"Violaciones de política aditiva detectadas ({violations.Count}):\n  - {violationList}");
        }
        _logger.LogInformation("Política aditiva: OK ✓");

        // 2. Verificar espacio en disco
        _logger.LogInformation("Pre-check: verificando espacio en disco...");
        var diskSpace = await _metadata.GetDiskSpace(ct);
        if (diskSpace < MinimumDiskSpaceBytes)
        {
            var availableMb = diskSpace / (1024.0 * 1024.0);
            var requiredMb = MinimumDiskSpaceBytes / (1024.0 * 1024.0);
            return new PreCheckResult(false,
                $"Espacio en disco insuficiente: {availableMb:F1} MB disponibles, " +
                $"mínimo requerido: {requiredMb:F0} MB.");
        }
        _logger.LogInformation("Espacio en disco: {Space:F1} MB ✓", diskSpace / (1024.0 * 1024.0));

        // 3. Verificar versión de PostgreSQL
        _logger.LogInformation("Pre-check: verificando versión de PostgreSQL...");
        var pgVersion = await _metadata.GetPgVersion(ct);

        if (!IsVersionAtLeast(pgVersion, MinimumPgVersion))
        {
            return new PreCheckResult(false,
                $"Versión de PostgreSQL no soportada: {pgVersion}. Mínimo requerido: {MinimumPgVersion}.");
        }
        _logger.LogInformation("Versión PostgreSQL: {Version} ✓", pgVersion);

        return new PreCheckResult(true, null);
    }

    // ── Version helpers ─────────────────────────────────────────────────────────

    private async Task<string> GetCurrentVersionAsync(CancellationToken ct)
    {
        var latest = await _versionRepo.GetLatestVersion(ct);
        return latest?.Version ?? "0.0.0";
    }

    private Task<IReadOnlyList<MigrationDescriptor>> DiscoverPendingMigrationsAsync(
        string migrationsDirectory,
        string currentVersion,
        string? targetVersion,
        CancellationToken ct)
    {
        // Escanear archivos de migración en el directorio
        var migrationFiles = Directory.GetFiles(migrationsDirectory, "*.cs")
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var descriptors = new List<MigrationDescriptor>();

        foreach (var file in migrationFiles)
        {
            var descriptor = ParseMigrationFile(file);
            if (descriptor != null)
            {
                // Solo incluir migraciones posteriores a currentVersion
                if (CompareSemver(descriptor.Version, currentVersion) > 0)
                {
                    // Si hay targetVersion, solo incluir hasta esa versión
                    if (targetVersion != null && CompareSemver(descriptor.Version, targetVersion) > 0)
                        continue;

                    descriptors.Add(descriptor);
                }
            }
        }

        // Ordenar por versión semántica
        descriptors.Sort((a, b) => CompareSemver(a.Version, b.Version));

        return Task.FromResult<IReadOnlyList<MigrationDescriptor>>(descriptors);
    }

    /// <summary>
    /// Descriptor de una migración extraído del archivo .cs.
    /// </summary>
    private record MigrationDescriptor(
        string FilePath,
        string Version,
        string Description,
        string ModuleName
    );

    private static MigrationDescriptor? ParseMigrationFile(string filePath)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            // Formato esperado: YYYYMMDDHHmmss_Description
            // La versión se deriva del timestamp o metadata

            // Extraer versión del nombre: usamos el timestamp como versión semántica base
            // Ej: 20260611123000_AddSchemaVersionTable → versión derivada
            var parts = fileName.Split('_', 2);
            if (parts.Length < 2)
                return null;

            var description = parts[1].Replace("_", " ");

            // La versión semántica se calcula desde el timestamp ordenable
            // No tenemos semver explícito en el nombre del archivo, así que derivamos uno
            // basado en la posición ordinal o en metadatos del archivo.
            var version = DeriveSemverFromTimestamp(parts[0]);

            // Determinar módulo desde el namespace o descripción
            var moduleName = DetermineModule(description);

            return new MigrationDescriptor(filePath, version, description, moduleName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deriva una versión semántica desde un timestamp EF Core.
    /// La versión es única y monotónicamente creciente.
    /// </summary>
    private static string DeriveSemverFromTimestamp(string timestamp)
    {
        // timestamp: YYYYMMDDHHmmss (14 dígitos)
        // Convertir a semver: YYYY.MMDD.HHmm → ej: 2026.0611.1230
        if (timestamp.Length >= 12)
        {
            var major = timestamp[..4];
            var minor = timestamp[4..8];
            var patch = timestamp[8..12];
            return $"{major}.{minor}.{patch}";
        }

        return $"0.0.{timestamp}";
    }

    private static string DetermineModule(string description)
    {
        // Heurística simple basada en palabras clave en la descripción
        var desc = description.ToLowerInvariant();

        if (desc.Contains("plugin")) return "plugins";
        if (desc.Contains("schema") || desc.Contains("version")) return "core";
        if (desc.Contains("member") || desc.Contains("user") || desc.Contains("auth")) return "members";
        if (desc.Contains("payment") || desc.Contains("sale") || desc.Contains("product")) return "sales";
        if (desc.Contains("routine") || desc.Contains("exercise") || desc.Contains("workout")) return "routines";
        if (desc.Contains("measurement") || desc.Contains("body")) return "anthropometry";
        if (desc.Contains("access") || desc.Contains("check")) return "access";

        return "core";
    }

    /// <summary>
    /// Compara dos versiones semánticas (major.minor.patch).
    /// Retorna: >0 si v1 > v2, 0 si iguales, <0 si v1 < v2.
    /// </summary>
    public static int CompareSemver(string v1, string v2)
    {
        var parts1 = v1.Split('.');
        var parts2 = v2.Split('.');

        for (int i = 0; i < Math.Max(parts1.Length, parts2.Length); i++)
        {
            var n1 = i < parts1.Length && long.TryParse(parts1[i], out var p1) ? p1 : 0;
            var n2 = i < parts2.Length && long.TryParse(parts2[i], out var p2) ? p2 : 0;

            if (n1 != n2)
                return n1.CompareTo(n2);
        }

        return 0;
    }

    private static bool IsVersionAtLeast(string actual, string required)
    {
        return CompareSemver(actual, required) >= 0;
    }

    // ── Migration application ───────────────────────────────────────────────────

    /// <summary>
    /// Applies a single migration by delegating to EF Core's IMigrator
    /// and then records the version in schema_version for tracking (HU-017).
    ///
    /// The migration name is derived from the file path: the filename
    /// without extension is the EF Core migration ID (e.g.,
    /// "20260611123000_AddSchemaVersionTable").
    ///
    /// Execution order:
    ///   1. Execute the migration via EF Core (IMigrationExecutor)
    ///   2. Record the version in schema_version (ISchemaVersionRepository)
    ///
    /// If step 1 fails, step 2 is never reached — the caller handles rollback.
    /// </summary>
    private async Task ApplyMigrationAsync(
        MigrationDescriptor descriptor,
        string appliedBy,
        CancellationToken ct)
    {
        var migrationName = Path.GetFileNameWithoutExtension(descriptor.FilePath);

        // Step 1: Execute the actual migration via EF Core
        _logger.LogDebug("Ejecutando migración EF Core: {MigrationName}", migrationName);
        await _migrationExecutor.ExecuteAsync(migrationName, ct);
        _logger.LogDebug("Migración EF Core completada: {MigrationName}", migrationName);

        // Step 2: Record the version in schema_version for our own tracking
        var hash = ComputeMigrationHash(descriptor.FilePath);

        var entry = Domain.Entities.SchemaVersion.Create(
            version: descriptor.Version,
            moduleName: descriptor.ModuleName,
            appliedBy: appliedBy,
            description: descriptor.Description,
            migrationHash: hash,
            rollbackSql: "-- Rollback manual requerido. Restaurar desde backup pre-upgrade.");

        await _versionRepo.RecordApplied(entry, ct);
    }

    private static string ComputeMigrationHash(string filePath)
    {
        if (!File.Exists(filePath))
            return "unknown";

        var bytes = File.ReadAllBytes(filePath);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // ── Rollback ────────────────────────────────────────────────────────────────

    private async Task RollbackAsync(
        string? backupPath,
        IReadOnlyList<string> appliedVersions,
        CancellationToken ct)
    {
        _logger.LogWarning("Iniciando rollback. Migraciones aplicadas antes del fallo: {Count}",
            appliedVersions.Count);

        if (backupPath != null && File.Exists(backupPath))
        {
            try
            {
                _logger.LogInformation("Restaurando backup: {BackupPath}", backupPath);
                await _backupHelper.RestoreBackupAsync(backupPath, ct);
                _logger.LogInformation("Backup restaurado exitosamente. La base de datos volvió al estado pre-upgrade.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "¡RESTAURACIÓN DE BACKUP FALLÓ! La base de datos puede estar en estado inconsistente. " +
                    "Backup: {BackupPath}. Restauración manual requerida.", backupPath);
                throw;
            }
        }
        else
        {
            _logger.LogError(
                "No hay backup disponible para restaurar. " +
                "La base de datos puede estar en estado inconsistente. " +
                "Migraciones aplicadas: {Versions}",
                string.Join(", ", appliedVersions));
        }
    }
}
