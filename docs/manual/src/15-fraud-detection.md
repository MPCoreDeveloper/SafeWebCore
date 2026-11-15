# 15. SafeWebCore.FraudDetection

`SafeWebCore.FraudDetection` is an optional companion package: it scores the client behind a request and returns a report
the application can act on. It is a separate package with its own identity and version, and the core library does not
reference it.

## What it evaluates

The detector compares what a client *claims* with what its request *shows*. The signals fall into three groups:

| Group | Examples |
| --- | --- |
| Network and geography | IP address, the country the IP resolves to, expected countries, known travel countries |
| Language and culture | `Accept-Language`, browser languages, system timezone, timezone offset, installed fonts, script support (Cyrillic, Arabic, CJK, Devanagari) |
| Behaviour | request path and method, user agent, headers, request timing, burst rate |
| Pen-test signals | scanner user agents, scanner headers, path probes, an authorisation header that marks an authorised test |

The result is a `FraudReport` with a `SuspicionScore` (0–100), a `RiskScore` (`Low`, `Medium`, `High`, `Critical`,
`Unclassified`), a `FraudVerdict` (`Clean`, `Suspicious`, `HighlySuspicious`, `RegionImpersonation`), a
`RecommendedAction` (`NoAction`, `Monitor`, `StepUpAuthentication`, `BlockRequest`) and the `Triggers` that produced
them. Every mapping fails closed: a verdict the library does not recognise becomes `Unclassified`, not `Low`.

## Registering and calling it

```csharp
using SafeWebCore.FraudDetection.Extensions;

builder.Services.AddSafeWebCoreFraudDetection(options =>
{
    options.EnableGeoCulturalConsistency = true;
    options.EnablePenTestDetection = true;
    options.GeoCulturalConsistency.ExpectedCountries.Add("NL");
    options.PenTestDetection.AuthorizationCheckRecipients.Add("security@example.com");
});
```

```csharp
public sealed class CheckoutEndpoint(IFraudDetector detector)
{
    public IResult Handle(ClientFingerprintData data)
    {
        var report = detector.Analyze(data);

        return report.RecommendedAction switch
        {
            RecommendedAction.BlockRequest => Results.StatusCode(StatusCodes.Status403Forbidden),
            RecommendedAction.StepUpAuthentication => Results.StatusCode(StatusCodes.Status401Unauthorized),
            _ => Results.Ok()
        };
    }
}
```

`ClientFingerprintData` is a record you fill from the request: `IpAddress`, `ResolvedCountryCode`, `AcceptLanguage`,
`UserAgent`, `SystemTimezone`, `TimezoneOffsetMinutes`, `BrowserLanguages`, `FingerprintVisitorId`, the font and script
flags, `InstalledFonts`, `ScreenResolution`, `TenantId`, `RequestMethod`, `RequestPath`, `RequestTimestampUtc` and
`RequestHeaders`. Country resolution is pluggable through `IGeoIpService`; the package does not embed a database.

## Authorised penetration tests

A scanner hitting your site is indistinguishable from an attacker unless it says who it is. The package supports that
explicitly:

```csharp
options.PenTestDetection.AuthorizationHeaderName = "X-PenTest-Authorized";
options.PenTestDetection.AuthorizationHeaderSecret = configuration["PenTest:Secret"];
options.PenTestDetection.SendAuthorizationCheckEmail = true;
options.PenTestDetection.NotificationCooldown = TimeSpan.FromMinutes(10);
```

A request carrying the configured header (and, when a secret is configured, the matching value) returns an authorised
report with `IsAuthorizedPenTest` and `IsDetectionBypassed` set and `SuspicionScore` at zero. An *un*authorised scanner
gets a report and, at most once per cooldown per source, a notification asking whether the test is authorised.

## Notifications stay injectable

This is a design rule of the package, not an implementation detail: **a notification is delivered through an interface you
register**, so an application can plug in its own mail module or route it to a ticket. Nothing is hard-coded, and
notifications are never reduced to a log line.

| Port | Registered with | Purpose |
| --- | --- | --- |
| `IPenTestAuthorizationNotificationSender` | default: logging sender | Delivers "is this test authorised?" |
| `IPenTestAuthorizationNotificationConsumer` | `AddPenTestAuthorizationNotificationConsumer<T>()` | Application-side delivery (queue, ticket, chat) |
| `IPenTestAuthorizationNotificationMailClient` | `AddPenTestAuthorizationNotificationMailClient<T>()` | Mail specifically, with recipients and subject from the options |

The default sender logs; adding a consumer or a mail client gives the notification a real destination without touching the
detector.

## Events, hooks and metrics

| Extension | Effect |
| --- | --- |
| `AddFraudEventSink<T>()` | Every analysis produces an event for your `IFraudEventSink` |
| `AddFraudWebhookSink(...)` | The built-in HTTP sink, for a SIEM or an automation endpoint |

The metrics are the four counters of Chapter 12 on the meter `SafeWebCore.FraudDetection`:
`fraud_analyses_total`, `fraud_events_by_risk_total` (tag `risk_level`), `fraud_events_by_verdict_total` (tag `verdict`)
and `fraud_event_sink_failures_total`. A throwing sink is isolated and counted, exactly as in the core package.

## Operating it

- **Start in monitor mode.** Act on `Monitor` and `StepUpAuthentication` before you act on `BlockRequest`; a scoring model
  that is wrong for your population blocks real customers.
- **Tune the thresholds, not the code.** `MediumSuspicionThreshold`, `HighSuspicionThreshold` and
  `HighInconsistencyThreshold` are options; a change is a configuration deploy.
- **Treat a report as input, not as a verdict.** The report says what it saw and what it recommends; the application
  decides, which is why `Analyze` is synchronous and returns data rather than acting.
- **Watch `risk_level` tag cardinality.** The tag is an enum name, so it is bounded — but a dashboard on it is how you
  notice that a new country or language is suddenly suspicious.
