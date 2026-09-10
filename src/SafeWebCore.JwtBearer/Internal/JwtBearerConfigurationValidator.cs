using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace SafeWebCore.JwtBearer;

/// <summary>
/// Deterministic, network-free checks on the effective <see cref="JwtBearerOptions"/> that
/// complement the framework's own <c>JwtBearerOptions.Validate()</c>. Used by the startup guard.
/// </summary>
internal static class JwtBearerConfigurationValidator
{
    /// <summary>
    /// Returns the configuration issues found. An empty list means the configuration is sound.
    /// </summary>
    /// <param name="options">The effective JWT bearer options to inspect.</param>
    public static IReadOnlyList<string> FindIssues(JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var issues = new List<string>();
        ValidateAuthorityAddress(options, issues);
        ValidateValidationParameters(options, issues);
        return issues;
    }

    private static void ValidateAuthorityAddress(JwtBearerOptions options, List<string> issues)
    {
        var address = options.Authority ?? options.MetadataAddress;
        if (string.IsNullOrWhiteSpace(address))
        {
            // The framework post-configure already requires Authority or MetadataAddress
            // when it has to build the configuration manager itself.
            return;
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
        {
            issues.Add($"The authority/metadata address '{address}' is not an absolute URI.");
            return;
        }

        if (options.RequireHttpsMetadata
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add($"The authority/metadata address '{address}' does not use HTTPS while RequireHttpsMetadata is enabled.");
        }
    }

    private static void ValidateValidationParameters(JwtBearerOptions options, List<string> issues)
    {
        var parameters = options.TokenValidationParameters;

        if (parameters.ValidateAudience
            && parameters.AudienceValidator is null
            && string.IsNullOrWhiteSpace(parameters.ValidAudience)
            && (parameters.ValidAudiences is null || !parameters.ValidAudiences.Any()))
        {
            issues.Add("Audience validation is enabled but no ValidAudience(s) are configured.");
        }

        var resolvesIssuerFromMetadata = !string.IsNullOrWhiteSpace(options.Authority)
            || !string.IsNullOrWhiteSpace(options.MetadataAddress);

        if (!resolvesIssuerFromMetadata
            && parameters.ValidateIssuer
            && parameters.IssuerValidator is null
            && string.IsNullOrWhiteSpace(parameters.ValidIssuer)
            && (parameters.ValidIssuers is null || !parameters.ValidIssuers.Any()))
        {
            issues.Add("Issuer validation is enabled but no ValidIssuer(s) are configured and no authority metadata is used to resolve the issuer.");
        }

        if (parameters.ValidAlgorithms is { } algorithms)
        {
            foreach (var algorithm in algorithms)
            {
                if (string.Equals(algorithm, "none", StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add("The algorithm 'none' must not be allowed in ValidAlgorithms.");
                }
            }
        }
    }
}
