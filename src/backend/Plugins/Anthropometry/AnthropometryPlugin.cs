// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using GymFlow.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymFlow.Plugins.Anthropometry;

public class AnthropometryPlugin : IPlugin
{
    public PluginMetadata Metadata => new(
        Id: "anthropometry",
        Name: "Anthropometry",
        Version: "1.0.0",
        OfflineCapable: true);

    public void ConfigureServices(IServiceCollection services)
    {
        // Core use cases already registered in Program.cs
        // This plugin provides metadata for tracking purposes
    }
}