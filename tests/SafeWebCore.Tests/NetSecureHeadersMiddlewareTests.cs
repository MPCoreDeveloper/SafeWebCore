using Microsoft.AspNetCore.TestHost;
using SafeWebCore.Extensions;

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
}
