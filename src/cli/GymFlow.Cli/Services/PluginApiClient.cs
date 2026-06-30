// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GymFlow.Cli.Services;

/// <summary>
/// HTTP client wrapper for GymFlow WebAPI plugins/modules endpoints.
///
/// Communicates with the WebAPI's PluginsController via REST calls.
/// The CLI remains decoupled from the backend — no direct .NET references needed.
///
/// Base URL: resolved from environment variable GYMFLOW_API_URL or default http://localhost:5000.
/// Auth: uses GYMFLOW_API_KEY environment variable as Bearer token.
/// </summary>
public class PluginApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PluginApiClient(HttpClient? http = null)
    {
        var baseUrl = Environment.GetEnvironmentVariable("GYMFLOW_API_URL")
            ?? "http://localhost:5000";

        _http = http ?? new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Set auth header if API key is configured
        var apiKey = Environment.GetEnvironmentVariable("GYMFLOW_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    // ── List ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls GET /api/plugins to retrieve all registered plugins.
    /// </summary>
    public async Task<IReadOnlyList<PluginDto>?> GetAllAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/plugins", ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var error = TryExtractError(body);
            Console.Error.WriteLine($"✗ Error: {error ?? $"HTTP {response.StatusCode}"}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<IReadOnlyList<PluginDto>>(json, JsonOptions);
    }

    // ── Enable ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls PATCH /api/plugins/{name}/enable to enable a plugin.
    /// </summary>
    public async Task<PluginDto?> EnableAsync(string name, CancellationToken ct = default)
    {
        return await PatchPluginStateAsync($"{name}/enable", ct);
    }

    // ── Disable ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls PATCH /api/plugins/{name}/disable to disable a plugin.
    /// </summary>
    public async Task<PluginDto?> DisableAsync(string name, CancellationToken ct = default)
    {
        return await PatchPluginStateAsync($"{name}/disable", ct);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<PluginDto?> PatchPluginStateAsync(string path, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"api/plugins/{path}");
        var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.Error.WriteLine($"✗ Plugin not found. Make sure the name is correct.");
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = TryExtractError(body);
            Console.Error.WriteLine($"✗ Error: {error ?? $"HTTP {response.StatusCode}: {response.ReasonPhrase}"}");
            return null;
        }

        return JsonSerializer.Deserialize<PluginDto>(body, JsonOptions);
    }

    private static string? TryExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
                return detail.GetString();
            if (doc.RootElement.TryGetProperty("title", out var title))
                return title.GetString();
            if (doc.RootElement.TryGetProperty("error", out var error))
                return error.GetString();
        }
        catch
        {
            // Not JSON or unexpected format
        }

        return body.Length > 200 ? body[..200] + "..." : body;
    }
}

// ── Response DTOs (mirrors WebAPI DTOs for HTTP deserialization) ─────────────

public record PluginDto(
    string Id,
    string Name,
    string Version,
    bool Enabled,
    bool OfflineCapable,
    DateTime InstalledAt,
    DateTime LastUpdated
);
