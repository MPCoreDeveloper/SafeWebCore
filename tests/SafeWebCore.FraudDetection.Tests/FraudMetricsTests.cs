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
        ConfigureListener(listener, SafeWebCoreFraudMetrics.MeterName, "safewebcore.fraud_analyses_total", (m, _) => observed += m);
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
        ConfigureListener(listener, SafeWebCoreFraudMetrics.MeterName, "safewebcore.fraud_events_by_risk_total", (m, tags) =>
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
        ConfigureListener(listener, SafeWebCoreFraudMetrics.MeterName, "safewebcore.fraud_events_by_verdict_total", (m, tags) =>
        {
            if (tags.TryGetValue("verdict", out var v) && v?.ToString() == FraudVerdict.RegionImpersonation.ToString())
                regionImp += m;
        });
        listener.Start();

        metrics.FraudEventsByVerdict.Add(1, new KeyValuePair<string, object?>("verdict", FraudVerdict.RegionImpersonation.ToString()));

        Assert.Equal(1, regionImp);
    }

    private static void ConfigureListener(MeterListener listener, string meterName, string instrumentName, Action<long, IReadOnlyDictionary<string, object?>> onMeasurement)
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
            {
                var dict = new Dictionary<string, object?>();
                foreach (var t in tags)
                    dict[t.Key] = t.Value;
                onMeasurement(measurement, dict);
            }
        });
    }
}
