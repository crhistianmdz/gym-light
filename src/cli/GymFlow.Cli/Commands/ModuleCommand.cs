// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using System.CommandLine;
using GymFlow.Cli.Services;

namespace GymFlow.Cli.Commands;

/// <summary>
/// gymflow module — Manage GymFlow plugins/modules (HU-016).
///
/// Communicates with the GymFlow WebAPI PluginsController to list,
/// enable, and disable modules via the plugin system (HU-015).
/// </summary>
public static class ModuleCommand
{
    public static Command Build()
    {
        var moduleCommand = new Command("module", "Manage GymFlow modules");

        // ── list ────────────────────────────────────────────────────────────
        var listCommand = new Command("list", "List all available modules");
        listCommand.SetHandler(async () =>
        {
            Console.WriteLine("Available Modules");
            Console.WriteLine("=================");

            try
            {
                var client = new PluginApiClient();
                var plugins = await client.GetAllAsync();

                if (plugins == null || plugins.Count == 0)
                {
                    Console.WriteLine("  No modules registered.");
                    Console.WriteLine();
                    Console.WriteLine("  Make sure the WebAPI is running:");
                    Console.WriteLine("    cd src/backend/WebAPI && dotnet run");
                    return;
                }

                // Table header
                Console.WriteLine();
                Console.WriteLine($"  {"Name",-22} {"Version",-10} {"Enabled",-8} {"Offline Capable",-16}");
                Console.WriteLine($"  {"────",-22} {"───────",-10} {"───────",-8} {"───────────────",-16}");

                foreach (var p in plugins)
                {
                    var enabled = p.Enabled ? "✓ Yes" : "✗ No";
                    var offline = p.OfflineCapable ? "✓ Yes" : "✗ No";
                    Console.WriteLine($"  {p.Name,-22} {p.Version,-10} {enabled,-8} {offline,-16}");
                }

                Console.WriteLine();
                Console.WriteLine($"  Total: {plugins.Count} module(s)");
            }
            catch (HttpRequestException)
            {
                Console.WriteLine();
                Console.WriteLine("⚠  Cannot reach GymFlow API — showing local info only");
                Console.WriteLine();
                ShowFallbackList();
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine();
                Console.WriteLine("⚠  Request timed out. Make sure the WebAPI is running.");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"✗ Unexpected error: {ex.Message}");
            }
        });

        // ── enable ──────────────────────────────────────────────────────────
        var enableCommand = new Command("enable", "Enable a module");
        var nameArg = new Argument<string>("name", "Module name to enable");
        enableCommand.AddArgument(nameArg);
        enableCommand.SetHandler(async (string name) =>
        {
            try
            {
                var client = new PluginApiClient();
                var plugin = await client.EnableAsync(name);

                if (plugin != null)
                {
                    Console.WriteLine($"✓ Module '{plugin.Name}' enabled successfully.");
                    Console.WriteLine($"  Version: {plugin.Version}");
                    Console.WriteLine($"  Status:  {(plugin.Enabled ? "Enabled" : "Disabled")}");
                }
                // Error messages are printed by PluginApiClient
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("✗ Cannot connect to GymFlow API.");
                Console.WriteLine("  Make sure the WebAPI is running:");
                Console.WriteLine("    cd src/backend/WebAPI && dotnet run");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("⚠  Request timed out. Make sure the WebAPI is running.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Unexpected error: {ex.Message}");
            }
        }, nameArg);

        // ── disable ─────────────────────────────────────────────────────────
        var disableCommand = new Command("disable", "Disable a module");
        var disableNameArg = new Argument<string>("name", "Module name to disable");
        disableCommand.AddArgument(disableNameArg);
        disableCommand.SetHandler(async (string name) =>
        {
            try
            {
                var client = new PluginApiClient();
                var plugin = await client.DisableAsync(name);

                if (plugin != null)
                {
                    Console.WriteLine($"✓ Module '{plugin.Name}' disabled successfully.");
                    Console.WriteLine($"  Version: {plugin.Version}");
                    Console.WriteLine($"  Status:  {(plugin.Enabled ? "Enabled" : "Disabled")}");
                }
                // Error messages are printed by PluginApiClient
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("✗ Cannot connect to GymFlow API.");
                Console.WriteLine("  Make sure the WebAPI is running:");
                Console.WriteLine("    cd src/backend/WebAPI && dotnet run");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("⚠  Request timed out. Make sure the WebAPI is running.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Unexpected error: {ex.Message}");
            }
        }, disableNameArg);

        moduleCommand.Add(listCommand);
        moduleCommand.Add(enableCommand);
        moduleCommand.Add(disableCommand);

        return moduleCommand;
    }

    /// <summary>
    /// Fallback display when the API is unreachable.
    /// </summary>
    private static void ShowFallbackList()
    {
        Console.WriteLine("CLI Version: 1.0.0");
        Console.WriteLine("Modules:      (API required for real values)");
        Console.WriteLine();
        Console.WriteLine("Run the WebAPI to get full module listing:");
        Console.WriteLine("  cd src/backend/WebAPI && dotnet run");
    }
}
