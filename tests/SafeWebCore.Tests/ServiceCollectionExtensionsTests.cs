using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SafeWebCore.Abstractions;
using SafeWebCore.Extensions;
using SafeWebCore.Infrastructure;
using SafeWebCore.Middleware;
using SafeWebCore.Options;

namespace SafeWebCore.Tests;

/// <summary>
/// Tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNetSecureHeadersFromConfigurationBindsDefaultSectionAndRegistersCoreServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NetSecureHeaders:EnableHsts"] = "false",
                ["NetSecureHeaders:XFrameOptionsValue"] = "SAMEORIGIN",
                ["NetSecureHeaders:RemoveXPoweredBy"] = "true",
                ["NetSecureHeaders:UseCspReportOnly"] = "true",
                ["NetSecureHeaders:Csp:DefaultSrc"] = "'self'"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var returnedServices = services.AddNetSecureHeadersFromConfiguration(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NetSecureHeadersOptions>>().Value;

        // Assert
        Assert.Same(services, returnedServices);
        Assert.False(options.EnableHsts);
        Assert.Equal("SAMEORIGIN", options.XFrameOptionsValue);
        Assert.True(options.RemoveXPoweredBy);
        Assert.True(options.UseCspReportOnly);
        Assert.Equal("'self'", options.Csp.DefaultSrc);
        Assert.IsType<NonceService>(provider.GetRequiredService<INonceService>());
        Assert.NotEmpty(provider.GetServices<ICspReportSink>());
        Assert.NotNull(provider.GetRequiredService<NetSecureHeadersMiddleware>());
        Assert.NotNull(provider.GetRequiredService<CspReportMiddleware>());
    }

    [Fact]
    public void AddNetSecureHeadersFromConfigurationBindsNamedSection()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Headers:ReferrerPolicyValue"] = "same-origin",
                ["Security:Headers:EnableNel"] = "true",
                ["Security:Headers:NelValue"] = "{\"report_to\":\"default\",\"max_age\":60}"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNetSecureHeadersFromConfiguration(configuration, "Security:Headers");
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NetSecureHeadersOptions>>().Value;

        // Assert
        Assert.Equal("same-origin", options.ReferrerPolicyValue);
        Assert.True(options.EnableNel);
        Assert.Equal("{\"report_to\":\"default\",\"max_age\":60}", options.NelValue);
    }

    [Fact]
    public void AddNetSecureHeadersFromConfigurationSectionBindsNestedCollections()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SafeWeb:Headers:PathPolicies:0:PathPrefix"] = "/api",
                ["SafeWeb:Headers:PathPolicies:0:Options:UseCspReportOnly"] = "true",
                ["SafeWeb:Headers:PathPolicies:0:Options:Csp:DefaultSrc"] = "'self' https://api.example.com",
                ["SafeWeb:Headers:ReportingEndpoints:0:Group"] = "default",
                ["SafeWeb:Headers:ReportingEndpoints:0:Url"] = "https://reports.example.com/csp",
                ["SafeWeb:Headers:AdditionalHeaders:0:Name"] = "X-Test-Header",
                ["SafeWeb:Headers:AdditionalHeaders:0:Value"] = "enabled"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNetSecureHeadersFromConfiguration(configuration.GetSection("SafeWeb:Headers"));
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NetSecureHeadersOptions>>().Value;

        // Assert
        var pathPolicy = Assert.Single(options.PathPolicies);
        Assert.Equal("/api", pathPolicy.PathPrefix);
        Assert.True(pathPolicy.Options.UseCspReportOnly);
        Assert.Equal("'self' https://api.example.com", pathPolicy.Options.Csp.DefaultSrc);

        var reportingEndpoint = Assert.Single(options.ReportingEndpoints);
        Assert.Equal("default", reportingEndpoint.Group);
        Assert.Equal("https://reports.example.com/csp", reportingEndpoint.Url);

        var additionalHeader = Assert.Single(options.AdditionalHeaders);
        Assert.Equal("X-Test-Header", additionalHeader.Name);
        Assert.Equal("enabled", additionalHeader.Value);
    }

    [Fact]
    public void AddNetSecureHeadersForEnvironmentUsesReportOnlyOutsideProductionByDefault()
    {
        // Arrange
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Staging };
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNetSecureHeadersForEnvironment(environment);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NetSecureHeadersOptions>>().Value;

        // Assert
        Assert.True(options.EnableCsp);
        Assert.True(options.UseCspReportOnly);
    }

    [Fact]
    public void AddNetSecureHeadersStrictAPlusForEnvironmentKeepsStrictPresetAndAllowsOverride()
    {
        // Arrange
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNetSecureHeadersStrictAPlusForEnvironment(environment, opts =>
        {
            opts.UseCspReportOnly = false;
            opts.ReferrerPolicyValue = "same-origin";
        });
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NetSecureHeadersOptions>>().Value;

        // Assert
        Assert.True(options.RemoveXPoweredBy);
        Assert.False(options.UseCspReportOnly);
        Assert.Equal("same-origin", options.ReferrerPolicyValue);
    }

    [Fact]
    public void AddNetSecureHeadersSwaggerRegistersSwaggerPreset()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNetSecureHeadersSwagger();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NetSecureHeadersOptions>>().Value;

        // Assert
        Assert.Contains("'unsafe-inline'", options.Csp.StyleSrc);
        Assert.Contains("https://cdn.jsdelivr.net", options.Csp.ScriptSrc);
    }

    [Fact]
    public void AddNetSecureHeadersReverseProxyPresetRegistersReverseProxyPreset()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNetSecureHeadersReverseProxyPreset();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NetSecureHeadersOptions>>().Value;

        // Assert
        Assert.Contains("https:", options.Csp.ConnectSrc);
        Assert.Contains("wss:", options.Csp.ConnectSrc);
    }

    [Fact]
    public void AddNetSecureHeadersBlazorWebSocketPresetRegistersBlazorWebSocketPreset()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNetSecureHeadersBlazorWebSocketPreset();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NetSecureHeadersOptions>>().Value;

        // Assert
        Assert.Contains("ws:", options.Csp.ConnectSrc);
        Assert.Contains("wss:", options.Csp.ConnectSrc);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "SafeWebCore.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
