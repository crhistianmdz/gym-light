// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Interfaces;

public interface IPluginRegistryRepository
{
    Task<PluginRegistry?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<PluginRegistry>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PluginRegistry>> GetEnabledAsync(CancellationToken ct = default);
    Task AddAsync(PluginRegistry plugin, CancellationToken ct = default);
    Task UpdateAsync(PluginRegistry plugin, CancellationToken ct = default);
    Task UpsertAsync(PluginRegistry plugin, CancellationToken ct = default);
}