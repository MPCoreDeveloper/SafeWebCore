using System.Diagnostics.Metrics;
using SafeWebCore.FraudDetection.Infrastructure;
using SafeWebCore.FraudDetection.Models;

namespace SafeWebCore.FraudDetection.Tests;

/// <summary>
/// Tests for the opt-in FraudDetection metrics using MeterListener.
/// </summary>
public sealed class FraudMetricsTests
{
    [Fact]
    public void FraudAnalysesCounterIncrements()
    {
        var metrics = new SafeWebCoreFraudMetrics();
        long observed = 0;

        using var listener = new MeterListener();
        ConfigureListener(listener, metrics.FraudAnalyses, (m, _) => observed += m);
        listener.Start();

        metrics.FraudAnalyses.Add(1);
        metrics.FraudAnalyses.Add(1);

        Assert.Equal(2, observed);
    }

    [Fact]
    public void FraudEventsByRiskIncrementsWithTag()
    {
        var metrics = new SafeWebCoreFraudMetrics();
        long critical = 0;

        using var listener = new MeterListener();
        ConfigureListener(listener, metrics.FraudEventsByRisk, (m, tags) =>
        {
            if (tags.TryGetValue("risk_level", out var level) && level?.ToString() == RiskLevel.Critical.ToString())
                critical += m;
        });
        listener.Start();

        metrics.FraudEventsByRisk.Add(1, new KeyValuePair<string, object?>("risk_level", RiskLevel.Critical.ToString()));
        metrics.FraudEventsByRisk.Add(1, new KeyValuePair<string, object?>("risk_level", RiskLevel.Critical.ToString()));

        Assert.Equal(2, critical);
    }

    [Fact]
    public void FraudEventsByVerdictIncrementsWithTag()
    {
        var metrics = new SafeWebCoreFraudMetrics();
        long regionImp = 0;

        using var listener = new MeterListener();
        ConfigureListener(listener, metrics.FraudEventsByVerdict, (m, tags) =>
        {
            if (tags.TryGetValue("verdict", out var v) && v?.ToString() == FraudVerdict.RegionImpersonation.ToString())
                regionImp += m;
        });
        listener.Start();

        metrics.FraudEventsByVerdict.Add(1, new KeyValuePair<string, object?>("verdict", FraudVerdict.RegionImpersonation.ToString()));

        Assert.Equal(1, regionImp);
    }

    /// <summary>
    /// Enables measurement events for exactly one instrument instance.
    /// </summary>
    /// <remarks>
    /// Filtering by meter name is not enough: every <see cref="SafeWebCoreFraudMetrics"/> instance creates a meter
    /// with the same name, and xUnit runs test classes in parallel, so a name filter also observes the counters of a
    /// detector or dispatcher test that increments its own metrics at the same time — which made this suite fail
    /// intermittently with "Expected: 2, Actual: 3". Enabling the instance keeps every assertion about this test's
    /// own increments.
    /// </remarks>
    private static void ConfigureListener(
        MeterListener listener,
        Instrument instrument,
        Action<long, IReadOnlyDictionary<string, object?>> onMeasurement)
    {
        listener.SetMeasurementEventCallback<long>((observed, measurement, tags, state) =>
        {
            if (!ReferenceEquals(observed, instrument))
            {
                return;
            }

            var dict = new Dictionary<string, object?>();
            foreach (var t in tags)
                dict[t.Key] = t.Value;
            onMeasurement(measurement, dict);
        });

        listener.EnableMeasurementEvents(instrument);
    }
}
