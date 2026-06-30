// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GymFlow.Cli.Services;

/// <summary>
/// HTTP client wrapper for GymFlow WebAPI schema endpoints (HU-017).
///
/// Communicates with the WebAPI's SchemaController via REST calls.
/// The CLI remains decoupled from the backend — no direct .NET references needed.
///
/// Base URL: resolved from environment variable GYMFLOW_API_URL or default http://localhost:5000.
/// Auth: uses GYMFLOW_API_KEY environment variable as Bearer token.
/// </summary>
public class SchemaApiClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SchemaApiClient(HttpClient? http = null)
    {
        _baseUrl = Environment.GetEnvironmentVariable("GYMFLOW_API_URL")
            ?? "http://localhost:5000";

        _http = http ?? new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromMinutes(5) // upgrades can take a while
        };

        // Set auth header if API key is configured
        var apiKey = Environment.GetEnvironmentVariable("GYMFLOW_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    // ── Upgrade ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls POST /api/schema/upgrade to execute a schema upgrade.
    /// </summary>
    public async Task<UpgradeResponse> UpgradeAsync(
        string? targetVersion,
        bool skipBackup,
        bool dryRun,
        bool verbose,
        string? appliedBy = null,
        CancellationToken ct = default)
    {
        var request = new
        {
            targetVersion,
            skipBackup,
            dryRun,
            verbose,
            appliedBy = appliedBy ?? Environment.UserName
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("api/schema/upgrade", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = TryExtractError(body);
            return new UpgradeResponse(false, targetVersion, 0, null, "0s",
                error ?? $"HTTP {response.StatusCode}: {response.ReasonPhrase}", []);
        }

        var dto = JsonSerializer.Deserialize<UpgradeResponse>(body, JsonOptions);
        return dto ?? new UpgradeResponse(false, targetVersion, 0, null, "0s",
            "Failed to parse upgrade response.", []);
    }

    // ── Status ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls GET /api/schema/status to retrieve current schema status.
    /// </summary>
    public async Task<StatusResponse?> GetStatusAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/schema/status", ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var error = TryExtractError(body);
            Console.WriteLine($"✗ Error: {error ?? $"HTTP {response.StatusCode}"}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<StatusResponse>(json, JsonOptions);
    }

    // ── Validate ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calls GET /api/schema/validate to validate schema consistency.
    /// </summary>
    public async Task<ValidationResponse?> ValidateAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/schema/validate", ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var error = TryExtractError(body);
            Console.WriteLine($"✗ Error: {error ?? $"HTTP {response.StatusCode}"}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<ValidationResponse>(json, JsonOptions);
    }

    // ── Helper ──────────────────────────────────────────────────────────────────

    private static string? TryExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("detail", out var detail))
                return detail.GetString();
            if (doc.RootElement.TryGetProperty("title", out var title))
                return title.GetString();
        }
        catch
        {
            // Not JSON or unexpected format
        }

        return body.Length > 200 ? body[..200] + "..." : body;
    }
}

// ── Response DTOs (mirrors WebAPI DTOs for HTTP deserialization) ─────────────

public record UpgradeResponse(
    bool Success,
    string? TargetVersion,
    int MigrationsApplied,
    string? BackupPath,
    string Duration,
    string? ErrorMessage,
    IReadOnlyList<string> AppliedVersions
);

public record StatusResponse(
    string CurrentVersion,
    DateTime? LastMigrationAt,
    string? LastMigrationDescription,
    int PendingMigrationsCount,
    long DiskSpaceBytes,
    string DiskSpaceFormatted,
    string PgVersion,
    bool IsLockHeld,
    IReadOnlyList<ModuleVersionResponse> ModuleVersions
);

public record ModuleVersionResponse(
    string ModuleName,
    string Version,
    DateTime AppliedAt
);

public record ValidationResponse(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<OrphanedMigrationResponse> OrphanedMigrations,
    IReadOnlyList<PolicyViolationResponse> PolicyViolations
);

public record OrphanedMigrationResponse(
    string MigrationId,
    string Source,
    string? FilePath,
    string? Description
);

public record PolicyViolationResponse(
    string FilePath,
    int LineNumber,
    string Operation,
    string Reason
);
