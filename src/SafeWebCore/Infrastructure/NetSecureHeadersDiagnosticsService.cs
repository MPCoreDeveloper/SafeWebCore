using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SafeWebCore.Metadata;
using SafeWebCore.Options;

namespace SafeWebCore.Infrastructure;

internal sealed class NetSecureHeadersDiagnosticsService(IOptions<NetSecureHeadersOptions> options) : INetSecureHeadersDiagnosticsService
{
    private readonly ResolvedPathPolicy _defaultPathPolicy = new(
        default,
        options.Value,
        options.Value.EnableCsp ? options.Value.Csp.Build() : null,
        PathPolicyResolver.BuildReportingEndpointsValue(options.Value.ReportingEndpoints));

    private readonly List<ResolvedPathPolicy> _pathPolicies = PathPolicyResolver.Build(options.Value.PathPolicies);

    public object CreateSnapshot(string? path = null, CspEndpointMode? endpointCspMode = null)
    {
        var normalizedPath = NormalizePath(path);
        var (effectiveOptions, matchedPathPolicy, cspTemplate, reportingEndpointsValue) =
            PathPolicyResolver.ResolveFor(_pathPolicies, new PathString(normalizedPath), _defaultPathPolicy);
        var headers = BuildHeaders(effectiveOptions, cspTemplate, reportingEndpointsValue, endpointCspMode);
        var warnings = BuildWarnings(effectiveOptions, matchedPathPolicy, endpointCspMode);

        return new NetSecureHeadersDiagnosticsSnapshot(
            normalizedPath,
            matchedPathPolicy,
            matchedPathPolicy is null,
            endpointCspMode?.ToString() ?? "Default",
            effectiveOptions.UseCspReportOnly,
            effectiveOptions.RemoveServerHeader,
            effectiveOptions.RemoveXPoweredBy,
            _pathPolicies.Select(policy => policy.Prefix.Value!).ToArray(),
            headers,
            warnings);
    }

    private static Dictionary<string, string> BuildHeaders(
        NetSecureHeadersOptions options,
        string? cspTemplate,
        string? reportingEndpointsValue,
        CspEndpointMode? endpointCspMode)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddStandardHeader(headers, options.EnableHsts, HeaderNames.StrictTransportSecurity, options.HstsValue);
        AddStandardHeader(headers, options.EnableXFrameOptions, HeaderNames.XFrameOptions, options.XFrameOptionsValue);
        AddStandardHeader(headers, options.EnableXContentTypeOptions, HeaderNames.XContentTypeOptions, options.XContentTypeOptionsValue);
        AddStandardHeader(headers, options.EnableReferrerPolicy, HeaderNames.ReferrerPolicy, options.ReferrerPolicyValue);
        AddStandardHeader(headers, options.EnablePermissionsPolicy, HeaderNames.PermissionsPolicy, options.PermissionsPolicyValue);
        AddStandardHeader(headers, options.EnableCoep, HeaderNames.CrossOriginEmbedderPolicy, options.CoepValue);
        AddStandardHeader(headers, options.EnableCoop, HeaderNames.CrossOriginOpenerPolicy, options.CoopValue);
        AddStandardHeader(headers, options.EnableCorp, HeaderNames.CrossOriginResourcePolicy, options.CorpValue);
        AddStandardHeader(headers, options.EnableXDnsPrefetchControl, HeaderNames.XDnsPrefetchControl, options.XDnsPrefetchControlValue);
        AddStandardHeader(headers, options.EnableXPermittedCrossDomainPolicies, HeaderNames.XPermittedCrossDomainPolicies, options.XPermittedCrossDomainPoliciesValue);
        AddStandardHeader(headers, options.EnableOriginAgentCluster, HeaderNames.OriginAgentCluster, options.OriginAgentClusterValue);
        AddStandardHeader(headers, options.EnableXRobotsTag, HeaderNames.XRobotsTag, options.XRobotsTagValue);
        AddStandardHeader(headers, options.EnableClearSiteData, HeaderNames.ClearSiteData, options.ClearSiteDataValue);

        if (options.EnableNel && !string.IsNullOrWhiteSpace(options.NelValue))
            headers[HeaderNames.NetworkErrorLogging] = options.NelValue;

        if (!string.IsNullOrWhiteSpace(reportingEndpointsValue))
            headers[HeaderNames.ReportingEndpoints] = reportingEndpointsValue;

        AddCspHeader(headers, cspTemplate, options, endpointCspMode);

        foreach (var additionalHeader in options.AdditionalHeaders)
        {
            headers[additionalHeader.Name] = additionalHeader.Value;
        }

        if (options.RemoveServerHeader)
            headers[HeaderNames.Server] = "(removed on response start)";

        if (options.RemoveXPoweredBy)
            headers[HeaderNames.XPoweredBy] = "(removed on response start)";

        return headers;
    }

    private static void AddStandardHeader(Dictionary<string, string> headers, bool enabled, string name, string value)
    {
        if (enabled)
            headers[name] = value;
    }

    private static void AddCspHeader(Dictionary<string, string> headers, string? cspTemplate, NetSecureHeadersOptions options, CspEndpointMode? endpointCspMode)
    {
        if (cspTemplate is null)
            return;

        var cspHeaderName = endpointCspMode switch
        {
            CspEndpointMode.ReportOnly => HeaderNames.ContentSecurityPolicyReportOnly,
            CspEndpointMode.Enforce => HeaderNames.ContentSecurityPolicy,
            _ => options.UseCspReportOnly ? HeaderNames.ContentSecurityPolicyReportOnly : HeaderNames.ContentSecurityPolicy
        };

        headers[cspHeaderName] = cspTemplate;
    }

    private static string[] BuildWarnings(NetSecureHeadersOptions options, string? matchedPathPolicy, CspEndpointMode? endpointCspMode)
    {
        var warnings = new List<string>(4);

        if (!string.IsNullOrWhiteSpace(matchedPathPolicy))
        {
            warnings.Add($"Path policy '{matchedPathPolicy}' matched this preview request. Longest-prefix wins when multiple prefixes overlap.");
        }

        if (options.UseCspReportOnly || endpointCspMode is CspEndpointMode.ReportOnly)
        {
            warnings.Add("CSP is previewed in report-only mode. Violations will be reported but not blocked.");
        }

        if (options.RemoveServerHeader || options.RemoveXPoweredBy)
        {
            warnings.Add("Server-identifying headers are removed via OnStarting. Upstream hosts, reverse proxies, or IIS modules may still need host-level configuration.");
        }

        if (endpointCspMode is CspEndpointMode.Enforce)
        {
            warnings.Add("Endpoint-level CSP mode override is forcing enforcement for this preview.");
        }

        return [.. warnings];
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        return path.StartsWith('/')
            ? path
            : $"/{path}";
    }

    private sealed record NetSecureHeadersDiagnosticsSnapshot(
        string Path,
        string? MatchedPathPolicy,
        bool UsesGlobalPolicy,
        string EffectiveCspModeOverride,
        bool UseCspReportOnly,
        bool RemoveServerHeader,
        bool RemoveXPoweredBy,
        string[] ConfiguredPathPolicies,
        IReadOnlyDictionary<string, string> Headers,
        string[] Warnings);
}
