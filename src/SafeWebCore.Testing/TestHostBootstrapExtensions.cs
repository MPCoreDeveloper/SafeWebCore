using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SafeWebCore.Extensions;
using SafeWebCore.Options;

namespace SafeWebCore.Testing;

/// <summary>
/// Bootstrap helpers for quickly setting up SafeWebCore in integration tests.
/// </summary>
public static class TestHostBootstrapExtensions
{
    /// <summary>
    /// Creates a minimal test host with SafeWebCore Strict A+ preset.
    /// </summary>
    public static IHost CreateSafeWebCoreTestHost(Action<IServiceCollection>? configureServices = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeadersStrictAPlus();
                    configureServices?.Invoke(services);
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseNetSecureHeaders();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "Test");
                    });
                });
            })
            .Start();
    }

    /// <summary>
    /// Creates a minimal test host with SafeWebCore using a custom configuration action.
    /// </summary>
    public static IHost CreateSafeWebCoreTestHost(Action<NetSecureHeadersOptions> configureOptions)
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddNetSecureHeaders(configureOptions);
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseNetSecureHeaders();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "Test");
                    });
                });
            })
            .Start();
    }
}
