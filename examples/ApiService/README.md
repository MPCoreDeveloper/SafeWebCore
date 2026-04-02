# SafeWebCore – API Service Example

Demonstrates SafeWebCore in a Web API application with a custom `ICspReportSink`, endpoint-level header overrides (`[SkipNetSecureHeaders]`, `[CspMode]`), path-based policies, and optional headers.

## What this example shows

| Feature | Where |
|---------|-------|
| `AddNetSecureHeadersApiPreset()` | `Program.cs` |
| Optional `X-Robots-Tag` header | `Program.cs` |
| Path-based policy for `/internal` | `Program.cs` |
| Custom `ICspReportSink` (`JsonFileCspReportSink`) | `Program.cs`, `Infrastructure/JsonFileCspReportSink.cs` |
| `[SkipNetSecureHeaders]` on health probe | `Controllers/AdminController.cs` |
| `[CspMode(CspEndpointMode.ReportOnly)]` on metrics endpoint | `Controllers/AdminController.cs` |
| Standard secured controller (no overrides) | `Controllers/ProductsController.cs` |

## Running the example

```bash
cd examples/ApiService
dotnet run
```

Then exercise the endpoints:

```bash
# Fully secured — inspect headers
curl -sI http://localhost:5000/api/products

# Health probe — no security headers (SkipNetSecureHeaders)
curl -sI http://localhost:5000/admin/health

# Metrics — Content-Security-Policy-Report-Only instead of enforced CSP
curl -sI http://localhost:5000/admin/metrics

# Internal — path policy headers (no CSP, strict HSTS)
curl -sI http://localhost:5000/internal/config
```

## Custom ICspReportSink

`JsonFileCspReportSink` writes each violation as a JSON-lines entry to `csp-violations.jsonl` next to the binary. Multiple sinks can run in parallel — the built-in `CspLoggingReportSink` (structured ILogger) is always active alongside any additional sinks you register.

```csharp
// Register additional sink — built-in logging sink stays active
builder.Services.AddSingleton<ICspReportSink, JsonFileCspReportSink>();
```

Implement `ICspReportSink` to forward violations anywhere: a database, a message queue, an external SIEM, etc.

## Endpoint metadata overrides

```csharp
// Skip all headers (health probes, readiness checks)
[SkipNetSecureHeaders]
public IActionResult Health() => Ok(...);

// Override CSP to report-only for this specific action
[CspMode(CspEndpointMode.ReportOnly)]
public IActionResult Metrics() => Ok(...);
```

These attributes work on both controller actions and Minimal API endpoints (`.WithMetadata(...)`).
