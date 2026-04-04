using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;
using SafeWebCore.Extensions;
using SafeWebCore.Options;

namespace SafeWebCore.Tests;

/// <summary>
/// Integration tests for startup validation of NetSecureHeaders options.
/// </summary>
public sealed class NetSecureHeadersOptionsValidationTests
{
    [Fact]
    public void StartWithCspReportOnlyAndCspDisabledThrowsOptionsValidationException()
    {
        // Arrange
        var hostBuilder = CreateHostBuilder(opts =>
        {
            opts.EnableCsp = false;
            opts.UseCspReportOnly = true;
        });

        // Act + Assert
        Assert.Throws<OptionsValidationException>(() => hostBuilder.Start());
    }

    [Fact]
    public void StartWithDuplicatePathPoliciesThrowsOptionsValidationException()
    {
        // Arrange
        var hostBuilder = CreateHostBuilder(opts =>
        {
            opts.PathPolicies.Add(new()
            {
                PathPrefix = "/api",
                Options = new()
            });

            opts.PathPolicies.Add(new()
            {
                PathPrefix = "api",
                Options = new()
            });
        });

        // Act + Assert
        Assert.Throws<OptionsValidationException>(() => hostBuilder.Start());
    }

    [Fact]
    public void StartWithDuplicateAdditionalHeadersThrowsOptionsValidationException()
    {
        // Arrange
        var hostBuilder = CreateHostBuilder(opts =>
        {
            opts.AdditionalHeaders.Add(new()
            {
                Name = "Document-Policy",
                Value = "force-load-at-top"
            });

            opts.AdditionalHeaders.Add(new()
            {
                Name = "document-policy",
                Value = "js-profiling"
            });
        });

        // Act + Assert
        Assert.Throws<OptionsValidationException>(() => hostBuilder.Start());
    }

    [Fact]
    public void StartWithRelativeReportingEndpointUrlThrowsOptionsValidationException()
    {
        // Arrange
        var hostBuilder = CreateHostBuilder(opts =>
        {
            opts.ReportingEndpoints.Add(new()
            {
                Group = "default",
                Url = "/reports"
            });
        });

        // Act + Assert
        Assert.Throws<OptionsValidationException>(() => hostBuilder.Start());
    }

    private static IHostBuilder CreateHostBuilder(Action<NetSecureHeadersOptions> configure)
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
                    app.UseNetSecureHeaders();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "Hello World");
                    });
                });
            });
}
