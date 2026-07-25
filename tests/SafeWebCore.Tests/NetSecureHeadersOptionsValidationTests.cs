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

        // Act
        var exception = Assert.Throws<OptionsValidationException>(() => hostBuilder.Start());

        // Assert
        Assert.Contains("UseCspReportOnly requires EnableCsp to be true", exception.Message);
        Assert.Contains("Fix: set EnableCsp = true, or set UseCspReportOnly = false", exception.Message);
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

        // Act
        var exception = Assert.Throws<OptionsValidationException>(() => hostBuilder.Start());

        // Assert
        Assert.Contains("Duplicate path policy prefix '/api' is not allowed", exception.Message);
        Assert.Contains("'/api' and 'api' collide", exception.Message);
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

        // Act
        var exception = Assert.Throws<OptionsValidationException>(() => hostBuilder.Start());

        // Assert
        Assert.Contains("Duplicate additional header 'document-policy' is not allowed", exception.Message);
        Assert.Contains("Fix: merge the values or keep only one entry per header name", exception.Message);
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

        // Act
        var exception = Assert.Throws<OptionsValidationException>(() => hostBuilder.Start());

        // Assert
        Assert.Contains("Reporting endpoint 'default' URL must be absolute", exception.Message);
        Assert.Contains("Fix: use a full URL such as 'https://reports.example.com/csp'", exception.Message);
    }

    [Fact]
    public void NonHttpSchemeReportingEndpointUrlThrowsOptionsValidationException()
    {
        // Arrange
        var hostBuilder = CreateHostBuilder(opts =>
        {
            opts.ReportingEndpoints.Add(new()
            {
                Group = "default",
                Url = "file:///reports"
            });
        });

        // Act
        var exception = Assert.Throws<OptionsValidationException>(() => hostBuilder.Start());

        // Assert
        Assert.Contains("Reporting endpoint 'default' URL must be absolute", exception.Message);
    }

    [Fact]
    public void StartWithNelEnabledButEmptyNelValueThrowsOptionsValidationException()
    {
        // Arrange
        var hostBuilder = CreateHostBuilder(opts =>
        {
            opts.EnableNel = true;
            opts.NelValue = "";
        });

        // Act
        var exception = Assert.Throws<OptionsValidationException>(() => hostBuilder.Start());

        // Assert
        Assert.Contains("EnableNel is true but NelValue is empty", exception.Message);
        Assert.Contains("Fix: set NelValue to a valid JSON object", exception.Message);
    }

    [Fact]
    public void StartWithNelEnabledButInvalidJsonNelValueThrowsOptionsValidationException()
    {
        // Arrange
        var hostBuilder = CreateHostBuilder(opts =>
        {
            opts.EnableNel = true;
            opts.NelValue = "not-json";
        });

        // Act
        var exception = Assert.Throws<OptionsValidationException>(() => hostBuilder.Start());

        // Assert
        Assert.Contains("NelValue must be a JSON object", exception.Message);
        Assert.Contains("Fix: use a string like", exception.Message);
    }

    [Fact]
    public void StartWithCspReportToWithoutMatchingReportingEndpointThrowsOptionsValidationException()
    {
        // Arrange
        var hostBuilder = CreateHostBuilder(opts =>
        {
            opts.Csp = opts.Csp with { ReportTo = "missing-group" };
            // No ReportingEndpoints entry for "missing-group"
        });

        // Act
        var exception = Assert.Throws<OptionsValidationException>(() => hostBuilder.Start());

        // Assert
        Assert.Contains("Csp.ReportTo references group 'missing-group'", exception.Message);
        Assert.Contains("no ReportingEndpoints entry with that Group exists", exception.Message);
        Assert.Contains("Fix: add ReportingEndpoints.Add", exception.Message);
    }

    [Fact]
    public void StartWithValidNelAndMatchingReportToSucceeds()
    {
        // Arrange - valid configuration should not throw
        var hostBuilder = CreateHostBuilder(opts =>
        {
            opts.EnableNel = true;
            opts.NelValue = "{\"report_to\":\"default\",\"max_age\":2592000}";
            opts.ReportingEndpoints.Add(new()
            {
                Group = "default",
                Url = "https://reports.example.com/nel"
            });
            opts.Csp = opts.Csp with { ReportTo = "default" };
        });

        // Act + Assert - should not throw
        var host = hostBuilder.Build();
        // If we reached here without exception, validation passed
        Assert.NotNull(host);
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
