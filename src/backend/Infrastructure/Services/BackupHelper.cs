// GymFlow Lite - Schema Versioning (HU-017)
// Copyright (C) 2026 GymFlow contributors
// License: AGPL v3 (see LICENSE)

using System.Diagnostics;
using System.Text.RegularExpressions;
using Npgsql;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Helper para backups pre-upgrade y restauración de base de datos (HU-017).
/// 
/// Envuelve pg_dump y pg_restore para crear backups antes de cada upgrade de esquema
/// y restaurarlos en caso de rollback.
/// 
/// Estrategia de rotación: mantiene los últimos 5 backups, eliminando los más antiguos.
/// Los backups se almacenan en formato custom (-Fc) para permitir restauración selectiva.
/// </summary>
public class BackupHelper
{
    private readonly string _connectionString;
    private readonly string _backupDirectory;
    private readonly string _pgDumpPath;
    private readonly string _pgRestorePath;
    private const int MaxBackups = 5;

    // Parsed connection parameters
    private readonly string _host;
    private readonly int _port;
    private readonly string _database;
    private readonly string _username;
    private readonly string _password;

    /// <param name="connectionString">Cadena de conexión a PostgreSQL.</param>
    /// <param name="backupDirectory">Directorio donde almacenar los backups. Default: ./backups/</param>
    /// <param name="pgDumpPath">Ruta al binario pg_dump. Default: busca en PATH.</param>
    /// <param name="pgRestorePath">Ruta al binario pg_restore. Default: busca en PATH.</param>
    public BackupHelper(
        string connectionString,
        string? backupDirectory = null,
        string? pgDumpPath = null,
        string? pgRestorePath = null)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _backupDirectory = backupDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "backups");
        _pgDumpPath = pgDumpPath ?? "pg_dump";
        _pgRestorePath = pgRestorePath ?? "pg_restore";

        // Parsear connection string para extraer parámetros
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        _host = builder.Host ?? "localhost";
        _port = builder.Port;
        _database = builder.Database ?? "gymflow_dev";
        _username = builder.Username ?? "";
        _password = builder.Password ?? "";

        Directory.CreateDirectory(_backupDirectory);
    }

    /// <summary>
    /// Crea un backup completo de la base de datos usando pg_dump.
    /// 
    /// Formato: {backupDirectory}/gymflow_backup_{database}_{timestamp}.dump
    /// El formato custom (-Fc) permite restauración paralela y selectiva.
    /// </summary>
    /// <param name="description">Descripción opcional del backup (ej: "pre-upgrade-v2.0.0").</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Ruta completa al archivo de backup creado.</returns>
    public async Task<string> CreateBackupAsync(string? description = null, CancellationToken ct = default)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var tag = description != null ? $"_{SanitizeFileName(description)}" : "";
        var fileName = $"gymflow_backup_{_database}{tag}_{timestamp}.dump";
        var backupPath = Path.Combine(_backupDirectory, fileName);

        // Construir argumentos de pg_dump
        var args = BuildPgDumpArgs(backupPath);

        var processInfo = new ProcessStartInfo
        {
            FileName = _pgDumpPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // PGPASSWORD por entorno para autenticación sin prompt
        processInfo.Environment["PGPASSWORD"] = _password;

        using var process = new Process { StartInfo = processInfo };

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(ct);
        var errorTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stderr = await errorTask;

        if (process.ExitCode != 0)
        {
            var errorMsg = string.IsNullOrWhiteSpace(stderr) ? $"pg_dump exit code: {process.ExitCode}" : stderr;
            throw new InvalidOperationException($"pg_dump falló: {errorMsg}");
        }

        // Verificar que el archivo se creó
        if (!File.Exists(backupPath))
            throw new InvalidOperationException($"pg_dump no generó el archivo de backup: {backupPath}");

        return backupPath;
    }

    /// <summary>
    /// Restaura un backup usando pg_restore.
    /// 
    /// ADVERTENCIA: Esto SOBREESCRIBE la base de datos actual.
    /// Usar solo durante rollback de upgrade fallido.
    /// 
    /// Requiere que la base de datos esté vacía o se use --clean.
    /// </summary>
    /// <param name="backupPath">Ruta al archivo .dump a restaurar.</param>
    /// <param name="ct">Token de cancelación.</param>
    public async Task RestoreBackupAsync(string backupPath, CancellationToken ct = default)
    {
        if (!File.Exists(backupPath))
            throw new FileNotFoundException($"Archivo de backup no encontrado: {backupPath}");

        var args = BuildPgRestoreArgs(backupPath);

        var processInfo = new ProcessStartInfo
        {
            FileName = _pgRestorePath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        processInfo.Environment["PGPASSWORD"] = _password;

        using var process = new Process { StartInfo = processInfo };

        process.Start();

        var errorTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stderr = await errorTask;

        if (process.ExitCode != 0)
        {
            // pg_restore puede reportar errores no fatales en stderr aunque funcione
            // Si el exit code != 0, es un error real
            var errorMsg = string.IsNullOrWhiteSpace(stderr) ? $"pg_restore exit code: {process.ExitCode}" : stderr;
            throw new InvalidOperationException($"pg_restore falló: {errorMsg}");
        }
    }

    /// <summary>
    /// Aplica rotación de backups: mantiene solo los últimos N backups.
    /// Elimina los más antiguos cuando se excede el límite.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Número de backups eliminados.</returns>
    public Task<int> CleanupOldBackupsAsync(CancellationToken ct = default)
    {
        var deleted = 0;

        if (!Directory.Exists(_backupDirectory))
            return Task.FromResult(deleted);

        var backups = Directory.GetFiles(_backupDirectory, "gymflow_backup_*.dump")
            .OrderByDescending(f => f) // Más recientes primero
            .ToList();

        // Mantener los primeros MaxBackups, eliminar el resto
        for (int i = MaxBackups; i < backups.Count; i++)
        {
            try
            {
                File.Delete(backups[i]);
                deleted++;
            }
            catch (Exception)
            {
                // Si un archivo no se puede eliminar (permisos, bloqueado), lo ignoramos
                // y seguimos con el siguiente. No queremos que la rotación falle el upgrade.
            }
        }

        return Task.FromResult(deleted);
    }

    /// <summary>
    /// Obtiene la ruta donde se almacenan los backups.
    /// </summary>
    public string GetBackupPath() => _backupDirectory;

    /// <summary>
    /// Lista los backups existentes ordenados por fecha (más reciente primero).
    /// </summary>
    public IReadOnlyList<string> ListBackups()
    {
        if (!Directory.Exists(_backupDirectory))
            return Array.Empty<string>();

        return Directory.GetFiles(_backupDirectory, "gymflow_backup_*.dump")
            .OrderByDescending(f => f)
            .ToList();
    }

    // ── Private helpers ──────────────────────────────────────────────────────────

    private string BuildPgDumpArgs(string outputPath)
    {
        // pg_dump -h host -p port -U user -d database -Fc -f output
        var args = $"-h {EscapeArg(_host)} -p {_port} -U {EscapeArg(_username)} -d {EscapeArg(_database)} -Fc -f {EscapeArg(outputPath)} --no-owner --no-acl";

        // Excluir tablas de sistema de EF Core para backup más limpio
        args += " --exclude-table=\\\"__EFMigrationsHistory\\\"";

        return args;
    }

    private string BuildPgRestoreArgs(string inputPath)
    {
        // pg_restore -h host -p port -U user -d database --clean --if-exists --no-owner --no-acl input
        return $"-h {EscapeArg(_host)} -p {_port} -U {EscapeArg(_username)} -d {EscapeArg(_database)} --clean --if-exists --no-owner --no-acl {EscapeArg(inputPath)}";
    }

    private static string EscapeArg(string arg)
    {
        // Escapar argumentos para shell: si contiene espacios, envolver en comillas
        if (string.IsNullOrEmpty(arg))
            return "\"\"";
        if (arg.Contains(' ') || arg.Contains('\t'))
            return $"\"{arg}\"";
        return arg;
    }

    private static string SanitizeFileName(string name)
    {
        // Reemplazar caracteres no válidos para nombres de archivo
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return Regex.Replace(sanitized, @"_+", "_").Trim('_');
    }
}
