using Microsoft.Extensions.Options;
using SafeWebCore.Options;

namespace SafeWebCore.Infrastructure;

internal sealed class NetSecureHeadersOptionsValidator : IValidateOptions<NetSecureHeadersOptions>
{
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
    }

    private static string NormalizePathPrefix(string pathPrefix)
        => pathPrefix.StartsWith('/') ? pathPrefix : $"/{pathPrefix}";
}
