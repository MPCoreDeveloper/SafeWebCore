using System.Collections.Generic;
using System.Diagnostics.Metrics;
using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Models;

namespace SafeWebCore.FraudDetection.Infrastructure;

/// <summary>
/// Internal dispatcher that forwards <see cref="FraudEvent"/> to all registered <see cref="IFraudEventSink"/> implementations.
/// This is additive and has no effect if no sinks are registered.
/// When SafeWebCoreFraudMetrics is provided, it also increments standard meters (opt-in consumption).
/// </summary>
internal sealed class FraudEventDispatcher : IFraudEventDispatcher
{
    private readonly IFraudEventSink[] _sinks;
    private readonly SafeWebCoreFraudMetrics? _metrics;

    public FraudEventDispatcher(IEnumerable<IFraudEventSink> sinks, SafeWebCoreFraudMetrics? metrics = null)
    {
        _sinks = sinks?.ToArray() ?? [];
        _metrics = metrics;
    }

    /// <inheritdoc />
    public void Dispatch(FraudEvent fraudEvent)
    {
        if (fraudEvent is null) return;

        if (_metrics is not null)
        {
            _metrics.FraudAnalyses.Add(1);

            var riskLevel = fraudEvent.Report.Risk.Level.ToString();
            var verdict = fraudEvent.Report.Verdict.ToString();

            _metrics.FraudEventsByRisk.Add(1, new KeyValuePair<string, object?>("risk_level", riskLevel));
            _metrics.FraudEventsByVerdict.Add(1, new KeyValuePair<string, object?>("verdict", verdict));
        }

        foreach (var sink in _sinks)
        {
            try
            {
                sink.OnFraudEvent(fraudEvent);
            }
            catch
            {
                // Sinks must not break fraud detection. Swallow per sink.
            }
        }
    }
}

/// <summary>
/// Internal contract for dispatching fraud events.
/// </summary>
internal interface IFraudEventDispatcher
{
    void Dispatch(FraudEvent fraudEvent);
}

/// <summary>
/// No-op dispatcher used when no sinks are registered.
/// </summary>
internal sealed class NoOpFraudEventDispatcher : IFraudEventDispatcher
{
    public void Dispatch(FraudEvent fraudEvent) { }
}
