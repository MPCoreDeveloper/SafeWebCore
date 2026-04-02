using Microsoft.AspNetCore.Mvc;
using SafeWebCore.Attributes;
using SafeWebCore.Metadata;

namespace SafeWebCore.Examples.ApiService.Controllers;

/// <summary>
/// Admin controller demonstrating endpoint-level header overrides.
/// </summary>
[ApiController]
[Route("admin")]
public sealed class AdminController : ControllerBase
{
    /// <summary>
    /// Health probe endpoint — security headers are skipped entirely.
    /// Use [SkipNetSecureHeaders] for health checks, readiness probes, or
    /// any endpoint consumed by infrastructure that does not need headers.
    /// </summary>
    [HttpGet("health")]
    [SkipNetSecureHeaders]
    public IActionResult Health() => Ok(new { status = "healthy", time = DateTimeOffset.UtcNow });

    /// <summary>
    /// Returns current application metrics.
    /// This endpoint overrides CSP to report-only mode so a tighter policy
    /// can be tested before enforcement without breaking the dashboard UI.
    /// </summary>
    [HttpGet("metrics")]
    [CspMode(CspEndpointMode.ReportOnly)]
    public IActionResult Metrics() =>
        Ok(new
        {
            requestsPerSecond = 42,
            p99LatencyMs = 18,
            errorRate = 0.001,
        });

    /// <summary>
    /// Returns system configuration (internal use only).
    /// Lives under /internal prefix — matched by the path policy configured
    /// in Program.cs, which applies a tailored set of headers.
    /// </summary>
    [HttpGet("/internal/config")]
    public IActionResult Config() =>
        Ok(new
        {
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            version = "1.0.0",
        });
}
