// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using System.Reflection;
using GymFlow.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymFlow.WebAPI.Plugins;

public class DiscoveredPlugin
{
    public required IPlugin Instance { get; init; }
    public required PluginMetadata Metadata { get; init; }
    public required string AssemblyPath { get; init; }
}

public interface IPluginLoader
{
    Task<IEnumerable<DiscoveredPlugin>> DiscoverAsync(string pluginsPath);
    void ValidatePlugin(IPlugin plugin);
    void RegisterServices(IServiceCollection services, IPlugin plugin);
}

public class PluginLoader : IPluginLoader
{
    private readonly ILogger<PluginLoader> _logger;
    private readonly HashSet<Type> _registeredServiceTypes = new();

    public PluginLoader(ILogger<PluginLoader> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<DiscoveredPlugin>> DiscoverAsync(string pluginsPath)
    {
        var plugins = new List<DiscoveredPlugin>();

        if (!Directory.Exists(pluginsPath))
        {
            _logger.LogInformation("Plugins directory does not exist: {Path}", pluginsPath);
            return plugins;
        }

        var dllFiles = Directory.GetFiles(pluginsPath, "*.dll");
        _logger.LogInformation("Found {Count} DLL files in plugins directory", dllFiles.Length);

        foreach (var dllPath in dllFiles)
        {
            try
            {
                var assembly = await LoadAssemblyAsync(dllPath);
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var pluginType in pluginTypes)
                {
                    var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
                    ValidatePlugin(plugin);

                    plugins.Add(new DiscoveredPlugin
                    {
                        Instance = plugin,
                        Metadata = plugin.Metadata,
                        AssemblyPath = dllPath
                    });

                    _logger.LogInformation("Discovered plugin: {Id} v{Version}", plugin.Metadata.Id, plugin.Metadata.Version);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load plugin from {Path}", dllPath);
            }
        }

        return plugins;
    }

    public void ValidatePlugin(IPlugin plugin)
    {
        if (string.IsNullOrWhiteSpace(plugin.Metadata.Id))
        {
            throw new InvalidOperationException("Plugin metadata Id is required.");
        }

        if (string.IsNullOrWhiteSpace(plugin.Metadata.Name))
        {
            throw new InvalidOperationException($"Plugin with Id '{plugin.Metadata.Id}' must have a Name.");
        }

        if (string.IsNullOrWhiteSpace(plugin.Metadata.Version))
        {
            throw new InvalidOperationException($"Plugin '{plugin.Metadata.Id}' must have a Version.");
        }
    }

    public void RegisterServices(IServiceCollection services, IPlugin plugin)
    {
        var initialCount = services.Count;
        plugin.ConfigureServices(services);

        var newServices = services.Count - initialCount;
        _logger.LogDebug("Plugin {Id} registered {Count} services", plugin.Metadata.Id, newServices);
    }

    private static async Task<Assembly> LoadAssemblyAsync(string path)
    {
        var bytes = await File.ReadAllBytesAsync(path);
        return Assembly.Load(bytes);
    }
}