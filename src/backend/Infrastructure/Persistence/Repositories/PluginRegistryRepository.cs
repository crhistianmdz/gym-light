// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence.Repositories;

public class PluginRegistryRepository : IPluginRegistryRepository
{
    private readonly GymFlowDbContext _context;

    public PluginRegistryRepository(GymFlowDbContext context)
    {
        _context = context;
    }

    public async Task<PluginRegistry?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.PluginRegistry.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<PluginRegistry>> GetAllAsync(CancellationToken ct = default) =>
        await _context.PluginRegistry.ToListAsync(ct);

    public async Task<IReadOnlyList<PluginRegistry>> GetEnabledAsync(CancellationToken ct = default) =>
        await _context.PluginRegistry.Where(p => p.Enabled).ToListAsync(ct);

    public async Task AddAsync(PluginRegistry plugin, CancellationToken ct = default)
    {
        await _context.PluginRegistry.AddAsync(plugin, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(PluginRegistry plugin, CancellationToken ct = default)
    {
        _context.PluginRegistry.Update(plugin);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpsertAsync(PluginRegistry plugin, CancellationToken ct = default)
    {
        var existing = await _context.PluginRegistry.FirstOrDefaultAsync(p => p.Id == plugin.Id, ct);
        if (existing == null)
        {
            await _context.PluginRegistry.AddAsync(plugin, ct);
        }
        else
        {
            existing.Name = plugin.Name;
            existing.Version = plugin.Version;
            existing.OfflineCapable = plugin.OfflineCapable;
            existing.Enabled = plugin.Enabled;
            existing.LastUpdated = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync(ct);
    }
}