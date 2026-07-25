using System.Diagnostics.Metrics;

namespace SafeWebCore.Infrastructure;

/// <summary>
/// Provides opt-in metrics for SafeWebCore using System.Diagnostics.Metrics.
/// 
/// Meters and instruments are created when SafeWebCore services are registered.
/// Consumption is opt-in via OpenTelemetry, Prometheus, or other metric exporters.
/// No metrics are emitted to external systems unless a consumer is configured.
/// </summary>
public sealed class SafeWebCoreMetrics
{
    /// <summary>The canonical meter name used for SafeWebCore metrics.</summary>
    public const string MeterName = "SafeWebCore";

    /// <summary>The underlying Meter for advanced scenarios (e.g. custom instruments).</summary>
    public Meter Meter { get; }

    /// <summary>Counter for total security header applications.</summary>
    public Counter<long> HeadersApplied { get; }

    /// <summary>Counter for total CSP violation reports processed.</summary>
    public Counter<long> CspViolations { get; }

    /// <summary>Counter for total path-specific policy matches.</summary>
    public Counter<long> PathPolicyMatches { get; }

    /// <summary>
    /// Creates the SafeWebCore metrics instruments.
    /// </summary>
    public SafeWebCoreMetrics()
    {
        Meter = new Meter(MeterName, "1.0");

        HeadersApplied = Meter.CreateCounter<long>(
            "safewebcore.headers_applied_total",
            unit: "{headers}",
            description: "Total number of times security headers were applied to responses.");

        CspViolations = Meter.CreateCounter<long>(
            "safewebcore.csp_violations_total",
            unit: "{violations}",
            description: "Total number of CSP violation reports processed.");

        PathPolicyMatches = Meter.CreateCounter<long>(
            "safewebcore.path_policy_matches_total",
            unit: "{matches}",
            description: "Total number of times a path-specific policy was matched.");
    }
}
