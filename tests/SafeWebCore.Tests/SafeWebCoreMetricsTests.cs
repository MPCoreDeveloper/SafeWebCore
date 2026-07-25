using System.Diagnostics.Metrics;
using SafeWebCore.Infrastructure;

namespace SafeWebCore.Tests;

/// <summary>
/// Tests for the opt-in SafeWebCore metrics (System.Diagnostics.Metrics).
/// Uses MeterListener to verify counters without requiring an external exporter.
/// </summary>
public sealed class SafeWebCoreMetricsTests
{
    [Fact]
    public void HeadersAppliedCounterIncrements()
    {
        var metrics = new SafeWebCoreMetrics();
        long observed = 0;

        using var listener = new MeterListener();
        ConfigureListener(listener, SafeWebCoreMetrics.MeterName, "safewebcore.headers_applied_total", m => observed += m);
        listener.Start();

        metrics.HeadersApplied.Add(1);
        metrics.HeadersApplied.Add(2);

        Assert.Equal(3, observed);
    }

    [Fact]
    public void CspViolationsCounterIncrements()
    {
        var metrics = new SafeWebCoreMetrics();
        long observed = 0;

        using var listener = new MeterListener();
        ConfigureListener(listener, SafeWebCoreMetrics.MeterName, "safewebcore.csp_violations_total", m => observed += m);
        listener.Start();

        metrics.CspViolations.Add(1);

        Assert.Equal(1, observed);
    }

    [Fact]
    public void PathPolicyMatchesCounterIncrements()
    {
        var metrics = new SafeWebCoreMetrics();
        long observed = 0;

        using var listener = new MeterListener();
        ConfigureListener(listener, SafeWebCoreMetrics.MeterName, "safewebcore.path_policy_matches_total", m => observed += m);
        listener.Start();

        metrics.PathPolicyMatches.Add(5);

        Assert.Equal(5, observed);
    }

    private static void ConfigureListener(MeterListener listener, string meterName, string instrumentName, Action<long> onMeasurement)
    {
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == meterName && instrument.Name == instrumentName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == instrumentName)
                onMeasurement(measurement);
        });
    }
}
