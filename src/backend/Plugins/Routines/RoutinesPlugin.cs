// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using GymFlow.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymFlow.Plugins.Routines;

public class RoutinesPlugin : IPlugin
{
    public PluginMetadata Metadata => new(
        Id: "routines",
        Name: "Routines",
        Version: "1.0.0",
        OfflineCapable: true);

    public void ConfigureServices(IServiceCollection services)
    {
        // Core use cases already registered in Program.cs
    }
}