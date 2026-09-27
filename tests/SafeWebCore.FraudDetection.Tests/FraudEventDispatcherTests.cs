using System.Diagnostics.Metrics;
using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Infrastructure;
using SafeWebCore.FraudDetection.Models;

namespace SafeWebCore.FraudDetection.Tests;

/// <summary>
/// Tests for the fraud event dispatcher: per-sink isolation and the swallowed-failure counter, so a
/// broken sink can never mute the sinks registered after it.
/// </summary>
public sealed class FraudEventDispatcherTests
{
    [Fact]
    public void DispatchKeepsDeliveringWhenAnEarlierSinkThrows()
    {
        // Arrange
        var failing = new RecordingFraudEventSink(throwOnEvent: true);
        var recording = new RecordingFraudEventSink();
        var dispatcher = new FraudEventDispatcher([failing, recording]);

        // Act
        dispatcher.Dispatch(CreateEvent());

        // Assert
        Assert.Empty(failing.Events);
        Assert.Single(recording.Events);
    }

    [Fact]
    public void DispatchCountsEverySwallowedSinkFailureInMetrics()
    {
        // Arrange
        var metrics = new SafeWebCoreFraudMetrics();
        long observed = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == SafeWebCoreFraudMetrics.MeterName &&
                instrument.Name == "safewebcore.fraud_event_sink_failures_total")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "safewebcore.fraud_event_sink_failures_total")
                observed += measurement;
        });
        listener.Start();

        var dispatcher = new FraudEventDispatcher(
            [new RecordingFraudEventSink(throwOnEvent: true), new RecordingFraudEventSink(throwOnEvent: true)],
            metrics);

        // Act
        dispatcher.Dispatch(CreateEvent());

        // Assert
        Assert.Equal(2, observed);
    }

    [Fact]
    public void DispatchWithoutSinksOrMetricsCompletes()
    {
        // Arrange - the metrics dependency is optional, so no counter may be required
        var dispatcher = new FraudEventDispatcher([]);

        // Act + Assert - must not throw
        dispatcher.Dispatch(CreateEvent());
    }

    private static FraudEvent CreateEvent()
        => new()
        {
            Report = new FraudReport { Verdict = FraudVerdict.Clean }
        };

    private sealed class RecordingFraudEventSink(bool throwOnEvent = false) : IFraudEventSink
    {
        public List<FraudEvent> Events { get; } = [];

        public void OnFraudEvent(FraudEvent fraudEvent)
        {
            if (throwOnEvent)
                throw new InvalidOperationException("Simulated sink failure.");

            Events.Add(fraudEvent);
        }
    }
}
