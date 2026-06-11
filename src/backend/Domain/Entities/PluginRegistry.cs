// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Domain.Entities;

public class PluginRegistry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool OfflineCapable { get; set; }
    public DateTime InstalledAt { get; set; }
    public DateTime LastUpdated { get; set; }

    public static PluginRegistry Create(string id, string name, string version, bool offlineCapable)
    {
        return new PluginRegistry
        {
            Id = id,
            Name = name,
            Version = version,
            OfflineCapable = offlineCapable,
            Enabled = false,
            InstalledAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
    }

    public void Enable()
    {
        Enabled = true;
        LastUpdated = DateTime.UtcNow;
    }

    public void Disable()
    {
        Enabled = false;
        LastUpdated = DateTime.UtcNow;
    }
}