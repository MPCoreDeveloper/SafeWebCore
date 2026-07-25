using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SafeWebCore.Abstractions;
using SafeWebCore.Attributes;
using SafeWebCore.Infrastructure;
using SafeWebCore.Metadata;
using SafeWebCore.Options;

namespace SafeWebCore.Middleware;

/// <summary>
/// Middleware that adds security headers to every HTTP response.
/// Generates a per-request CSP nonce and stores it in <see cref="HttpContext.Items"/>
/// under <see cref="NetSecureHeaders.CspNonceKey"/>.
/// </summary>
public sealed class NetSecureHeadersMiddleware : IMiddleware
{
    private sealed record ResolvedPathPolicy(
        PathString Prefix,
        NetSecureHeadersOptions Options,
        string? CspTemplate,
        string? ReportingEndpointsValue);

    private readonly INonceService _nonceService;
    private readonly NetSecureHeadersOptions _defaultOptions;
    private readonly string? _defaultCspTemplate;
    private readonly string? _defaultReportingEndpointsValue;
    private readonly List<ResolvedPathPolicy> _pathPolicies;
    private readonly SecurityEventDispatcher _eventDispatcher;
    private readonly SafeWebCoreMetrics _metrics;

    /// <summary>
    /// Backward-compatible constructor that creates the middleware
    /// with only the originally shipped dependencies.
    /// </summary>
    /// <param name="nonceService">The nonce service for generating CSP nonces.</param>
    /// <param name="options">The options for configuring security headers.</param>
    /// <param name="eventDispatcher">Dispatcher for security telemetry events.</param>
    public NetSecureHeadersMiddleware(
        INonceService nonceService,
        IOptions<NetSecureHeadersOptions> options,
        SecurityEventDispatcher eventDispatcher)
        : this(nonceService, options, eventDispatcher, null)
    {
    }

    /// <summary>
    /// Creates the middleware with optional observability integrations.
    /// </summary>
    /// <param name="nonceService">The nonce service for generating CSP nonces.</param>
    /// <param name="options">The options for configuring security headers.</param>
    /// <param name="eventDispatcher">Dispatcher for security telemetry events.</param>
    /// <param name="metrics">Optional metrics instance for opt-in counters.</param>
    public NetSecureHeadersMiddleware(
        INonceService nonceService,
        IOptions<NetSecureHeadersOptions> options,
        SecurityEventDispatcher eventDispatcher,
        SafeWebCoreMetrics? metrics)
    {
        ArgumentNullException.ThrowIfNull(nonceService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(eventDispatcher);

        _nonceService = nonceService;
        _defaultOptions = options.Value;
        _defaultCspTemplate = options.Value.EnableCsp ? options.Value.Csp.Build() : null;
        _defaultReportingEndpointsValue = BuildReportingEndpointsValue(options.Value.ReportingEndpoints);
        _pathPolicies = BuildPathPolicies(options.Value.PathPolicies);
        _eventDispatcher = eventDispatcher;
        _metrics = metrics ?? new SafeWebCoreMetrics();
    }

    /// <summary>
    /// Invokes the middleware to add security headers to the response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<SkipNetSecureHeadersAttribute>() is not null)
        {
            await next(context);
            return;
        }

        var endpointCspMode = endpoint?.Metadata.GetMetadata<CspModeAttribute>()?.Mode;
        var (effectiveOptions, matchedPathPolicy, cspTemplate, reportingEndpointsValue) = ResolvePolicy(context.Request.Path);

        // Generate per-request nonce and expose via HttpContext.Items
        var nonce = _nonceService.GenerateNonce();
        context.Items[NetSecureHeaders.CspNonceKey] = nonce;

        // Set security headers before the response body is written
        AddSecurityHeaders(context.Response, nonce, effectiveOptions, cspTemplate, reportingEndpointsValue, endpointCspMode);

        // Emit additive telemetry events + metrics (opt-in consumption)
        _ = _eventDispatcher.EmitAsync(new SecurityEvent
        {
            EventType = SecurityEventType.HeadersApplied,
            Path = context.Request.Path,
            Properties = new Dictionary<string, object?>
            {
                ["HasCsp"] = effectiveOptions.EnableCsp,
                ["UseReportOnly"] = effectiveOptions.UseCspReportOnly || endpointCspMode == CspEndpointMode.ReportOnly,
                ["MatchedPathPolicy"] = matchedPathPolicy
            }
        }, context.RequestAborted);

        _metrics.HeadersApplied.Add(1);

        if (!string.IsNullOrEmpty(matchedPathPolicy))
        {
            _ = _eventDispatcher.EmitAsync(new SecurityEvent
            {
                EventType = SecurityEventType.PathPolicyMatched,
                Path = context.Request.Path,
                Properties = new Dictionary<string, object?>
                {
                    ["MatchedPathPolicy"] = matchedPathPolicy
                }
            }, context.RequestAborted);

            _metrics.PathPolicyMatches.Add(1);
        }

        // Remove Server and/or X-Powered-By just before headers are flushed to the client.
        // Using OnStarting to ensure we catch headers added by later components (e.g. Kestrel, IIS modules).
        if (effectiveOptions.RemoveServerHeader || effectiveOptions.RemoveXPoweredBy)
        {
            var removeServer = effectiveOptions.RemoveServerHeader;
            var removeXpb = effectiveOptions.RemoveXPoweredBy;
            context.Response.OnStarting(static state =>
            {
                var (srv, xpb, resp) = ((bool, bool, HttpResponse))state;
                if (srv) resp.Headers.Remove(HeaderNames.Server);
                if (xpb) resp.Headers.Remove(HeaderNames.XPoweredBy);
                return Task.CompletedTask;
            }, (removeServer, removeXpb, context.Response));
        }

        await next(context);
    }

    private static List<ResolvedPathPolicy> BuildPathPolicies(List<PathPolicyOptions> configuredPolicies)
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

    private (NetSecureHeadersOptions Options, string? MatchedPathPolicy, string? CspTemplate, string? ReportingEndpointsValue) ResolvePolicy(PathString requestPath)
    {
        foreach (var policy in _pathPolicies)
        {
            if (!requestPath.StartsWithSegments(policy.Prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            return (policy.Options, policy.Prefix.Value, policy.CspTemplate, policy.ReportingEndpointsValue);
        }

        return (_defaultOptions, null, _defaultCspTemplate, _defaultReportingEndpointsValue);
    }

    private static string? BuildReportingEndpointsValue(List<ReportingEndpointOptions> endpoints)
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

    private static void AddSecurityHeaders(
        HttpResponse response,
        string nonce,
        NetSecureHeadersOptions options,
        string? cspTemplate,
        string? reportingEndpointsValue,
        CspEndpointMode? endpointCspMode)
    {
        var headers = response.Headers;

        AddIfEnabled(headers, options.EnableHsts, HeaderNames.StrictTransportSecurity, options.HstsValue);
        AddIfEnabled(headers, options.EnableXFrameOptions, HeaderNames.XFrameOptions, options.XFrameOptionsValue);
        AddIfEnabled(headers, options.EnableXContentTypeOptions, HeaderNames.XContentTypeOptions, options.XContentTypeOptionsValue);
        AddIfEnabled(headers, options.EnableReferrerPolicy, HeaderNames.ReferrerPolicy, options.ReferrerPolicyValue);
        AddIfEnabled(headers, options.EnablePermissionsPolicy, HeaderNames.PermissionsPolicy, options.PermissionsPolicyValue);
        AddIfEnabled(headers, options.EnableCoep, HeaderNames.CrossOriginEmbedderPolicy, options.CoepValue);
        AddIfEnabled(headers, options.EnableCoop, HeaderNames.CrossOriginOpenerPolicy, options.CoopValue);
        AddIfEnabled(headers, options.EnableCorp, HeaderNames.CrossOriginResourcePolicy, options.CorpValue);
        AddIfEnabled(headers, options.EnableXDnsPrefetchControl, HeaderNames.XDnsPrefetchControl, options.XDnsPrefetchControlValue);
        AddIfEnabled(headers, options.EnableXPermittedCrossDomainPolicies, HeaderNames.XPermittedCrossDomainPolicies, options.XPermittedCrossDomainPoliciesValue);
        AddIfEnabled(headers, options.EnableOriginAgentCluster, HeaderNames.OriginAgentCluster, options.OriginAgentClusterValue);
        AddIfEnabled(headers, options.EnableXRobotsTag, HeaderNames.XRobotsTag, options.XRobotsTagValue);
        AddIfEnabled(headers, options.EnableClearSiteData, HeaderNames.ClearSiteData, options.ClearSiteDataValue);

        if (options.EnableNel && !string.IsNullOrWhiteSpace(options.NelValue))
            headers.Append(HeaderNames.NetworkErrorLogging, options.NelValue);

        if (!string.IsNullOrWhiteSpace(reportingEndpointsValue))
            headers.Append(HeaderNames.ReportingEndpoints, reportingEndpointsValue);

        if (cspTemplate is not null)
        {
            var cspValue = cspTemplate.Replace("{nonce}", nonce, StringComparison.Ordinal);
            var useReportOnly = endpointCspMode switch
            {
                CspEndpointMode.ReportOnly => true,
                CspEndpointMode.Enforce => false,
                _ => options.UseCspReportOnly
            };

            var cspHeaderName = useReportOnly
                ? HeaderNames.ContentSecurityPolicyReportOnly
                : HeaderNames.ContentSecurityPolicy;

            headers.Append(cspHeaderName, cspValue);
        }

        foreach (var additionalHeader in options.AdditionalHeaders)
        {
            headers[additionalHeader.Name] = additionalHeader.Value;
        }

        foreach (var policy in options.CustomPolicies)
        {
            policy.Apply(response);
        }
    }

    private static void AddIfEnabled(Microsoft.AspNetCore.Http.IHeaderDictionary headers, bool enabled, string name, string value)
    {
        if (enabled)
            headers.Append(name, value);
    }
}
