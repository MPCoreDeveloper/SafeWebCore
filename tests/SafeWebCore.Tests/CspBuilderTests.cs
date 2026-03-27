using SafeWebCore.Builder;
using Xunit;

namespace SafeWebCore.Tests;

/// <summary>
/// Tests for CspBuilder.
/// </summary>
public sealed class CspBuilderTests
{
    [Fact]
    public void BuildWithDefaultOptionsReturnsExpectedCsp()
    {
        // Arrange
        var builder = new CspBuilder();

        // Act
        var result = builder.Build().Build();

        // Assert — core fetch directives
        Assert.Contains("default-src 'none'", result);
        Assert.Contains("script-src 'nonce-{nonce}' 'strict-dynamic' https:", result);
        Assert.Contains("style-src 'nonce-{nonce}'", result);
        Assert.Contains("img-src 'self' https: data:", result);
        Assert.Contains("object-src 'none'", result);

        // Assert — document / navigation directives
        Assert.Contains("base-uri 'none'", result);
        Assert.Contains("form-action 'self'", result);
        Assert.Contains("frame-ancestors 'none'", result);

        // Assert — transport
        Assert.Contains("upgrade-insecure-requests", result);

        // block-all-mixed-content is deprecated and disabled by default
        Assert.DoesNotContain("block-all-mixed-content", result);
    }

    [Fact]
    public void DefaultSrcCustomValueSetsCorrectly()
    {
        // Arrange
        var builder = new CspBuilder().DefaultSrc("'self'");

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.Contains("default-src 'self'", result);
    }

    [Fact]
    public void ScriptSrcCustomValueSetsCorrectly()
    {
        // Arrange
        var builder = new CspBuilder().ScriptSrc("'self' 'unsafe-inline'");

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.Contains("script-src 'self' 'unsafe-inline'", result);
    }

    [Fact]
    public void UpgradeInsecureRequestsDisabledNotIncluded()
    {
        // Arrange
        var builder = new CspBuilder().UpgradeInsecureRequests(false);

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.DoesNotContain("upgrade-insecure-requests", result);
    }

    [Fact]
    public void ReportToSetIncluded()
    {
        // Arrange
        var builder = new CspBuilder().ReportTo("default");

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.Contains("report-to default", result);
    }

    [Fact]
    public void ScriptSrcElemSetIncluded()
    {
        // Arrange
        var builder = new CspBuilder().ScriptSrcElem("'self'");

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.Contains("script-src-elem 'self'", result);
    }

    [Fact]
    public void StyleSrcAttrSetIncluded()
    {
        // Arrange
        var builder = new CspBuilder().StyleSrcAttr("'unsafe-inline'");

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.Contains("style-src-attr 'unsafe-inline'", result);
    }

    [Fact]
    public void WorkerSrcSetIncluded()
    {
        // Arrange
        var builder = new CspBuilder().WorkerSrc("'self' blob:");

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.Contains("worker-src 'self' blob:", result);
    }

    [Fact]
    public void RequireTrustedTypesForSetIncluded()
    {
        // Arrange
        var builder = new CspBuilder().RequireTrustedTypesFor("'script'");

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.Contains("require-trusted-types-for 'script'", result);
    }

    [Fact]
    public void TrustedTypesSetIncluded()
    {
        // Arrange
        var builder = new CspBuilder().TrustedTypes("myPolicy 'allow-duplicates'");

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.Contains("trusted-types myPolicy 'allow-duplicates'", result);
    }

    [Fact]
    public void FencedFrameSrcSetIncluded()
    {
        // Arrange
        var builder = new CspBuilder().FencedFrameSrc("'self'");

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.Contains("fenced-frame-src 'self'", result);
    }

    [Fact]
    public void FontSrcSetIncluded()
    {
        // Arrange
        var builder = new CspBuilder().FontSrc("'self' https://fonts.gstatic.com");

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.Contains("font-src 'self' https://fonts.gstatic.com", result);
    }

    [Fact]
    public void ConnectSrcSetIncluded()
    {
        // Arrange
        var builder = new CspBuilder().ConnectSrc("'self' wss://api.example.com");

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.Contains("connect-src 'self' wss://api.example.com", result);
    }

    [Fact]
    public void SandboxSetIncluded()
    {
        // Arrange
        var builder = new CspBuilder().Sandbox("allow-scripts allow-same-origin");

        // Act
        var result = builder.Build().Build();

        // Assert
        Assert.Contains("sandbox allow-scripts allow-same-origin", result);
    }

    [Fact]
    public void EmptyOptionalDirectivesNotIncluded()
    {
        // Arrange — defaults leave optional directives empty
        var builder = new CspBuilder();

        // Act
        var result = builder.Build().Build();

        // Assert — none of the optional directives should appear
        Assert.DoesNotContain("script-src-elem", result);
        Assert.DoesNotContain("script-src-attr", result);
        Assert.DoesNotContain("style-src-elem", result);
        Assert.DoesNotContain("style-src-attr", result);
        Assert.DoesNotContain("font-src", result);
        Assert.DoesNotContain("connect-src", result);
        Assert.DoesNotContain("media-src", result);
        Assert.DoesNotContain("child-src", result);
        Assert.DoesNotContain("worker-src", result);
        Assert.DoesNotContain("manifest-src", result);
        Assert.DoesNotContain("fenced-frame-src", result);
        Assert.DoesNotContain("sandbox", result);
        Assert.DoesNotContain("require-trusted-types-for", result);
        Assert.DoesNotContain("trusted-types", result);
    }
}
