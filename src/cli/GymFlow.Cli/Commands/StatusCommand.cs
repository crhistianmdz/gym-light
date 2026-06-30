// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using System.CommandLine;
using GymFlow.Cli.Services;

namespace GymFlow.Cli.Commands;

/// <summary>
/// gymflow status — Show GymFlow schema and system status (HU-017).
///
/// Queries the WebAPI for real schema information:
///   - Current schema version and last migration
///   - Pending migrations count
///   - Disk space available
///   - PostgreSQL version
///   - Advisory lock status
///   - Module-level version breakdown
///
/// Falls back to basic info if the API is unreachable.
/// </summary>
public static class StatusCommand
{
    public static Command Build()
    {
        var command = new Command("status", "Show GymFlow status information");

        var verboseOption = new Option<bool>("--verbose") { Description = "Show detailed output" };
        command.AddOption(verboseOption);

        command.SetHandler(async (bool verbose) =>
        {
            Console.WriteLine("GymFlow Lite Status");
            Console.WriteLine("==================");

            try
            {
                var client = new SchemaApiClient();
                var status = await client.GetStatusAsync();

                if (status != null)
                {
                    // ── Real status from API ──────────────────────────────
                    Console.WriteLine($"Version:          {status.CurrentVersion}");
                    Console.WriteLine($"Last Migration:   {status.LastMigrationAt?.ToString("yyyy-MM-dd HH:mm") ?? "none"}");
                    if (!string.IsNullOrWhiteSpace(status.LastMigrationDescription))
                        Console.WriteLine($"  Description:    {status.LastMigrationDescription}");
                    Console.WriteLine($"Pending:          {status.PendingMigrationsCount} migration(s)");
                    Console.WriteLine($"Disk Space:       {status.DiskSpaceFormatted}");
                    Console.WriteLine($"PostgreSQL:       {status.PgVersion}");
                    Console.WriteLine($"Schema Lock:      {(status.IsLockHeld ? "🔒 HELD (upgrade in progress)" : "✓ Free")}");

                    if (verbose)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Module Versions:");
                        Console.WriteLine("----------------");
                        foreach (var module in status.ModuleVersions)
                        {
                            var appliedStr = module.AppliedAt != DateTime.MinValue
                                ? module.AppliedAt.ToString("yyyy-MM-dd HH:mm")
                                : "not registered";
                            Console.WriteLine($"  {module.ModuleName,-20} v{module.Version,-12} ({appliedStr})");
                        }

                        Console.WriteLine();
                        Console.WriteLine("Environment:");
                        Console.WriteLine("  API URL:       " +
                            (Environment.GetEnvironmentVariable("GYMFLOW_API_URL") ?? "http://localhost:5000 (default)"));
                        Console.WriteLine("  DB Host:       " +
                            (Environment.GetEnvironmentVariable("GYMFLOW_PG_HOST") ?? "localhost (default)"));
                        Console.WriteLine("  DB Name:       " +
                            (Environment.GetEnvironmentVariable("GYMFLOW_PG_DATABASE") ?? "gymflow_dev (default)"));

                        // Show backup info
                        var backupHelper = new Helpers.CliBackupHelper();
                        var backups = backupHelper.ListBackups();
                        Console.WriteLine($"  Backups:       {backups.Count} found in {backupHelper.BackupDirectory}");
                        if (backups.Count > 0)
                        {
                            foreach (var backup in backups.Take(3))
                            {
                                var info = new FileInfo(backup);
                                Console.WriteLine($"    - {info.Name} ({FormatBytes(info.Length)})");
                            }
                            if (backups.Count > 3)
                                Console.WriteLine($"    ... and {backups.Count - 3} more");
                        }
                    }
                }
                else
                {
                    ShowFallbackStatus(verbose);
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("⚠  Cannot reach GymFlow API — showing local info only");
                Console.WriteLine();
                ShowFallbackStatus(verbose);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠  Error querying API: {ex.Message}");
                Console.WriteLine();
                ShowFallbackStatus(verbose);
            }
        }, verboseOption);

        return command;
    }

    /// <summary>
    /// Fallback status display when the API is unreachable.
    /// Shows what we can determine locally (CLI version, env vars, local backups).
    /// </summary>
    private static void ShowFallbackStatus(bool verbose)
    {
        Console.WriteLine("CLI Version:      1.0.0");
        Console.WriteLine("Schema Version:   (API required for real value)");

        if (verbose)
        {
            Console.WriteLine();
            Console.WriteLine("Local Backups:");
            try
            {
                var backupHelper = new Helpers.CliBackupHelper();
                var backups = backupHelper.ListBackups();
                Console.WriteLine($"  Directory:     {backupHelper.BackupDirectory}");
                Console.WriteLine($"  Count:         {backups.Count}");
                foreach (var backup in backups.Take(5))
                {
                    var info = new FileInfo(backup);
                    Console.WriteLine($"    - {info.Name} ({FormatBytes(info.Length)})");
                }
                if (backups.Count > 5)
                    Console.WriteLine($"    ... and {backups.Count - 5} more");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error listing backups: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Run the WebAPI to get full status:");
        Console.WriteLine("  cd src/backend/WebAPI && dotnet run");
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "N/A";

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
