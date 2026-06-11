// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;

namespace GymFlow.Plugins.Abstractions;

public interface IPlugin
{
    PluginMetadata Metadata { get; }
    void ConfigureServices(IServiceCollection services);
}