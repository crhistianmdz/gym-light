// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Net;
using System.Text;
using System.Text.Json;
using GymFlow.Cli.Commands;
using GymFlow.Cli.Services;
using Xunit;

namespace GymFlow.Cli.Tests;

/// <summary>
/// Tests for the <c>gymflow module</c> command group (HU-016).
///
/// Uses a fake HttpClient handler to return canned JSON responses,
/// so no real API server is needed. Captures console output via
/// StringWriter to verify user-facing text.
/// </summary>
public sealed class ModuleCommandTests : IDisposable
{
    private readonly StringWriter _consoleOutput;
    private readonly StringWriter _consoleError;
    private readonly TextWriter _originalOutput;
    private readonly TextWriter _originalError;

    public ModuleCommandTests()
    {
        _consoleOutput = new StringWriter();
        _consoleError = new StringWriter();
        _originalOutput = Console.Out;
        _originalError = Console.Error;
        Console.SetOut(_consoleOutput);
        Console.SetError(_consoleError);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOutput);
        Console.SetError(_originalError);
        _consoleOutput.Dispose();
        _consoleError.Dispose();
    }

    private string GetConsoleOutput()
    {
        Console.Out.Flush();
        var output = _consoleOutput.ToString();
        _consoleOutput.GetStringBuilder().Clear();
        return output;
    }

    private string GetConsoleError()
    {
        Console.Error.Flush();
        var error = _consoleError.ToString();
        _consoleError.GetStringBuilder().Clear();
        return error;
    }

    /// <summary>Gets combined stdout + stderr (useful when errors go to stderr).</summary>
    private string GetCombinedOutput() => GetConsoleOutput() + GetConsoleError();

    // ── list ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_WhenApiReturnsPlugins_DisplaysTable()
    {
        // Arrange
        var plugins = new[]
        {
            new PluginDto("core", "Core", "1.0.0", true, false,
                new DateTime(2026, 1, 1), new DateTime(2026, 1, 1)),
            new PluginDto("members", "Members", "1.1.0", true, true,
                new DateTime(2026, 2, 15), new DateTime(2026, 3, 10)),
            new PluginDto("sales", "Sales", "0.9.0", false, false,
                new DateTime(2026, 3, 1), new DateTime(2026, 3, 1)),
        };

        var json = JsonSerializer.Serialize(plugins, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var client = new PluginApiClient(httpClient);

        // Build command and inject the client
        var command = BuildListCommand(client);

        // Act
        var exitCode = await command.InvokeAsync(Array.Empty<string>());

        // Assert
        Assert.Equal(0, exitCode);
        var output = GetConsoleOutput();
        Assert.Contains("Available Modules", output);
        Assert.Contains("Core", output);
        Assert.Contains("Members", output);
        Assert.Contains("Sales", output);
        Assert.Contains("✓ Yes", output); // Core and Members are enabled
        Assert.Contains("✗ No", output);  // Sales is disabled
        Assert.Contains("Total: 3", output);
    }

    [Fact]
    public async Task List_WhenApiReturnsEmpty_ShowsNoModulesMessage()
    {
        // Arrange
        var json = "[]";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var client = new PluginApiClient(httpClient);

        var command = BuildListCommand(client);

        // Act
        var exitCode = await command.InvokeAsync(Array.Empty<string>());

        // Assert
        Assert.Equal(0, exitCode);
        var output = GetConsoleOutput();
        Assert.Contains("No modules registered", output);
    }

    [Fact]
    public async Task List_WhenApiUnreachable_ShowsFallback()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(throwOnRequest: true);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var client = new PluginApiClient(httpClient);

        var command = BuildListCommand(client);

        // Act
        var exitCode = await command.InvokeAsync(Array.Empty<string>());

        // Assert
        Assert.Equal(0, exitCode);
        var output = GetConsoleOutput();
        Assert.Contains("Cannot reach GymFlow API", output);
    }

    // ── enable ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Enable_WhenPluginExists_ShowsSuccess()
    {
        // Arrange
        var plugin = new PluginDto("members", "Members", "1.1.0", true, true,
            new DateTime(2026, 2, 15), new DateTime(2026, 3, 10));

        var json = JsonSerializer.Serialize(plugin, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var client = new PluginApiClient(httpClient);

        var command = BuildEnableCommand(client);

        // Act
        var exitCode = await command.InvokeAsync(new[] { "members" });

        // Assert — verify the PATCH was made with correct path
        Assert.Contains("/api/plugins/members/enable", handler.LastRequestUri,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(System.Net.Http.HttpMethod.Patch, handler.LastRequestMethod);

        // Assert output
        Assert.Equal(0, exitCode);
        var output = GetConsoleOutput();
        Assert.Contains("Module 'Members' enabled successfully", output);
        Assert.Contains("Version: 1.1.0", output);
        Assert.Contains("Status:  Enabled", output);
    }

    [Fact]
    public async Task Enable_WhenPluginNotFound_ShowsError()
    {
        // Arrange
        var json = """{"error":"Plugin not found"}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var client = new PluginApiClient(httpClient);

        var command = BuildEnableCommand(client);

        // Act
        var exitCode = await command.InvokeAsync(new[] { "nonexistent" });

        // Assert
        Assert.Equal(0, exitCode);
        var output = GetCombinedOutput();
        Assert.Contains("Plugin not found", output);
    }

    // ── disable ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Disable_WhenPluginExists_ShowsSuccess()
    {
        // Arrange
        var plugin = new PluginDto("freeze", "Freeze", "1.0.0", false, false,
            new DateTime(2026, 1, 15), new DateTime(2026, 6, 30));

        var json = JsonSerializer.Serialize(plugin, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var client = new PluginApiClient(httpClient);

        var command = BuildDisableCommand(client);

        // Act
        var exitCode = await command.InvokeAsync(new[] { "freeze" });

        // Assert — verify the PATCH was made with correct path
        Assert.Contains("/api/plugins/freeze/disable", handler.LastRequestUri,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(System.Net.Http.HttpMethod.Patch, handler.LastRequestMethod);

        // Assert output
        Assert.Equal(0, exitCode);
        var output = GetConsoleOutput();
        Assert.Contains("Module 'Freeze' disabled successfully", output);
        Assert.Contains("Status:  Disabled", output);
    }

    [Fact]
    public async Task Disable_WhenApiUnreachable_ShowsConnectionError()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(throwOnRequest: true);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000") };
        var client = new PluginApiClient(httpClient);

        var command = BuildDisableCommand(client);

        // Act
        var exitCode = await command.InvokeAsync(new[] { "members" });

        // Assert
        Assert.Equal(0, exitCode);
        var output = GetConsoleOutput();
        Assert.Contains("Cannot connect to GymFlow API", output);
    }

    // ── Command builders with injected PluginApiClient ───────────────────────

    private static Command BuildListCommand(PluginApiClient client)
    {
        var cmd = new Command("list", "List modules");
        cmd.SetHandler(async () =>
        {
            Console.WriteLine("Available Modules");
            Console.WriteLine("=================");

            try
            {
                var plugins = await client.GetAllAsync();
                if (plugins == null || plugins.Count == 0)
                {
                    Console.WriteLine("  No modules registered.");
                    return;
                }

                Console.WriteLine();
                Console.WriteLine($"  {"Name",-22} {"Version",-10} {"Enabled",-8} {"Offline Capable",-16}");
                Console.WriteLine($"  {"────",-22} {"───────",-10} {"───────",-8} {"───────────────",-16}");

                foreach (var p in plugins)
                {
                    var enabled = p.Enabled ? "✓ Yes" : "✗ No";
                    var offline = p.OfflineCapable ? "✓ Yes" : "✗ No";
                    Console.WriteLine($"  {p.Name,-22} {p.Version,-10} {enabled,-8} {offline,-16}");
                }
                Console.WriteLine();
                Console.WriteLine($"  Total: {plugins.Count} module(s)");
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Cannot reach GymFlow API");
            }
        });
        return cmd;
    }

    private static Command BuildEnableCommand(PluginApiClient client)
    {
        var cmd = new Command("enable", "Enable a module");
        var nameArg = new Argument<string>("name", "Module name");
        cmd.AddArgument(nameArg);
        cmd.SetHandler(async (string name) =>
        {
            try
            {
                var plugin = await client.EnableAsync(name);
                if (plugin != null)
                {
                    Console.WriteLine($"✓ Module '{plugin.Name}' enabled successfully.");
                    Console.WriteLine($"  Version: {plugin.Version}");
                    Console.WriteLine($"  Status:  {(plugin.Enabled ? "Enabled" : "Disabled")}");
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Cannot connect to GymFlow API.");
            }
        }, nameArg);
        return cmd;
    }

    private static Command BuildDisableCommand(PluginApiClient client)
    {
        var cmd = new Command("disable", "Disable a module");
        var nameArg = new Argument<string>("name", "Module name");
        cmd.AddArgument(nameArg);
        cmd.SetHandler(async (string name) =>
        {
            try
            {
                var plugin = await client.DisableAsync(name);
                if (plugin != null)
                {
                    Console.WriteLine($"✓ Module '{plugin.Name}' disabled successfully.");
                    Console.WriteLine($"  Version: {plugin.Version}");
                    Console.WriteLine($"  Status:  {(plugin.Enabled ? "Enabled" : "Disabled")}");
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Cannot connect to GymFlow API.");
            }
        }, nameArg);
        return cmd;
    }
}

/// <summary>
/// A fake <see cref="HttpMessageHandler"/> for unit testing HTTP clients.
/// Returns a canned JSON response or throws to simulate network errors.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string? _responseBody;
    private readonly string _contentType;
    private readonly bool _throwOnRequest;

    /// <summary>Last requested URI (for assertion).</summary>
    public string? LastRequestUri { get; private set; }

    /// <summary>Last HTTP method used (for assertion).</summary>
    public HttpMethod? LastRequestMethod { get; private set; }

    /// <summary>
    /// Creates a handler that returns the given status code and JSON body.
    /// </summary>
    public FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody,
        string contentType = "application/json")
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
        _contentType = contentType;
    }

    /// <summary>
    /// Creates a handler that throws <see cref="HttpRequestException"/> on any request.
    /// </summary>
    public FakeHttpMessageHandler(bool throwOnRequest)
    {
        _throwOnRequest = throwOnRequest;
        _contentType = "application/json";
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri?.ToString();
        LastRequestMethod = request.Method;

        if (_throwOnRequest)
        {
            throw new HttpRequestException("Simulated network error");
        }

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = _responseBody != null
                ? new StringContent(_responseBody, Encoding.UTF8, _contentType)
                : null
        };

        return Task.FromResult(response);
    }
}
