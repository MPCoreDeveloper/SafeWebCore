using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SafeWebCore.Attributes;
using SafeWebCore.Metadata;
using SafeWebCore.Options;

namespace SafeWebCore.Middleware;

/// <summary>
/// Middleware that adds security headers to every HTTP response.
/// Generates a per-request CSP nonce and stores it in <see cref="HttpContext.Items"/>
/// under <see cref="NetSecureHeaders.CspNonceKey"/>.
/// </summary>
/// <param name="nonceService">The nonce service for generating CSP nonces.</param>
/// <param name="options">The options for configuring security headers.</param>
public sealed class NetSecureHeadersMiddleware(
    INonceService nonceService,
    IOptions<NetSecureHeadersOptions> options) : IMiddleware
{
    private sealed record ResolvedPathPolicy(PathString Prefix, NetSecureHeadersOptions Options, string? CspTemplate);

    private readonly NetSecureHeadersOptions _defaultOptions = options.Value;

    // PERF: Pre-build the CSP template once — avoids StringBuilder work on every request
    private readonly string? _defaultCspTemplate = options.Value.EnableCsp ? options.Value.Csp.Build() : null;

    private readonly List<ResolvedPathPolicy> _pathPolicies = BuildPathPolicies(options.Value.PathPolicies);

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
        var (effectiveOptions, cspTemplate) = ResolvePolicy(context.Request.Path);

        // Generate per-request nonce and expose via HttpContext.Items
        var nonce = nonceService.GenerateNonce();
        context.Items[NetSecureHeaders.CspNonceKey] = nonce;

        // Set security headers before the response body is written
        AddSecurityHeaders(context.Response, nonce, effectiveOptions, cspTemplate, endpointCspMode);

        // Remove Server header just before headers are flushed to the client
        if (effectiveOptions.RemoveServerHeader)
        {
            context.Response.OnStarting(static state =>
            {
                ((HttpResponse)state).Headers.Remove(HeaderNames.Server);
                return Task.CompletedTask;
            }, context.Response);
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

            resolvedPolicies.Add(new ResolvedPathPolicy(new PathString(normalizedPrefix), policy.Options, cspTemplate));
        }

        resolvedPolicies.Sort(static (a, b) =>
            b.Prefix.Value!.Length.CompareTo(a.Prefix.Value!.Length));

        return resolvedPolicies;
    }

    private (NetSecureHeadersOptions Options, string? CspTemplate) ResolvePolicy(PathString requestPath)
    {
        var matchedPolicy = _pathPolicies
            .FirstOrDefault(policy => requestPath.StartsWithSegments(policy.Prefix, StringComparison.OrdinalIgnoreCase));

        if (matchedPolicy is not null)
            return (matchedPolicy.Options, matchedPolicy.CspTemplate);

        return (_defaultOptions, _defaultCspTemplate);
    }

    private static void AddSecurityHeaders(
        HttpResponse response,
        string nonce,
        NetSecureHeadersOptions options,
        string? cspTemplate,
        CspEndpointMode? endpointCspMode)
    {
        var headers = response.Headers;

        if (options.EnableHsts)
            headers.Append(HeaderNames.StrictTransportSecurity, options.HstsValue);

        if (options.EnableXFrameOptions)
            headers.Append(HeaderNames.XFrameOptions, options.XFrameOptionsValue);

        if (options.EnableXContentTypeOptions)
            headers.Append(HeaderNames.XContentTypeOptions, options.XContentTypeOptionsValue);

        if (options.EnableReferrerPolicy)
            headers.Append(HeaderNames.ReferrerPolicy, options.ReferrerPolicyValue);

        if (options.EnablePermissionsPolicy)
            headers.Append(HeaderNames.PermissionsPolicy, options.PermissionsPolicyValue);

        if (options.EnableCoep)
            headers.Append(HeaderNames.CrossOriginEmbedderPolicy, options.CoepValue);

        if (options.EnableCoop)
            headers.Append(HeaderNames.CrossOriginOpenerPolicy, options.CoopValue);

        if (options.EnableCorp)
            headers.Append(HeaderNames.CrossOriginResourcePolicy, options.CorpValue);

        if (options.EnableXDnsPrefetchControl)
            headers.Append(HeaderNames.XDnsPrefetchControl, options.XDnsPrefetchControlValue);

        if (options.EnableXPermittedCrossDomainPolicies)
            headers.Append(HeaderNames.XPermittedCrossDomainPolicies, options.XPermittedCrossDomainPoliciesValue);

        if (options.EnableOriginAgentCluster)
            headers.Append(HeaderNames.OriginAgentCluster, options.OriginAgentClusterValue);

        if (options.EnableXRobotsTag)
            headers.Append(HeaderNames.XRobotsTag, options.XRobotsTagValue);

        if (options.EnableClearSiteData)
            headers.Append(HeaderNames.ClearSiteData, options.ClearSiteDataValue);

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

        // Apply custom policies
        foreach (var policy in options.CustomPolicies)
        {
            policy.Apply(response);
        }
    }
}
