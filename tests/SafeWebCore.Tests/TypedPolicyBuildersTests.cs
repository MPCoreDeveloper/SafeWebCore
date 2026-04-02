using SafeWebCore.Builder;

namespace SafeWebCore.Tests;

/// <summary>
/// Tests for typed non-CSP policy builders.
/// </summary>
public sealed class TypedPolicyBuildersTests
{
    [Fact]
    public void ReferrerPolicyBuilderBuildsExpectedValue()
    {
        // Arrange
        var builder = new ReferrerPolicyBuilder();

        // Act
        var result = builder.NoReferrer().Build();

        // Assert
        Assert.Equal("no-referrer", result);
    }

    [Fact]
    public void PermissionsPolicyBuilderDisableBuildsEmptyAllowList()
    {
        // Arrange
        var builder = new PermissionsPolicyBuilder();

        // Act
        var result = builder.Disable(PermissionsFeature.Camera).Build();

        // Assert
        Assert.Equal("camera=()", result);
    }

    [Fact]
    public void PermissionsPolicyBuilderAllowSelfBuildsSelfAllowList()
    {
        // Arrange
        var builder = new PermissionsPolicyBuilder();

        // Act
        var result = builder.AllowSelf(PermissionsFeature.Geolocation).Build();

        // Assert
        Assert.Equal("geolocation=(self)", result);
    }

    [Fact]
    public void PermissionsPolicyBuilderAllowOriginNormalizesQuotes()
    {
        // Arrange
        var builder = new PermissionsPolicyBuilder();

        // Act
        var result = builder.Allow(PermissionsFeature.Microphone, "https://voice.example.com").Build();

        // Assert
        Assert.Equal("microphone=(\"https://voice.example.com\")", result);
    }

    [Fact]
    public void CrossOriginPolicyBuilderBuildReturnsTypedValues()
    {
        // Arrange
        var builder = new CrossOriginPolicyBuilder();

        // Act
        var values = builder.CoepCredentialless().CoopSameOriginAllowPopups().CorpSameSite().Build();

        // Assert
        Assert.Equal("credentialless", values.Coep);
    }

    [Fact]
    public void CrossOriginPolicyBuilderBuildSetsConfiguredCoop()
    {
        // Arrange
        var builder = new CrossOriginPolicyBuilder();

        // Act
        var values = builder.CoepCredentialless().CoopSameOriginAllowPopups().CorpSameSite().Build();

        // Assert
        Assert.Equal("same-origin-allow-popups", values.Coop);
    }

    [Fact]
    public void CrossOriginPolicyBuilderBuildSetsConfiguredCorp()
    {
        // Arrange
        var builder = new CrossOriginPolicyBuilder();

        // Act
        var values = builder.CoepCredentialless().CoopSameOriginAllowPopups().CorpSameSite().Build();

        // Assert
        Assert.Equal("same-site", values.Corp);
    }
}
