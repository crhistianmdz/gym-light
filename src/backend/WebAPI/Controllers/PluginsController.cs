// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using GymFlow.Application.DTOs;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Owner")]
public class PluginsController : ControllerBase
{
    private readonly IPluginRegistryRepository _pluginRegistry;

    public PluginsController(IPluginRegistryRepository pluginRegistry)
    {
        _pluginRegistry = pluginRegistry;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var plugins = await _pluginRegistry.GetAllAsync(ct);
        var response = plugins.Select(p => new PluginResponseDto(
            p.Id,
            p.Name,
            p.Version,
            p.Enabled,
            p.OfflineCapable,
            p.InstalledAt,
            p.LastUpdated));
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var plugin = await _pluginRegistry.GetByIdAsync(id, ct);
        if (plugin == null)
            return NotFound(new { error = "Plugin not found" });

        return Ok(new PluginResponseDto(
            plugin.Id,
            plugin.Name,
            plugin.Version,
            plugin.Enabled,
            plugin.OfflineCapable,
            plugin.InstalledAt,
            plugin.LastUpdated));
    }

    [HttpPatch("{id}/enable")]
    public async Task<IActionResult> Enable(string id, CancellationToken ct)
    {
        var plugin = await _pluginRegistry.GetByIdAsync(id, ct);
        if (plugin == null)
            return NotFound(new { error = "Plugin not found" });

        plugin.Enable();
        await _pluginRegistry.UpdateAsync(plugin, ct);

        return Ok(new PluginResponseDto(
            plugin.Id,
            plugin.Name,
            plugin.Version,
            plugin.Enabled,
            plugin.OfflineCapable,
            plugin.InstalledAt,
            plugin.LastUpdated));
    }

    [HttpPatch("{id}/disable")]
    public async Task<IActionResult> Disable(string id, CancellationToken ct)
    {
        var plugin = await _pluginRegistry.GetByIdAsync(id, ct);
        if (plugin == null)
            return NotFound(new { error = "Plugin not found" });

        plugin.Disable();
        await _pluginRegistry.UpdateAsync(plugin, ct);

        return Ok(new PluginResponseDto(
            plugin.Id,
            plugin.Name,
            plugin.Version,
            plugin.Enabled,
            plugin.OfflineCapable,
            plugin.InstalledAt,
            plugin.LastUpdated));
    }
}