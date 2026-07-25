using SafeWebCore.FraudDetection.Models;

namespace SafeWebCore.FraudDetection.Abstractions;

/// <summary>
/// Receives fraud analysis events for custom reactions (logging, metrics, notifications, webhooks, etc.).
/// This is an additive, opt-in extensibility point.
/// </summary>
/// <remarks>
/// Register implementations via <c>AddFraudEventSink&lt;T&gt;</c>.
/// Events are delivered after <see cref="IFraudDetector.Analyze"/> produces a <see cref="FraudReport"/>.
/// Existing <see cref="IFraudDetector"/> behavior and <see cref="FraudReport"/> shape are unchanged.
/// </remarks>
public interface IFraudEventSink
{
    /// <summary>
    /// Called when a fraud analysis completes.
    /// Implementations should be fast or dispatch work to a background queue.
    /// </summary>
    /// <param name="fraudEvent">The fraud analysis event.</param>
    void OnFraudEvent(FraudEvent fraudEvent);
}

/// <summary>
/// Represents the result of a fraud analysis for telemetry / action consumers.
/// </summary>
public sealed record FraudEvent
{
    /// <summary>The fraud report produced by the detector.</summary>
    public required FraudReport Report { get; init; }

    /// <summary>
    /// Optional reference to the input fingerprint (may contain sensitive data).
    /// Only populated if the sink registration or configuration explicitly requests it.
    /// Default implementations do not include the fingerprint.
    /// </summary>
    public ClientFingerprintData? Fingerprint { get; init; }

    /// <summary>When the event was created (UTC).</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
