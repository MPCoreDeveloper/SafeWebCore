using Microsoft.AspNetCore.Builder;
using SafeWebCore.Infrastructure;
using SafeWebCore.Middleware;

namespace SafeWebCore.Extensions;

/// <summary>
/// Extension methods for configuring NetSecureHeaders middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the NetSecureHeaders middleware to the application pipeline.
    /// This middleware adds security headers to HTTP responses.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseNetSecureHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<NetSecureHeadersMiddleware>();
    }

    /// <summary>
    /// Adds the CSP report middleware to the application pipeline.
    /// This middleware handles CSP violation reports at /csp-report.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseCspReport(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<CspReportMiddleware>();
    }
}
