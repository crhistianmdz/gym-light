// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Application.DTOs;

public record PluginResponseDto(
    string Id,
    string Name,
    string Version,
    bool Enabled,
    bool OfflineCapable,
    DateTime InstalledAt,
    DateTime LastUpdated);