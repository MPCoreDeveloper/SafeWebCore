# 10. Report-Only Rollout and Reporting

A strict policy on an existing site is a change that breaks things, and the breakage is invisible until a user hits it. The
way to make that change safely is a report-only rollout: send the policy in the header a browser evaluates but does not
enforce, collect what it would have blocked, fix the site, then enforce.

## The report-only switch

```csharp
builder.Services.AddNetSecureHeaders(options =>
{
    options.ApplyPreset(SecurePresets.Mvc());
    options.UseCspReportOnly = true;         // CSP travels in Content-Security-Policy-Report-Only
});
```

The header name changes; the policy value does not. Everything else — the nonce, the other headers — is unaffected. Two
rules apply:

- `UseCspReportOnly` requires `EnableCsp`; the combination is rejected at startup (Chapter 5).
- Report-only is per configuration. A path policy or an endpoint override may be report-only while the rest enforces
  (Chapters 8 and 9).

The `ForEnvironment` overloads are the shortest way to make the difference deliberate:

```csharp
builder.Services.AddNetSecureHeadersStrictAPlusForEnvironment(builder.Environment);
// production: enforced. every other environment: report-only.
```

## Receiving the reports

Browsers send a report as a `POST` to the URL named by `report-uri`, or to the endpoint named by a Reporting API group.
`UseCspReport()` is the handler for both shapes:

```csharp
app.UseCspReport();   // POST /csp-report
```

| Aspect | Behaviour |
| --- | --- |
| Path | `/csp-report`, POST only. Any other request passes through untouched |
| Content types | the legacy `application/csp-report` object and the Reporting API array (`application/reports+json`) |
| Parsing | `CspViolationReport`, with `document-uri`, `violated-directive`, `effective-directive`, `blocked-uri`, `disposition`, `status-code`, `source-file`, line and column, and `sample` |
| Result | `204 No Content` when every sink accepted it, `400 Bad Request` on an unparsable payload |
| Sinks | every `ICspReportSink` in the container, in registration order |

Point the policy at it:

```csharp
options.ReportingEndpoints =
[
    new ReportingEndpointOptions { Group = "default", Url = "https://app.example.com/csp-report" }
];
options.Csp = new CspBuilder().ReportTo("default").Build();
```

## Writing a sink

A sink is an `ICspReportSink` in the container. The default one logs a warning per violation; registering your own adds
to it rather than replacing it, because the default is added with `TryAddEnumerable`.

```csharp
using SafeWebCore.Abstractions;
using SafeWebCore.Models;

public sealed class ViolationTableSink(ILogger<ViolationTableSink> logger) : ICspReportSink
{
    public Task WriteAsync(CspViolationReport report, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "CSP blocked {Blocked} on {Document} ({Directive})",
            report.BlockedUri,
            report.DocumentUri,
            report.EffectiveDirective);

        return Task.CompletedTask;
    }
}
```

```csharp
builder.Services.AddSingleton<ICspReportSink, ViolationTableSink>();
```

Two design notes from the production use this was built for: the sink receives the parsed report, not a `JsonElement`, so
it does not have to re-implement the two payload shapes; and a throwing sink does not stop the others — each is invoked
inside its own `try`, exactly like the event dispatcher of Chapter 12.

## Network Error Logging

`Network-Error-Logging` is the Reporting API's sibling for transport failures. It is opt-in because it requires the
Reporting API group to exist:

```csharp
options.EnableNel = true;
options.NelValue = """{"report_to":"default","max_age":2592000}""";
```

The validator checks that `NelValue` looks like a JSON object with the recommended `report_to` and `max_age` fields, and
warns when the reporting group it names is not configured. Chapter 11 shows the same finding in the diagnostics output.

## A rollout that survives review

1. Turn on `UseCspReportOnly` for the whole application. Enforce nothing yet.
2. Register a sink that records `effective-directive` and `blocked-uri` with a count, not one log line per hit.
3. Wait a week. Add the sources the report proves you need — a CDN, an analytics host, an inline script that needs a
   nonce (Chapter 7) — and re-deploy in report-only.
4. When the report goes quiet for the paths that matter, enforce one path policy at a time (Chapter 8).
5. Keep the reporting endpoint. It costs nothing and it is how the next regression announces itself.
