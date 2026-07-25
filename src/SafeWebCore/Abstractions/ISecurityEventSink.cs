using System;
using System.Threading;
using System.Threading.Tasks;

namespace SafeWebCore.Abstractions;

/// <summary>
/// Receives security-related events from SafeWebCore for custom processing (logging, metrics, telemetry).
/// This is an additive extensibility point and does not change default behavior.
/// </summary>
public interface ISecurityEventSink
{
    /// <summary>
    /// Called when a security-relevant event occurs.
    /// </summary>
    /// <param name="securityEvent">The security event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a security-related event emitted by SafeWebCore.
/// </summary>
public sealed record SecurityEvent
{
    /// <summary>The type of security event.</summary>
    public required SecurityEventType EventType { get; init; }

    /// <summary>When the event occurred (UTC).</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Optional path or resource associated with the event.</summary>
    public string? Path { get; init; }

    /// <summary>Additional structured properties (safe for telemetry export).</summary>
    public System.Collections.Generic.IReadOnlyDictionary<string, object?> Properties { get; init; }
        = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(new System.Collections.Generic.Dictionary<string, object?>());
}

/// <summary>
/// Known security event types emitted by SafeWebCore.
/// </summary>
public enum SecurityEventType
{
    /// <summary>A CSP violation was processed.</summary>
    CspViolation = 1,

    /// <summary>A path policy was matched for a request.</summary>
    PathPolicyMatched = 2,

    /// <summary>Headers were applied to a response.</summary>
    HeadersApplied = 3,

    /// <summary>A diagnostic or warning was generated.</summary>
    Diagnostic = 4
}
