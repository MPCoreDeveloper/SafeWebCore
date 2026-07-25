using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using SafeWebCore.Extensions;
using SafeWebCore.Options;

namespace SafeWebCore.Tests;

/// <summary>
/// Integration tests for the SafeWebCore diagnostics endpoint.
/// </summary>
public sealed class DiagnosticsEndpointTests
{
    [Fact]
    public async Task DiagnosticsEndpointReturnsEffectiveGlobalHeaders()
    {
        // Arrange
        using var host = await CreateHostAsync(opts =>
        {
            opts.RemoveXPoweredBy = true;
            opts.EnableXRobotsTag = true;
            opts.XRobotsTagValue = "noindex";
            opts.AdditionalHeaders.Add(new AdditionalHeaderOptions
            {
                Name = "X-Test-Header",
                Value = "enabled"
            });
        });

        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/safewebcore/diagnostics", TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        response.EnsureSuccessStatusCode();

        var root = document.RootElement;
        Assert.True(root.GetProperty("usesGlobalPolicy").GetBoolean());
        Assert.Equal("/", root.GetProperty("path").GetString());

        var headers = root.GetProperty("headers");
        Assert.Equal("noindex", headers.GetProperty("X-Robots-Tag").GetString());
        Assert.Equal("enabled", headers.GetProperty("X-Test-Header").GetString());
        Assert.True(headers.TryGetProperty("Content-Security-Policy", out _));
        Assert.Equal("(removed on response start)", headers.GetProperty("X-Powered-By").GetString());

        var warnings = root.GetProperty("warnings").EnumerateArray().Select(static x => x.GetString()).ToArray();
        Assert.Contains(warnings, warning => warning is not null && warning.Contains("OnStarting", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DiagnosticsEndpointResolvesPathPolicyAndSupportsCspModeOverride()
    {
        // Arrange
        using var host = await CreateHostAsync(opts =>
        {
            opts.PathPolicies.Add(new PathPolicyOptions
            {
                PathPrefix = "/api",
                Options = new NetSecureHeadersOptions
                {
                    ReferrerPolicyValue = "no-referrer",
                    UseCspReportOnly = true
                }
            });
        });

        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/safewebcore/diagnostics?path=/api/orders&cspMode=Enforce", TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        response.EnsureSuccessStatusCode();

        var root = document.RootElement;
        Assert.Equal("/api", root.GetProperty("matchedPathPolicy").GetString());
        Assert.False(root.GetProperty("usesGlobalPolicy").GetBoolean());
        Assert.Equal("Enforce", root.GetProperty("effectiveCspModeOverride").GetString());

        var configuredPathPolicies = root.GetProperty("configuredPathPolicies").EnumerateArray().Select(static x => x.GetString()).ToArray();
        Assert.Contains("/api", configuredPathPolicies);

        var headers = root.GetProperty("headers");
        Assert.Equal("no-referrer", headers.GetProperty("Referrer-Policy").GetString());
        Assert.True(headers.TryGetProperty("Content-Security-Policy", out _));
        Assert.False(headers.TryGetProperty("Content-Security-Policy-Report-Only", out _));

        var warnings = root.GetProperty("warnings").EnumerateArray().Select(static x => x.GetString()).ToArray();
        Assert.Contains(warnings, warning => warning is not null && warning.Contains("forcing enforcement", StringComparison.OrdinalIgnoreCase));
    }

    private static Task<IHost> CreateHostAsync(Action<NetSecureHeadersOptions> configure)
        => new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeaders(configure);
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseNetSecureHeaders();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapSafeWebCoreDiagnostics();
                        endpoints.MapGet("/", () => "Hello World");
                    });
                });
            })
            .StartAsync(TestContext.Current.CancellationToken);
}
