// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using System.Diagnostics;

namespace GymFlow.Cli.Commands.Helpers;

/// <summary>
/// CLI-side wrapper for PostgreSQL backup and restore operations (HU-017).
///
/// Invokes pg_dump and pg_restore via process spawn. Uses connection parameters
/// from environment variables:
///   - GYMFLOW_PG_HOST     (default: localhost)
///   - GYMFLOW_PG_PORT     (default: 5432)
///   - GYMFLOW_PG_USER     (default: gymflow)
///   - GYMFLOW_PG_PASSWORD (default: gymflow)
///   - GYMFLOW_PG_DATABASE (default: gymflow_dev)
///
/// This is CLI-specific — separate from the Infrastructure.Services.BackupHelper
/// which uses Npgsql and runs within the backend process.
///
/// The CLI helper is designed for standalone use (docker exec or direct pg_dump)
/// when the backend is not running.
/// </summary>
public class CliBackupHelper
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _user;
    private readonly string _password;
    private readonly string _database;
    private readonly string _backupDirectory;

    public const int MaxBackups = 5;

    public CliBackupHelper(string? backupDirectory = null)
    {
        _host = Environment.GetEnvironmentVariable("GYMFLOW_PG_HOST") ?? "localhost";
        _port = int.TryParse(Environment.GetEnvironmentVariable("GYMFLOW_PG_PORT"), out var p) ? p : 5432;
        _user = Environment.GetEnvironmentVariable("GYMFLOW_PG_USER") ?? "gymflow";
        _password = Environment.GetEnvironmentVariable("GYMFLOW_PG_PASSWORD") ?? "gymflow";
        _database = Environment.GetEnvironmentVariable("GYMFLOW_PG_DATABASE") ?? "gymflow_dev";
        _backupDirectory = backupDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "backups");
    }

    /// <summary>
    /// Gets the directory where backups are stored.
    /// </summary>
    public string BackupDirectory => _backupDirectory;

    /// <summary>
    /// Creates a database backup using pg_dump.
    /// </summary>
    /// <param name="description">Optional description for the backup filename.</param>
    /// <param name="verbose">If true, prints progress to console.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Path to the created backup file, or null if failed.</returns>
    public async Task<string?> CreateBackupAsync(
        string? description = null,
        bool verbose = false,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(_backupDirectory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var tag = description != null ? $"_{SanitizeFileName(description)}" : "";
        var outputPath = Path.Combine(_backupDirectory, $"gymflow_backup_{_database}{tag}_{timestamp}.sql");

        if (verbose)
            Console.WriteLine($"  Creating backup: {outputPath}");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = $"-h {_host} -p {_port} -U {_user} -d {_database} -F p --no-owner --no-acl",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.Environment["PGPASSWORD"] = _password;

            using var process = new Process { StartInfo = psi };
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var stderr = await errorTask;

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"✗ pg_dump failed (exit {process.ExitCode}): {stderr.Trim()}");
                return null;
            }

            var stdout = await outputTask;
            await File.WriteAllTextAsync(outputPath, stdout, ct);

            if (verbose)
            {
                var fileInfo = new FileInfo(outputPath);
                Console.WriteLine($"✓ Backup created ({FormatBytes(fileInfo.Length)}): {outputPath}");
            }

            // Rotate old backups
            CleanupOldBackups();

            return outputPath;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Backup cancelled.");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Backup failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Restores a database from a backup file using psql.
    /// </summary>
    /// <param name="backupPath">Path to the backup .sql file.</param>
    /// <param name="verbose">If true, prints progress to console.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if restoration succeeded.</returns>
    public async Task<bool> RestoreBackupAsync(
        string backupPath,
        bool verbose = false,
        CancellationToken ct = default)
    {
        if (!File.Exists(backupPath))
        {
            Console.WriteLine($"✗ Backup file not found: {backupPath}");
            return false;
        }

        if (verbose)
            Console.WriteLine($"  Restoring from: {backupPath}");

        try
        {
            var backupContent = await File.ReadAllTextAsync(backupPath, ct);

            var psi = new ProcessStartInfo
            {
                FileName = "psql",
                Arguments = $"-h {_host} -p {_port} -U {_user} -d {_database}",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.Environment["PGPASSWORD"] = _password;

            using var process = new Process { StartInfo = psi };
            process.Start();

            await process.StandardInput.WriteAsync(backupContent);
            process.StandardInput.Close();

            var errorTask = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            var stderr = await errorTask;

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"✗ Restore failed (exit {process.ExitCode}): {stderr.Trim()}");
                return false;
            }

            if (verbose)
                Console.WriteLine("✓ Restore completed.");

            return true;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Restore cancelled.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Restore failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Lists existing backups ordered by most recent first.
    /// </summary>
    public IReadOnlyList<string> ListBackups()
    {
        if (!Directory.Exists(_backupDirectory))
            return Array.Empty<string>();

        return Directory.GetFiles(_backupDirectory, "gymflow_backup_*.sql")
            .OrderByDescending(f => f)
            .ToList();
    }

    /// <summary>
    /// Removes backups exceeding the maximum retention count.
    /// </summary>
    /// <returns>Number of backups deleted.</returns>
    public int CleanupOldBackups()
    {
        var deleted = 0;

        if (!Directory.Exists(_backupDirectory))
            return deleted;

        var backups = Directory.GetFiles(_backupDirectory, "gymflow_backup_*.sql")
            .OrderByDescending(f => f)
            .ToList();

        for (int i = MaxBackups; i < backups.Count; i++)
        {
            try
            {
                File.Delete(backups[i]);
                deleted++;
            }
            catch
            {
                // Ignore: file locked or permissions issue
            }
        }

        return deleted;
    }

    // ── Private helpers ──────────────────────────────────────────────────────────

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return System.Text.RegularExpressions.Regex.Replace(sanitized, @"_+", "_").Trim('_');
    }

    private static string FormatBytes(long bytes)
    {
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
