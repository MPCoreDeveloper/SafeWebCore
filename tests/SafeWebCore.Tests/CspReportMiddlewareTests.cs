using System.Text;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SafeWebCore.Abstractions;
using SafeWebCore.Extensions;
using SafeWebCore.Infrastructure;
using SafeWebCore.Models;

namespace SafeWebCore.Tests;

/// <summary>
/// Integration tests for CSP report middleware parsing and sink dispatch.
/// </summary>
public sealed class CspReportMiddlewareTests
{
    [Fact]
    public async Task PostValidCspReportWritesToSink()
    {
        // Arrange
        var spySink = new SpyCspReportSink();
        using var host = await CreateHostAsync(services =>
        {
            services.AddSingleton<ICspReportSink>(spySink);
        });

        using var client = host.GetTestClient();
        const string payload = """
            {
              "csp-report": {
                "document-uri": "https://example.com/",
                "violated-directive": "script-src-elem",
                "effective-directive": "script-src",
                "blocked-uri": "https://evil.example.com/script.js",
                "disposition": "enforce",
                "status-code": 200
              }
            }
            """;

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/csp-report", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
        Assert.Single(spySink.Reports);
    }

    [Fact]
    public async Task PostInvalidJsonReturnsBadRequest()
    {
        // Arrange
        using var host = await CreateHostAsync();
        using var client = host.GetTestClient();
        using var content = new StringContent("{ invalid", Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/csp-report", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCspReportWithoutDirectiveReturnsBadRequest()
    {
        // Arrange
        using var host = await CreateHostAsync();
        using var client = host.GetTestClient();
        const string payload = """
            {
              "csp-report": {
                "document-uri": "https://example.com/"
              }
            }
            """;

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/csp-report", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostValidCspReportEmitsCspViolationSecurityEvent()
    {
        // Arrange
        var spySink = new SpyCspReportSink();
        var eventSpy = new SpySecurityEventSink();

        using var host = await CreateHostAsync(services =>
        {
            services.AddSingleton<ICspReportSink>(spySink);
            services.AddSingleton<ISecurityEventSink>(eventSpy);
        });

        using var client = host.GetTestClient();
        const string payload = """
            {
              "csp-report": {
                "document-uri": "https://example.com/page",
                "violated-directive": "script-src-elem",
                "effective-directive": "script-src",
                "blocked-uri": "https://evil.example.com/script.js",
                "disposition": "enforce"
              }
            }
            """;

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/csp-report", content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

        var violationEvent = eventSpy.Events.FirstOrDefault(e => e.EventType == SecurityEventType.CspViolation);
        Assert.NotNull(violationEvent);
        Assert.Equal("https://example.com/page", violationEvent.Path);
        Assert.Equal("script-src-elem", violationEvent.Properties["ViolatedDirective"]);
        Assert.Equal("https://evil.example.com/script.js", violationEvent.Properties["BlockedUri"]);
    }

    private static async Task<IHost> CreateHostAsync(Action<IServiceCollection>? configureServices = null)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeaders(_ => { });
                    configureServices?.Invoke(services);
                });
                webBuilder.Configure(app =>
                {
                    app.UseCspReport();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "OK");
                    });
                });
            });

        return await hostBuilder.StartAsync(TestContext.Current.CancellationToken);
    }

    private sealed class SpyCspReportSink : ICspReportSink
    {
        public List<CspViolationReport> Reports { get; } = [];

        public Task WriteAsync(CspViolationReport report, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(report);
            Reports.Add(report);
            return Task.CompletedTask;
        }
    }

    private sealed class SpySecurityEventSink : ISecurityEventSink
    {
        public List<SecurityEvent> Events { get; } = [];

        public Task WriteAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(securityEvent);
            return Task.CompletedTask;
        }
    }
}
