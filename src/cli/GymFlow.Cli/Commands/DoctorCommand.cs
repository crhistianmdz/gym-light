// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using System.CommandLine;
using GymFlow.Cli.Services;

namespace GymFlow.Cli.Commands;

/// <summary>
/// gymflow doctor — Run diagnostic checks (HU-017).
///
/// Checks:
///   1. Docker availability
///   2. PostgreSQL container status
///   3. Redis container status
///   4. Schema consistency validation (HU-017) via WebAPI
///      - schema_version ↔ __EFMigrationsHistory consistency
///      - Orphaned migrations detection
///      - Additive migration policy violations
///
/// The --fix flag attempts to auto-resolve issues where possible
/// (future: auto-register orphaned EF migrations into schema_version).
/// </summary>
public static class DoctorCommand
{
    public static Command Build()
    {
        var command = new Command("doctor", "Run diagnostic checks");

        var fixOption = new Option<bool>("--fix") { Description = "Attempt to fix issues automatically" };
        var verboseOption = new Option<bool>("--verbose") { Description = "Enable verbose output" };

        command.AddOption(fixOption);
        command.AddOption(verboseOption);

        command.SetHandler(async (bool fix, bool verbose) =>
        {
            var issues = new List<string>();
            var warnings = new List<string>();

            Console.WriteLine("GymFlow Lite — Diagnostic Check");
            Console.WriteLine("================================");
            Console.WriteLine();

            // ── Check 1: Docker ────────────────────────────────────────
            Console.Write("  Docker          ");
            if (CheckDocker())
                Console.WriteLine("✓ OK");
            else
            {
                Console.WriteLine("✗ NOT AVAILABLE");
                issues.Add("Docker not available — required for PostgreSQL and Redis containers.");
            }

            // ── Check 2: PostgreSQL ────────────────────────────────────
            Console.Write("  PostgreSQL      ");
            if (CheckPostgreSQL())
                Console.WriteLine("✓ OK");
            else
            {
                Console.WriteLine("✗ NOT AVAILABLE");
                issues.Add("PostgreSQL container not running. Start with: docker compose up -d postgres");
            }

            // ── Check 3: Redis ─────────────────────────────────────────
            Console.Write("  Redis           ");
            if (CheckRedis())
                Console.WriteLine("✓ OK");
            else
            {
                Console.WriteLine("✗ NOT AVAILABLE");
                warnings.Add("Redis container not running. Start with: docker compose up -d redis");
            }

            Console.WriteLine();

            // ── Check 4: Schema validation (HU-017) ────────────────────
            Console.WriteLine("Schema Validation (HU-017):");
            Console.WriteLine("--------------------------");

            try
            {
                var client = new SchemaApiClient();
                var validation = await client.ValidateAsync();

                if (validation != null)
                {
                    if (validation.IsValid)
                    {
                        Console.WriteLine("  ✓ Schema is valid and consistent");
                        Console.WriteLine($"    Errors:   {validation.Errors.Count}");
                        Console.WriteLine($"    Warnings: {validation.Warnings.Count}");

                        if (verbose && validation.Warnings.Count > 0)
                        {
                            foreach (var warning in validation.Warnings)
                                Console.WriteLine($"    ⚠  {warning}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("  ✗ Schema issues detected");
                        Console.WriteLine($"    Errors:   {validation.Errors.Count}");
                        Console.WriteLine($"    Warnings: {validation.Warnings.Count}");

                        foreach (var error in validation.Errors)
                        {
                            Console.WriteLine($"    ✗  {error}");
                            issues.Add(error);
                        }

                        foreach (var warning in validation.Warnings)
                        {
                            Console.WriteLine($"    ⚠  {warning}");
                            warnings.Add(warning);
                        }
                    }

                    // ── Orphaned migrations detail ─────────────────────────
                    if (validation.OrphanedMigrations.Count > 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("  Orphaned Migrations:");
                        foreach (var orphan in validation.OrphanedMigrations)
                        {
                            var sourceLabel = orphan.Source switch
                            {
                                "ef_history_only" => "EF-only (not in schema_version)",
                                "schema_version_only" => "schema_version-only (not in EF history)",
                                _ => orphan.Source
                            };
                            Console.WriteLine($"    - [{sourceLabel}] {orphan.MigrationId}");

                            if (verbose && !string.IsNullOrWhiteSpace(orphan.Description))
                                Console.WriteLine($"      {orphan.Description}");
                        }

                        if (fix)
                        {
                            Console.WriteLine();
                            Console.WriteLine("  ℹ  --fix for orphaned migrations is planned for a future release.");
                            Console.WriteLine("     For now, use SQL to manually sync schema_version with __EFMigrationsHistory.");
                        }
                    }

                    // ── Policy violations detail ───────────────────────────
                    if (validation.PolicyViolations.Count > 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("  Additive Policy Violations:");
                        foreach (var violation in validation.PolicyViolations)
                        {
                            Console.WriteLine($"    - {Path.GetFileName(violation.FilePath)}:{violation.LineNumber}");
                            Console.WriteLine($"      {violation.Operation}: {violation.Reason}");
                        }

                        issues.Add($"{validation.PolicyViolations.Count} migration policy violation(s) detected. " +
                                   "Fix these before upgrading.");
                    }
                }
                else
                {
                    Console.WriteLine("  ⚠  Could not validate schema via API");
                    warnings.Add("Schema validation unavailable — is the WebAPI running?");
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("  ⚠  Cannot reach GymFlow API for schema validation");
                warnings.Add("Start the WebAPI to run full schema validation: cd src/backend/WebAPI && dotnet run");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠  Schema validation error: {ex.Message}");
                warnings.Add($"Schema validation failed: {ex.Message}");
            }

            // ── Summary ─────────────────────────────────────────────────
            Console.WriteLine();
            Console.WriteLine("════════════════════════════════");
            if (issues.Count == 0 && warnings.Count == 0)
            {
                Console.WriteLine("✓ ALL CHECKS PASSED");
                Console.WriteLine("  Docker, PostgreSQL, Redis and Schema are healthy.");
            }
            else if (issues.Count == 0)
            {
                Console.WriteLine("✓ SYSTEM IS HEALTHY (warnings only)");
                Console.WriteLine($"  {warnings.Count} warning(s):");
                foreach (var warning in warnings)
                    Console.WriteLine($"    ⚠  {warning}");
            }
            else
            {
                Console.WriteLine("✗ ISSUES DETECTED");
                Console.WriteLine($"  {issues.Count} error(s):");
                foreach (var issue in issues)
                    Console.WriteLine($"    ✗  {issue}");

                if (warnings.Count > 0)
                {
                    Console.WriteLine($"  {warnings.Count} warning(s):");
                    foreach (var warning in warnings)
                        Console.WriteLine($"    ⚠  {warning}");
                }
            }

            if (fix && issues.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Some issues require manual intervention. --fix handled what it could.");
            }
        }, fixOption, verboseOption);

        return command;
    }

    // ── Infrastructure checks ───────────────────────────────────────────────────

    private static bool CheckDocker()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch { return false; }
    }

    private static bool CheckPostgreSQL()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "ps --filter name=postgres -q",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return !string.IsNullOrWhiteSpace(process.StandardOutput.ReadToEnd());
        }
        catch { return false; }
    }

    private static bool CheckRedis()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "ps --filter name=redis -q",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return !string.IsNullOrWhiteSpace(process.StandardOutput.ReadToEnd());
        }
        catch { return false; }
    }
}
