# 12. Telemetry: Events and Metrics

Two channels, both opt-in and both additive: **events** push something that happened to your code, **metrics** let a
collector pull counters. Neither can change a response — the dispatch path is written so that a broken consumer cannot
break a request.

## Events

A security event is a record with four fields:

```csharp
public sealed record SecurityEvent
{
    public required SecurityEventType EventType { get; init; }   // CspViolation=1, PathPolicyMatched=2, HeadersApplied=3, Diagnostic=4
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? Path { get; init; }
    public IReadOnlyDictionary<string, object?> Properties { get; init; }
}
```

| Event | Emitted when | `Properties` |
| --- | --- | --- |
| `HeadersApplied` | every request the middleware handles | `HasCsp`, `UseReportOnly`, `MatchedPathPolicy` |
| `PathPolicyMatched` | a path policy matched (in addition to `HeadersApplied`) | `MatchedPathPolicy` |
| `CspViolation` | the report endpoint accepted a violation (Chapter 10) | `ViolatedDirective`, `BlockedUri`, `Disposition` |

Write a sink and register it:

```csharp
using SafeWebCore.Abstractions;

public sealed class AuditSink(IAuditWriter writer) : ISecurityEventSink
{
    public Task WriteAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        if (securityEvent.EventType == SecurityEventType.CspViolation)
        {
            writer.RecordViolation(securityEvent.Path, securityEvent.Properties);
        }

        return Task.CompletedTask;
    }
}
```

```csharp
builder.Services.AddSafeWebCoreSecurityEventSink<AuditSink>();
```

**A throwing sink is isolated.** The dispatcher materialises the sink sequence once, invokes every sink inside its own
`try`, counts a failure in `SecurityEventSinkFailures` and continues with the next one. That matters because the security
call sites do not await the returned task: an unobserved exception there would be a crash in a background thread much
later. A sink is therefore a *best-effort observer*, never a place to put a security decision.

**The default sink logs one Information line per request.** `LoggingSecurityEventSink` is registered with
`TryAddEnumerable`, so it is present whether or not you add your own. If the volume is not wanted, remove it explicitly
before adding yours:

```csharp
builder.Services.RemoveAll<ISecurityEventSink>();   // Microsoft.Extensions.DependencyInjection.Extensions
builder.Services.AddSafeWebCoreSecurityEventSink<AuditSink>();
```

## Metrics

`SafeWebCoreMetrics` publishes four counters on the meter **`SafeWebCore`**:

| Counter | Instrument name | Meaning |
| --- | --- | --- |
| `HeadersApplied` | `safewebcore.headers_applied_total` | Requests the middleware handled |
| `CspViolations` | `safewebcore.csp_violations_total` | Reports the endpoint accepted |
| `PathPolicyMatches` | `safewebcore.path_policy_matches_total` | Requests that matched a path policy |
| `SecurityEventSinkFailures` | `safewebcore.security_event_sink_failures_total` | Sink invocations that threw and were isolated |

`SafeWebCore.FraudDetection` publishes on its own meter, **`SafeWebCore.FraudDetection`** (Chapter 15):

| Counter | Instrument name |
| --- | --- |
| `FraudAnalyses` | `safewebcore.fraud_analyses_total` |
| `FraudEventsByRisk` | `safewebcore.fraud_events_by_risk_total` (tag `risk_level`) |
| `FraudEventsByVerdict` | `safewebcore.fraud_events_by_verdict_total` (tag `verdict`) |
| `FraudEventSinkFailures` | `safewebcore.fraud_event_sink_failures_total` |

Consume them with any `MeterListener`, or with OpenTelemetry:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter(SafeWebCoreMetrics.MeterName)                 // "SafeWebCore"
        .AddMeter(SafeWebCoreFraudMetrics.MeterName)            // "SafeWebCore.FraudDetection"
        .AddPrometheusExporter());
```

## What to alert on

| Signal | Why it matters |
| --- | --- |
| `headers_applied_total` flat while traffic is not | The middleware is not in the pipeline — a routing or `Use*` order mistake |
| `csp_violations_total` jumps | A deploy added markup the policy blocks; the report says which directive |
| `security_event_sink_failures_total` above zero | An audit or SIEM sink is broken; the headers are still applied |
| `path_policy_matches_total` static on an area you just split | The prefix did not normalise the way you expected (Chapter 8) |

> **Telemetry is not the security control.** The headers protect the browser whether or not anyone collects the counters.
> Metrics make a regression visible early; they do not prevent one.
