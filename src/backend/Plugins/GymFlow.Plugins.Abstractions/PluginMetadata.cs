// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Plugins.Abstractions;

public record PluginMetadata(
    string Id,
    string Name,
    string Version,
    bool OfflineCapable);