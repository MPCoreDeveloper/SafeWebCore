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
                failures.Add("Path policy prefix must not be null, empty, or whitespace. Fix: use a concrete path such as '/api' or '/admin'.");
                continue;
            }

            var normalizedPrefix = NormalizePathPrefix(pathPolicy.PathPrefix);
            if (!seenPrefixes.Add(normalizedPrefix))
            {
                failures.Add($"Duplicate path policy prefix '{normalizedPrefix}' is not allowed. Path prefixes are normalized, so values like '/api' and 'api' collide.");
            }

            ValidatePolicy(pathPolicy.Options, $"Path policy '{normalizedPrefix}'", failures);
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidatePolicy(NetSecureHeadersOptions options, string scope, List<string> failures)
    {
        ValidateCspConfiguration(options, scope, failures);
        ValidateNelConfiguration(options, scope, failures);
        ValidateAdditionalHeaders(options, scope, failures);
        ValidateReportingEndpoints(options, scope, failures);
    }

    private static void ValidateCspConfiguration(NetSecureHeadersOptions options, string scope, List<string> failures)
    {
        if (!options.EnableCsp && options.UseCspReportOnly)
        {
            failures.Add($"{scope}: UseCspReportOnly requires EnableCsp to be true. Fix: set EnableCsp = true, or set UseCspReportOnly = false.");
        }

        if (options.EnableCsp && options.Csp is null)
        {
            failures.Add($"{scope}: Csp configuration must not be null when EnableCsp is true. Fix: assign a valid CspOptions instance, or set EnableCsp = false.");
        }

        // ReportTo consistency: if Csp.ReportTo is set, a matching ReportingEndpoint should exist at the options level.
        // Note: We validate against the top-level ReportingEndpoints for simplicity (path policies may override).
        if (options.EnableCsp && options.Csp is not null && !string.IsNullOrWhiteSpace(options.Csp.ReportTo))
        {
            var reportToGroup = options.Csp.ReportTo.Trim();
            var hasMatchingEndpoint = options.ReportingEndpoints.Any(e =>
                string.Equals(e.Group, reportToGroup, StringComparison.OrdinalIgnoreCase));

            if (!hasMatchingEndpoint)
            {
                failures.Add($"{scope}: Csp.ReportTo references group '{reportToGroup}', but no ReportingEndpoints entry with that Group exists. Fix: add ReportingEndpoints.Add(new() {{ Group = \"{reportToGroup}\", Url = \"https://reports.example.com/csp\" }}); or clear Csp.ReportTo.");
            }
        }
    }

    private static void ValidateAdditionalHeaders(NetSecureHeadersOptions options, string scope, List<string> failures)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in options.AdditionalHeaders)
        {
            if (string.IsNullOrWhiteSpace(header.Name))
            {
                failures.Add($"{scope}: Additional header name must not be null, empty, or whitespace. Fix: provide the exact header name, e.g. \"Document-Policy\".");
                continue;
            }

            if (string.IsNullOrWhiteSpace(header.Value))
            {
                failures.Add($"{scope}: Additional header '{header.Name}' value must not be null, empty, or whitespace. Fix: provide the value to emit, e.g. \"force-load-at-top\".");
            }

            if (!seen.Add(header.Name))
            {
                failures.Add($"{scope}: Duplicate additional header '{header.Name}' is not allowed. Fix: merge the values or keep only one entry per header name.");
            }
        }
    }

    private static void ValidateReportingEndpoints(NetSecureHeadersOptions options, string scope, List<string> failures)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var endpoint in options.ReportingEndpoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Group))
            {
                failures.Add($"{scope}: Reporting endpoint group must not be null, empty, or whitespace. Fix: use a stable group name such as 'default'.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(endpoint.Url))
            {
                failures.Add($"{scope}: Reporting endpoint '{endpoint.Group}' URL must not be null, empty, or whitespace. Fix: provide an absolute HTTPS URL such as 'https://reports.example.com/csp'.");
            }
            else if (!Uri.TryCreate(endpoint.Url, UriKind.Absolute, out var uri))
            {
                failures.Add($"{scope}: Reporting endpoint '{endpoint.Group}' URL must be absolute. Fix: use a full URL such as 'https://reports.example.com/csp'.");
            }
            else if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                failures.Add($"{scope}: Reporting endpoint '{endpoint.Group}' URL must be absolute. Fix: use a full URL such as 'https://reports.example.com/csp'.");
            }

            if (!seen.Add(endpoint.Group))
            {
                failures.Add($"{scope}: Duplicate reporting endpoint group '{endpoint.Group}' is not allowed. Fix: keep a single entry per reporting group.");
            }
        }
    }

    private static void ValidateNelConfiguration(NetSecureHeadersOptions options, string scope, List<string> failures)
    {
        if (!options.EnableNel)
            return;

        if (string.IsNullOrWhiteSpace(options.NelValue))
        {
            failures.Add($"{scope}: EnableNel is true but NelValue is empty. Fix: set NelValue to a valid JSON object, e.g. {{\"report_to\":\"default\",\"max_age\":2592000}} or disable NEL with EnableNel = false.");
            return;
        }

        // Basic structural check for required fields in NEL JSON (report_to and max_age are recommended by spec)
        var trimmed = options.NelValue.Trim();
        if (!trimmed.StartsWith('{') || !trimmed.EndsWith('}'))
        {
            failures.Add($"{scope}: NelValue must be a JSON object. Fix: use a string like {{\"report_to\":\"default\",\"max_age\":2592000}}.");
            return;
        }

        if (!trimmed.Contains("\"report_to\"", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.Contains("'report_to'", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{scope}: NelValue is missing recommended 'report_to' field. Fix: include \"report_to\":\"<group>\" that matches a ReportingEndpoints entry, e.g. {{\"report_to\":\"default\",\"max_age\":2592000}}.");
        }

        if (!trimmed.Contains("\"max_age\"", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.Contains("'max_age'", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{scope}: NelValue is missing 'max_age' field. Fix: add \"max_age\":<seconds>, e.g. {{\"report_to\":\"default\",\"max_age\":2592000}}.");
        }
    }

    private static string NormalizePathPrefix(string pathPrefix)
        => pathPrefix.StartsWith('/') ? pathPrefix : $"/{pathPrefix}";
}
