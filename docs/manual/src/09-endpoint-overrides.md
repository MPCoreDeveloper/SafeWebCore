# 9. Endpoint Overrides

Path policies split an application by area. Endpoint overrides handle the individual exceptions: the health check that
must not be graded, the webhook that must not receive a policy, the one report that has to be enforced while the rest is
still report-only.

## Two things can be overridden

| Override | Extension | Effect |
| --- | --- | --- |
| Skip the middleware | `SkipNetSecureHeaders()` | No security headers at all for this endpoint |
| Force the CSP mode | `WithCspMode(CspEndpointMode.Enforce \| CspEndpointMode.ReportOnly)` | This endpoint sends the CSP in the named mode, whatever the options say |

```csharp
app.MapGet("/health", () => Results.Ok("up")).SkipNetSecureHeaders();

app.MapGet("/api/report", () => Results.Ok()).WithCspMode(CspEndpointMode.Enforce);
```

In MVC the same behaviour is an attribute on the action or the controller — the extension methods add the same metadata
that the attributes carry:

```csharp
[SkipNetSecureHeaders]
public IActionResult Index() => View();

[CspMode(CspEndpointMode.ReportOnly)]
public IActionResult Preview() => View();
```

## Precedence

The order is fixed and documented, because an "it depends" here is a bug waiting to happen:

1. **`SkipNetSecureHeaders` wins over everything.** The middleware hands the request to the next component before it
   resolves a policy, so a skipped endpoint has no headers, no nonce and no event.
2. **`CspMode` wins over the path policy and the global options** for the *mode only*. The rest of the header set still
   comes from the path policy that matched.
3. **Path policy wins over global options** (Chapter 8).
4. **Global options are last.**

```text
Request  /api/admin/users      path policy /api/admin      → its header set
         + [CspMode(ReportOnly)]                          → its CSP sent report-only
Request  /health + [Skip…]                                 → no headers at all
```

## What a skipped endpoint gives up

Everything the middleware does, not only the headers: no `OnStarting` removal of `Server`, no nonce in
`HttpContext.Items`, no `HeadersApplied` metric and no security event. That is usually what you want for a probe from a
load balancer — and it is the reason to use it narrowly. A skipped endpoint inside an otherwise covered application is a
hole in the report; the diagnostics endpoint (Chapter 11) does not know about it, and the analyzer package does not
either.

## Choosing between the two tools

| Situation | Tool |
| --- | --- |
| A whole area with its own policy | Path policy (Chapter 8) |
| One endpoint that must not be touched (health, webhook, static file) | `SkipNetSecureHeaders()` |
| One endpoint that must be enforced earlier than the rest | `WithCspMode(CspEndpointMode.Enforce)` |
| One endpoint that must stay report-only while the rest enforces | `WithCspMode(CspEndpointMode.ReportOnly)` |
| A header value for one endpoint | A custom policy with a path check (Chapter 13), not an override |

> **The override is metadata, not magic.** `SkipNetSecureHeaders()` and `WithCspMode()` attach metadata the middleware
> reads from `HttpContext.GetEndpoint()`. An endpoint that is not routed — a middleware-terminated request, a static
> file served before routing — has no metadata, so nothing is overridden. Put `UseRouting()` before
> `UseNetSecureHeaders()` when you rely on either.
