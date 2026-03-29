using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
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
    private readonly NetSecureHeadersOptions _options = options.Value;

    // PERF: Pre-build the CSP template once — avoids StringBuilder work on every request
    private readonly string? _cspTemplate = options.Value.EnableCsp ? options.Value.Csp.Build() : null;

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

        // Generate per-request nonce and expose via HttpContext.Items
        var nonce = nonceService.GenerateNonce();
        context.Items[NetSecureHeaders.CspNonceKey] = nonce;

        // Set security headers before the response body is written
        AddSecurityHeaders(context.Response, nonce);

        // Remove Server header just before headers are flushed to the client
        if (_options.RemoveServerHeader)
        {
            context.Response.OnStarting(static state =>
            {
                ((HttpResponse)state).Headers.Remove(HeaderNames.Server);
                return Task.CompletedTask;
            }, context.Response);
        }

        await next(context);
    }

    private void AddSecurityHeaders(HttpResponse response, string nonce)
    {
        var headers = response.Headers;

        if (_options.EnableHsts)
            headers.Append(HeaderNames.StrictTransportSecurity, _options.HstsValue);

        if (_options.EnableXFrameOptions)
            headers.Append(HeaderNames.XFrameOptions, _options.XFrameOptionsValue);

        if (_options.EnableXContentTypeOptions)
            headers.Append(HeaderNames.XContentTypeOptions, _options.XContentTypeOptionsValue);

        if (_options.EnableReferrerPolicy)
            headers.Append(HeaderNames.ReferrerPolicy, _options.ReferrerPolicyValue);

        if (_options.EnablePermissionsPolicy)
            headers.Append(HeaderNames.PermissionsPolicy, _options.PermissionsPolicyValue);

        if (_options.EnableCoep)
            headers.Append(HeaderNames.CrossOriginEmbedderPolicy, _options.CoepValue);

        if (_options.EnableCoop)
            headers.Append(HeaderNames.CrossOriginOpenerPolicy, _options.CoopValue);

        if (_options.EnableCorp)
            headers.Append(HeaderNames.CrossOriginResourcePolicy, _options.CorpValue);

        if (_options.EnableXDnsPrefetchControl)
            headers.Append(HeaderNames.XDnsPrefetchControl, _options.XDnsPrefetchControlValue);

        if (_options.EnableXPermittedCrossDomainPolicies)
            headers.Append(HeaderNames.XPermittedCrossDomainPolicies, _options.XPermittedCrossDomainPoliciesValue);

        if (_cspTemplate is not null)
        {
            var cspValue = _cspTemplate.Replace("{nonce}", nonce, StringComparison.Ordinal);
            headers.Append(HeaderNames.ContentSecurityPolicy, cspValue);
        }

        // Apply custom policies
        foreach (var policy in _options.CustomPolicies)
        {
            policy.Apply(response);
        }
    }
}
