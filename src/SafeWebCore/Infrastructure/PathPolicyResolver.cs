using System.Text;
using Microsoft.AspNetCore.Http;
using SafeWebCore.Options;

namespace SafeWebCore.Infrastructure;

/// <summary>
/// A configured path policy in the form the middleware and the diagnostics preview both need: the
/// normalized prefix, the effective options, the pre-built CSP template and the
/// Reporting-Endpoints header value.
/// </summary>
/// <param name="Prefix">The normalized (leading slash) path prefix.</param>
/// <param name="Options">The effective options for requests matching this prefix.</param>
/// <param name="CspTemplate">The built CSP template, or <see langword="null"/> when CSP is disabled.</param>
/// <param name="ReportingEndpointsValue">The Reporting-Endpoints header value, or <see langword="null"/> when there are none.</param>
internal sealed record ResolvedPathPolicy(
    PathString Prefix,
    NetSecureHeadersOptions Options,
    string? CspTemplate,
    string? ReportingEndpointsValue);

/// <summary>
/// Resolves configured path policies into the pre-computed form used per request and builds the
/// Reporting-Endpoints header value.
/// </summary>
/// <remarks>
/// <see cref="Middleware.NetSecureHeadersMiddleware"/> and
/// <see cref="NetSecureHeadersDiagnosticsService"/> must resolve exactly the same policy for a
/// request, otherwise the diagnostics preview would describe a configuration that is not applied.
/// Both call this type instead of keeping a copy.
/// </remarks>
internal static class PathPolicyResolver
{
    /// <summary>
    /// Normalizes and sorts the configured path policies; the longest prefix wins on a match.
    /// </summary>
    /// <param name="configuredPolicies">The configured path policies.</param>
    public static List<ResolvedPathPolicy> Build(List<PathPolicyOptions> configuredPolicies)
    {
        if (configuredPolicies.Count == 0)
            return [];

        var resolvedPolicies = new List<ResolvedPathPolicy>(configuredPolicies.Count);

        foreach (var policy in configuredPolicies)
        {
            if (string.IsNullOrWhiteSpace(policy.PathPrefix))
                continue;

            var normalizedPrefix = policy.PathPrefix.StartsWith('/')
                ? policy.PathPrefix
                : $"/{policy.PathPrefix}";

            var cspTemplate = policy.Options.EnableCsp
                ? policy.Options.Csp.Build()
                : null;

            var reportingEndpointsValue = BuildReportingEndpointsValue(policy.Options.ReportingEndpoints);

            resolvedPolicies.Add(new ResolvedPathPolicy(
                new PathString(normalizedPrefix),
                policy.Options,
                cspTemplate,
                reportingEndpointsValue));
        }

        resolvedPolicies.Sort(static (a, b) =>
            b.Prefix.Value!.Length.CompareTo(a.Prefix.Value!.Length));

        return resolvedPolicies;
    }

    /// <summary>
    /// Resolves the policy that applies to <paramref name="requestPath"/> and flattens it into the
    /// tuple <see cref="Middleware.NetSecureHeadersMiddleware"/> and
    /// <see cref="NetSecureHeadersDiagnosticsService"/> both consume.
    /// </summary>
    /// <param name="pathPolicies">The resolved policies, longest prefix first.</param>
    /// <param name="requestPath">The request path to match.</param>
    /// <param name="defaultPolicy">The policy applied when no configured prefix matches.</param>
    /// <returns>
    /// The effective options, the matched path prefix (<see langword="null"/> for the default policy),
    /// the CSP template and the Reporting-Endpoints header value.
    /// </returns>
    public static (NetSecureHeadersOptions Options, string? MatchedPathPolicy, string? CspTemplate, string? ReportingEndpointsValue) ResolveFor(
        List<ResolvedPathPolicy> pathPolicies,
        PathString requestPath,
        ResolvedPathPolicy defaultPolicy)
    {
        var policy = Resolve(pathPolicies, requestPath, defaultPolicy);
        return (policy.Options, policy.Prefix.Value, policy.CspTemplate, policy.ReportingEndpointsValue);
    }

    /// <summary>
    /// Returns the policy that applies to <paramref name="requestPath"/>: the first (longest) matching
    /// configured prefix, or <paramref name="defaultPolicy"/> when none matches.
    /// </summary>
    /// <param name="pathPolicies">The resolved policies, longest prefix first.</param>
    /// <param name="requestPath">The request path to match.</param>
    /// <param name="defaultPolicy">The policy applied when no configured prefix matches.</param>
    private static ResolvedPathPolicy Resolve(
        List<ResolvedPathPolicy> pathPolicies,
        PathString requestPath,
        ResolvedPathPolicy defaultPolicy)
    {
        foreach (var policy in pathPolicies)
        {
            if (requestPath.StartsWithSegments(policy.Prefix, StringComparison.OrdinalIgnoreCase))
                return policy;
        }

        return defaultPolicy;
    }

    /// <summary>
    /// Builds the Reporting-Endpoints header value (`group="url"` pairs) for the configured
    /// endpoints.
    /// </summary>
    /// <param name="endpoints">The configured reporting endpoints.</param>
    /// <returns>The header value, or <see langword="null"/> when nothing is configured.</returns>
    public static string? BuildReportingEndpointsValue(List<ReportingEndpointOptions> endpoints)
    {
        if (endpoints.Count == 0)
            return null;

        var builder = new StringBuilder(endpoints.Count * 48);

        for (var index = 0; index < endpoints.Count; index++)
        {
            if (index > 0)
                builder.Append(", ");

            var endpoint = endpoints[index];
            builder.Append(endpoint.Group).Append("=\"").Append(endpoint.Url).Append('"');
        }

        return builder.ToString();
    }
}
