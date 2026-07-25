using System.Diagnostics.Metrics;

namespace SafeWebCore.FraudDetection.Infrastructure;

/// <summary>
/// Provides opt-in metrics for SafeWebCore.FraudDetection using System.Diagnostics.Metrics.
/// 
/// Meter name: "SafeWebCore.FraudDetection"
/// Instruments are created on registration.
/// Actual metric export is opt-in (OpenTelemetry, etc.).
/// </summary>
public sealed class SafeWebCoreFraudMetrics
{
    /// <summary>The canonical meter name used for FraudDetection metrics.</summary>
    public const string MeterName = "SafeWebCore.FraudDetection";

    /// <summary>The underlying Meter for advanced scenarios.</summary>
    public Meter Meter { get; }

    /// <summary>Counter for total fraud analyses performed.</summary>
    public Counter<long> FraudAnalyses { get; }

    /// <summary>Counter for fraud events tagged by RiskLevel.</summary>
    public Counter<long> FraudEventsByRisk { get; }

    /// <summary>Counter for fraud events tagged by FraudVerdict.</summary>
    public Counter<long> FraudEventsByVerdict { get; }

    /// <summary>
    /// Creates the FraudDetection metrics instruments.
    /// </summary>
    public SafeWebCoreFraudMetrics()
    {
        Meter = new Meter(MeterName, "1.0");

        FraudAnalyses = Meter.CreateCounter<long>(
            "safewebcore.fraud_analyses_total",
            unit: "{analyses}",
            description: "Total number of fraud analyses performed.");

        FraudEventsByRisk = Meter.CreateCounter<long>(
            "safewebcore.fraud_events_by_risk_total",
            unit: "{events}",
            description: "Fraud events broken down by RiskLevel (Low, Medium, High, Critical). Tag: risk_level");

        FraudEventsByVerdict = Meter.CreateCounter<long>(
            "safewebcore.fraud_events_by_verdict_total",
            unit: "{events}",
            description: "Fraud events broken down by FraudVerdict. Tag: verdict");
    }
}
