using Microsoft.AspNetCore.TestHost;
using SafeWebCore.Extensions;
using SafeWebCore.Metadata;
using SafeWebCore.Options;

namespace SafeWebCore.Tests;

/// <summary>
/// Integration tests for NetSecureHeadersMiddleware using a custom test server.
/// </summary>
public sealed class NetSecureHeadersMiddlewareTests : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;

    public NetSecureHeadersMiddlewareTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeaders(_ => { });
                });
                webBuilder.Configure(app =>
                {
                    app.UseNetSecureHeaders();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "Hello World");
                    });
                });
            })
            .Start();

        _client = _host.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetRequestAddsSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        Assert.Contains("default-src 'none'", csp);
        Assert.Contains("script-src 'nonce-", csp);

        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
    }

    [Fact]
    public async Task GetRequestNonceIsUniquePerRequest()
    {
        // Act
        var ct = TestContext.Current.CancellationToken;
        var response1 = await _client.GetAsync("/", ct);
        var response2 = await _client.GetAsync("/", ct);

        // Assert — nonces must differ between requests
        var csp1 = response1.Headers.GetValues("Content-Security-Policy").First();
        var csp2 = response2.Headers.GetValues("Content-Security-Policy").First();
        Assert.NotEqual(csp1, csp2);
    }

    [Fact]
    public async Task GetRequestWithReportOnlyEnabledAddsCspReportOnlyHeader()
    {
        // Arrange
        using var reportOnlyHost = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeaders(opts =>
                    {
                        opts.UseCspReportOnly = true;
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseNetSecureHeaders();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "Hello World");
                    });
                });
            })
            .StartAsync(TestContext.Current.CancellationToken);

        using var reportOnlyClient = reportOnlyHost.GetTestClient();

        // Act
        var response = await reportOnlyClient.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.True(response.Headers.Contains("Content-Security-Policy-Report-Only"));
        Assert.False(response.Headers.Contains("Content-Security-Policy"));

        var csp = response.Headers.GetValues("Content-Security-Policy-Report-Only").First();
        Assert.Contains("default-src 'none'", csp);
        Assert.Contains("script-src 'nonce-", csp);

        await reportOnlyHost.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRequestOnMappedPathUsesPathSpecificPolicy()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeaders(opts =>
                    {
                        opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";
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
                });
                webBuilder.Configure(app =>
                {
                    app.UseNetSecureHeaders();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "Root");
                        endpoints.MapGet("/api/ping", () => "Pong");
                    });
                });
            })
            .StartAsync(TestContext.Current.CancellationToken);

        using var client = host.GetTestClient();

        // Act
        var rootResponse = await client.GetAsync("/", TestContext.Current.CancellationToken);
        var apiResponse = await client.GetAsync("/api/ping", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("strict-origin-when-cross-origin", rootResponse.Headers.GetValues("Referrer-Policy").First());
        Assert.True(rootResponse.Headers.Contains("Content-Security-Policy"));
        Assert.False(rootResponse.Headers.Contains("Content-Security-Policy-Report-Only"));

        Assert.Equal("no-referrer", apiResponse.Headers.GetValues("Referrer-Policy").First());
        Assert.True(apiResponse.Headers.Contains("Content-Security-Policy-Report-Only"));
        Assert.False(apiResponse.Headers.Contains("Content-Security-Policy"));

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRequestWithNestedPathUsesLongestMatchingPolicy()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeaders(opts =>
                    {
                        opts.PathPolicies.Add(new PathPolicyOptions
                        {
                            PathPrefix = "/api",
                            Options = new NetSecureHeadersOptions
                            {
                                XFrameOptionsValue = "DENY"
                            }
                        });

                        opts.PathPolicies.Add(new PathPolicyOptions
                        {
                            PathPrefix = "/api/admin",
                            Options = new NetSecureHeadersOptions
                            {
                                XFrameOptionsValue = "SAMEORIGIN"
                            }
                        });
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseNetSecureHeaders();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/admin/dashboard", () => "Admin");
                    });
                });
            })
            .StartAsync(TestContext.Current.CancellationToken);

        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/api/admin/dashboard", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("SAMEORIGIN", response.Headers.GetValues("X-Frame-Options").First());

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRequestOnEndpointWithSkipMetadataDoesNotAddSecurityHeaders()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeaders(_ => { });
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseNetSecureHeaders();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/public", () => "Public").SkipNetSecureHeaders();
                    });
                });
            })
            .StartAsync(TestContext.Current.CancellationToken);

        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/public", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Content-Security-Policy"));
        Assert.False(response.Headers.Contains("X-Frame-Options"));

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRequestOnEndpointWithReportOnlyMetadataOverridesGlobalCspMode()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeaders(opts =>
                    {
                        opts.UseCspReportOnly = false;
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseNetSecureHeaders();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/report-only", () => "ReportOnly")
                            .WithCspMode(CspEndpointMode.ReportOnly);
                    });
                });
            })
            .StartAsync(TestContext.Current.CancellationToken);

        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/report-only", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Content-Security-Policy-Report-Only"));
        Assert.False(response.Headers.Contains("Content-Security-Policy"));

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRequestDoesNotEmitOptionalAdditionalHeadersByDefault()
    {
        // Act
        var response = await _client.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert
        Assert.False(response.Headers.Contains("Origin-Agent-Cluster"));
        Assert.False(response.Headers.Contains("X-Robots-Tag"));
        Assert.False(response.Headers.Contains("Clear-Site-Data"));
        Assert.False(response.Headers.Contains("Reporting-Endpoints"));
    }

    [Fact]
    public async Task GetRequestWithOptionalAdditionalHeadersEnabledEmitsConfiguredValues()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeaders(opts =>
                    {
                        opts.EnableOriginAgentCluster = true;
                        opts.OriginAgentClusterValue = "?1";
                        opts.EnableXRobotsTag = true;
                        opts.XRobotsTagValue = "noindex, nofollow";
                        opts.EnableClearSiteData = true;
                        opts.ClearSiteDataValue = "\"cache\", \"cookies\"";
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseNetSecureHeaders();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "Hello World");
                    });
                });
            })
            .StartAsync(TestContext.Current.CancellationToken);

        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("?1", response.Headers.GetValues("Origin-Agent-Cluster").First());
        Assert.Equal("noindex, nofollow", response.Headers.GetValues("X-Robots-Tag").First());
        Assert.Equal("\"cache\", \"cookies\"", response.Headers.GetValues("Clear-Site-Data").First());

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRequestWithAdditionalHeadersEmitsConfiguredValues()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeaders(opts =>
                    {
                        opts.AdditionalHeaders.Add(new()
                        {
                            Name = "Document-Policy",
                            Value = "force-load-at-top"
                        });
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseNetSecureHeaders();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "Hello World");
                    });
                });
            })
            .StartAsync(TestContext.Current.CancellationToken);

        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("force-load-at-top", response.Headers.GetValues("Document-Policy").First());

        await host.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRequestWithReportingEndpointsEmitsReportingEndpointsHeader()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeaders(opts =>
                    {
                        opts.ReportingEndpoints.Add(new()
                        {
                            Group = "default",
                            Url = "https://reports.example.com/default"
                        });
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseNetSecureHeaders();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "Hello World");
                    });
                });
            })
            .StartAsync(TestContext.Current.CancellationToken);

        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("default=\"https://reports.example.com/default\"", response.Headers.GetValues("Reporting-Endpoints").First());

        await host.StopAsync(TestContext.Current.CancellationToken);
    }
}
