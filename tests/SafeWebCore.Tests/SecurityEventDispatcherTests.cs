using System.Diagnostics.Metrics;
using SafeWebCore.Abstractions;
using SafeWebCore.Infrastructure;

namespace SafeWebCore.Tests;

/// <summary>
/// Tests for the security event dispatcher: per-sink isolation and the swallowed-failure counter that
/// keep one broken telemetry sink from muting every sink registered after it.
/// </summary>
public sealed class SecurityEventDispatcherTests
{
    [Fact]
    public async Task EmitAsyncKeepsDeliveringWhenAnEarlierSinkThrows()
    {
        // Arrange
        var failing = new RecordingSecurityEventSink(throwOnWrite: true);
        var recording = new RecordingSecurityEventSink();
        var dispatcher = new SecurityEventDispatcher([failing, recording]);

        // Act
        await dispatcher.EmitAsync(CreateEvent(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(failing.Events);
        Assert.Single(recording.Events);
    }

    [Fact]
    public async Task EmitAsyncCountsEverySwallowedSinkFailureInMetrics()
    {
        // Arrange
        var metrics = new SafeWebCoreMetrics();
        long observed = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == SafeWebCoreMetrics.MeterName &&
                instrument.Name == "safewebcore.security_event_sink_failures_total")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "safewebcore.security_event_sink_failures_total")
                observed += measurement;
        });
        listener.Start();

        var dispatcher = new SecurityEventDispatcher(
            [new RecordingSecurityEventSink(throwOnWrite: true), new RecordingSecurityEventSink(throwOnWrite: true)],
            metrics);

        // Act
        await dispatcher.EmitAsync(CreateEvent(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, observed);
    }

    [Fact]
    public async Task EmitAsyncInvokesSinksInRegistrationOrder()
    {
        // Arrange
        var order = new List<string>();
        var dispatcher = new SecurityEventDispatcher(
            [new OrderedSecurityEventSink("first", order), new OrderedSecurityEventSink("second", order)]);

        // Act
        await dispatcher.EmitAsync(CreateEvent(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, order.Count);
        Assert.Equal("first", order[0]);
        Assert.Equal("second", order[1]);
    }

    [Fact]
    public async Task EmitAsyncWithoutSinksOrMetricsCompletes()
    {
        // Arrange - the metrics dependency is optional, so the backward-compatible constructor must work
        var dispatcher = new SecurityEventDispatcher([]);

        // Act - the call must complete without throwing
        var exception = await Record.ExceptionAsync(
            () => dispatcher.EmitAsync(CreateEvent(), TestContext.Current.CancellationToken));

        // Assert
        Assert.Null(exception);
    }

    private static SecurityEvent CreateEvent()
        => new()
        {
            EventType = SecurityEventType.HeadersApplied,
            Path = "/"
        };

    private sealed class RecordingSecurityEventSink(bool throwOnWrite = false) : ISecurityEventSink
    {
        public List<SecurityEvent> Events { get; } = [];

        public Task WriteAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
        {
            if (throwOnWrite)
                throw new InvalidOperationException("Simulated sink failure.");

            Events.Add(securityEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class OrderedSecurityEventSink(string name, List<string> order) : ISecurityEventSink
    {
        public Task WriteAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
        {
            order.Add(name);
            return Task.CompletedTask;
        }
    }
}
