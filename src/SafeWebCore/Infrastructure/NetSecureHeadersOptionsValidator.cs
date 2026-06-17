using Microsoft.Extensions.Options;
using SafeWebCore.Options;

namespace SafeWebCore.Infrastructure;

internal sealed class NetSecureHeadersOptionsValidator : IValidateOptions<NetSecureHeadersOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, NetSecureHeadersOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];
        ValidatePolicy(options, "Global policy", failures);

        var seenPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pathPolicy in options.PathPolicies)
        {
            if (string.IsNullOrWhiteSpace(pathPolicy.PathPrefix))
            {
                failures.Add("Path policy prefix must not be null, empty, or whitespace.");
                continue;
            }

            var normalizedPrefix = NormalizePathPrefix(pathPolicy.PathPrefix);
            if (!seenPrefixes.Add(normalizedPrefix))
            {
                failures.Add($"Duplicate path policy prefix '{normalizedPrefix}' is not allowed.");
            }

            ValidatePolicy(pathPolicy.Options, $"Path policy '{normalizedPrefix}'", failures);
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidatePolicy(NetSecureHeadersOptions options, string scope, List<string> failures)
    {
        if (!options.EnableCsp && options.UseCspReportOnly)
        {
            failures.Add($"{scope}: UseCspReportOnly requires EnableCsp to be true.");
        }

        if (options.EnableCsp && options.Csp is null)
        {
            failures.Add($"{scope}: Csp configuration must not be null when EnableCsp is true.");
        }

        var seenAdditionalHeaderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var additionalHeader in options.AdditionalHeaders)
        {
            if (string.IsNullOrWhiteSpace(additionalHeader.Name))
            {
                failures.Add($"{scope}: Additional header name must not be null, empty, or whitespace.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(additionalHeader.Value))
            {
                failures.Add($"{scope}: Additional header '{additionalHeader.Name}' value must not be null, empty, or whitespace.");
            }

            if (!seenAdditionalHeaderNames.Add(additionalHeader.Name))
            {
                failures.Add($"{scope}: Duplicate additional header '{additionalHeader.Name}' is not allowed.");
            }
        }

        var seenReportingEndpointGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var endpoint in options.ReportingEndpoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Group))
            {
                failures.Add($"{scope}: Reporting endpoint group must not be null, empty, or whitespace.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(endpoint.Url))
            {
                failures.Add($"{scope}: Reporting endpoint '{endpoint.Group}' URL must not be null, empty, or whitespace.");
            }
            else if (!Uri.TryCreate(endpoint.Url, UriKind.Absolute, out _))
            {
                failures.Add($"{scope}: Reporting endpoint '{endpoint.Group}' URL must be absolute.");
            }

            if (!seenReportingEndpointGroups.Add(endpoint.Group))
            {
                failures.Add($"{scope}: Duplicate reporting endpoint group '{endpoint.Group}' is not allowed.");
            }
        }
    }

    private static string NormalizePathPrefix(string pathPrefix)
        => pathPrefix.StartsWith('/') ? pathPrefix : $"/{pathPrefix}";
}
