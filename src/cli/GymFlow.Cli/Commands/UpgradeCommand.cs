// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using System.CommandLine;
using GymFlow.Cli.Services;

namespace GymFlow.Cli.Commands;

/// <summary>
/// gymflow upgrade — Execute a schema upgrade (HU-017).
///
/// Communicates with the GymFlow WebAPI SchemaController to trigger
/// the upgrade process. Supports --target, --skip-backup, --dry-run,
/// and --verbose options.
///
/// The CLI delegates the full upgrade workflow to the backend:
/// advisory lock → pre-checks → backup → apply migrations → record → release.
/// </summary>
public static class UpgradeCommand
{
    public static Command Build()
    {
        var command = new Command("upgrade", "Upgrade GymFlow Lite to a new version");

        var targetOption = new Option<string?>("--target")
        {
            Description = "Target version (e.g., 1.1.0). If omitted, applies all pending migrations.",
            IsRequired = false
        };
        var skipBackupOption = new Option<bool>("--skip-backup")
        {
            Description = "Skip backup before upgrade (NOT recommended in production)"
        };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what would be done without applying changes"
        };
        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Enable verbose output"
        };

        command.AddOption(targetOption);
        command.AddOption(skipBackupOption);
        command.AddOption(dryRunOption);
        command.AddOption(verboseOption);

        command.SetHandler(async (string? target, bool skipBackup, bool dryRun, bool verbose) =>
        {
            Console.WriteLine("GymFlow Lite — Schema Upgrade");
            Console.WriteLine("=============================");
            Console.WriteLine();

            if (dryRun)
                Console.WriteLine("🔍 DRY RUN MODE — no changes will be applied");
            if (skipBackup)
                Console.WriteLine("⚠  BACKUP SKIPPED — no rollback point available");
            Console.WriteLine();

            try
            {
                var client = new SchemaApiClient();

                Console.WriteLine($"Target version: {target ?? "latest (all pending)"}");
                Console.WriteLine($"Connecting to API: {Environment.GetEnvironmentVariable("GYMFLOW_API_URL") ?? "http://localhost:5000"}");
                Console.WriteLine();

                if (verbose)
                {
                    Console.WriteLine($"  --skip-backup: {skipBackup}");
                    Console.WriteLine($"  --dry-run:     {dryRun}");
                    Console.WriteLine($"  --verbose:     {verbose}");
                    Console.WriteLine();
                }

                Console.Write("Running upgrade...");

                var result = await client.UpgradeAsync(
                    targetVersion: target,
                    skipBackup: skipBackup,
                    dryRun: dryRun,
                    verbose: verbose);

                Console.WriteLine();

                if (result.Success)
                {
                    Console.WriteLine();
                    Console.WriteLine("✓ Upgrade completed successfully!");
                    Console.WriteLine($"  Migrations applied: {result.MigrationsApplied}");
                    Console.WriteLine($"  Duration:           {result.Duration}");

                    if (!string.IsNullOrWhiteSpace(result.BackupPath))
                        Console.WriteLine($"  Backup:             {result.BackupPath}");

                    if (result.AppliedVersions.Count > 0)
                    {
                        Console.WriteLine("  Versions applied:");
                        foreach (var version in result.AppliedVersions)
                            Console.WriteLine($"    → {version}");
                    }

                    if (dryRun && result.MigrationsApplied == 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("🔍 Dry run: no violations found. Ready for real upgrade.");
                    }
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine($"✗ Upgrade failed: {result.ErrorMessage}");

                    if (result.MigrationsApplied > 0)
                    {
                        Console.WriteLine($"  Migrations applied before failure: {result.MigrationsApplied}");
                        Console.WriteLine("  The database was rolled back to its pre-upgrade state.");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine();
                Console.WriteLine($"✗ Cannot connect to GymFlow API: {ex.Message}");
                Console.WriteLine("  Make sure the WebAPI is running:");
                Console.WriteLine("    cd src/backend/WebAPI && dotnet run");
                Console.WriteLine("  Or set GYMFLOW_API_URL to the correct endpoint.");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine();
                Console.WriteLine("⚠ Upgrade timed out or was cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"✗ Unexpected error: {ex.Message}");
                if (verbose)
                    Console.WriteLine($"  {ex}");
            }
        }, targetOption, skipBackupOption, dryRunOption, verboseOption);

        return command;
    }
}
