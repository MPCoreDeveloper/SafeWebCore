using Microsoft.AspNetCore.Authentication.JwtBearer;
using SafeWebCore.JwtBearer;

namespace SafeWebCore.JwtBearer.Tests;

public sealed class JwtBearerConfigurationValidatorTests
{
    [Fact]
    public void FindIssuesReturnsEmptyForSoundConfiguration()
    {
        var options = new JwtBearerOptions { Authority = "https://login.example.com/tenant/v2.0" };
        options.TokenValidationParameters.ValidAudience = "api";
        options.TokenValidationParameters.ValidateAudience = true;

        Assert.Empty(JwtBearerConfigurationValidator.FindIssues(options));
    }

    [Fact]
    public void FindIssuesReportsPlainHttpAuthorityWhenHttpsIsRequired()
    {
        var options = new JwtBearerOptions
        {
            Authority = "http://login.example.com/tenant/v2.0",
            RequireHttpsMetadata = true,
        };

        var issues = JwtBearerConfigurationValidator.FindIssues(options);

        Assert.Contains(issues, issue => issue.Contains("HTTPS", StringComparison.Ordinal));
    }

    [Fact]
    public void FindIssuesReportsRelativeAuthority()
    {
        var options = new JwtBearerOptions { Authority = "/relative/metadata" };

        var issues = JwtBearerConfigurationValidator.FindIssues(options);

        Assert.Contains(issues, issue => issue.Contains("absolute URI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FindIssuesReportsMissingAudienceWhenValidationIsEnabled()
    {
        var options = new JwtBearerOptions();
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateIssuer = false;

        var issues = JwtBearerConfigurationValidator.FindIssues(options);

        Assert.Contains(issues, issue => issue.Contains("Audience", StringComparison.Ordinal));
    }

    [Fact]
    public void FindIssuesIgnoresAudienceRequirementWhenCustomValidatorIsSet()
    {
        var options = new JwtBearerOptions();
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateIssuer = false;
        options.TokenValidationParameters.AudienceValidator = (_, _, _) => true;

        Assert.Empty(JwtBearerConfigurationValidator.FindIssues(options));
    }

    [Fact]
    public void FindIssuesReportsMissingIssuerWithoutMetadata()
    {
        var options = new JwtBearerOptions();
        options.TokenValidationParameters.ValidateAudience = false;
        options.TokenValidationParameters.ValidateIssuer = true;

        var issues = JwtBearerConfigurationValidator.FindIssues(options);

        Assert.Contains(issues, issue => issue.Contains("Issuer", StringComparison.Ordinal));
    }

    [Fact]
    public void FindIssuesAllowsIssuerFromMetadata()
    {
        var options = new JwtBearerOptions { Authority = "https://login.example.com/tenant/v2.0" };
        options.TokenValidationParameters.ValidateAudience = false;
        options.TokenValidationParameters.ValidateIssuer = true;

        Assert.Empty(JwtBearerConfigurationValidator.FindIssues(options));
    }

    [Fact]
    public void FindIssuesReportsNoneAlgorithm()
    {
        var options = new JwtBearerOptions();
        options.TokenValidationParameters.ValidAlgorithms = new[] { "RS256", "none" };

        var issues = JwtBearerConfigurationValidator.FindIssues(options);

        Assert.Contains(issues, issue => issue.Contains("none", StringComparison.OrdinalIgnoreCase));
    }
}