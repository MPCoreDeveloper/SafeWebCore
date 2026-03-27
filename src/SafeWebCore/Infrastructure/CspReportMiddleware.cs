using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SafeWebCore.Infrastructure;

/// <summary>
/// Middleware to handle CSP violation reports.
/// </summary>
public sealed partial class CspReportMiddleware(ILogger<CspReportMiddleware> logger) : IMiddleware
{
    /// <summary>
    /// Invokes the middleware to handle CSP reports.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.Request.Path == "/csp-report" && context.Request.Method == "POST")
        {
            using var reader = new StreamReader(context.Request.Body);
            var report = await reader.ReadToEndAsync();

            LogCspViolation(logger, report);

            context.Response.StatusCode = StatusCodes.Status200OK;
            return;
        }

        await next(context);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "CSP Violation Report: {Report}")]
    private static partial void LogCspViolation(ILogger logger, string report);
}
