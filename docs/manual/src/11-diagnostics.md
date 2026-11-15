# 11. The Diagnostics Endpoint

Configuration is invisible. The diagnostics endpoint makes it visible: it answers *what would this request receive, and
why* without sending that request to a real endpoint.

## Mapping it

```csharp
app.MapSafeWebCoreDiagnostics();                    // GET /safewebcore/diagnostics
app.MapSafeWebCoreDiagnostics("/_diag/headers");    // or wherever your policy allows
```

It answers `GET` with two optional query parameters:

| Parameter | Meaning |
| --- | --- |
| `path` | The request path to resolve. Defaults to `/` |
| `cspMode` | `Enforce` or `ReportOnly`, to preview an endpoint override (Chapter 9) |

```bash
curl -sS "http://localhost:5000/safewebcore/diagnostics?path=/api/orders/42" | jq
```

## What it returns

A JSON snapshot built by the same resolver the middleware uses, so it cannot disagree with what a request would get:

| Field | Meaning |
| --- | --- |
| `path` | The normalised path that was resolved |
| `matchedPathPolicy` | The prefix that won, or `null` when the global options apply |
| `usesGlobalPolicy` | `true` when no path policy matched |
| `effectiveCspModeOverride` | The CSP mode that an endpoint override would force, or `Default` |
| `useCspReportOnly` | The mode the effective options state |
| `removeServerHeader`, `removeXPoweredBy` | Whether `OnStarting` will delete those headers |
| `configuredPathPolicies` | Every prefix that would be considered, longest first |
| `headers` | The header names and values that would be written, with `{nonce}` left as a placeholder |
| `warnings` | The things worth knowing about this configuration |

`warnings` is the part that earns the endpoint its place. It reports, for the resolved policy: that a path policy matched
(so you can see the longest-prefix rule working), that CSP is report-only, that `Server` removal depends on `OnStarting`
and cannot reach a proxy, and that an endpoint override is forcing a mode.

```json
{
  "path": "/api/orders/42",
  "matchedPathPolicy": "/api",
  "usesGlobalPolicy": false,
  "useCspReportOnly": false,
  "headers": {
    "Content-Security-Policy": "default-src 'none'; script-src 'nonce-{nonce}' 'strict-dynamic' https:; …"
  },
  "warnings": [
    "Path policy '/api' matched this preview request. Longest-prefix wins when multiple prefixes overlap."
  ]
}
```

## Using it in a release

- **After a configuration change**, request the endpoint in staging for each area — `/`, `/api/...`, an admin path — and
  diff the response against the previous deploy. A header that disappeared is visible before a scanner finds it.
- **When a scanner disagrees with you**, the `headers` map is the ground truth of what this application sends.
- **When a path policy "does not work"**, `configuredPathPolicies` and `matchedPathPolicy` answer the two questions
  worth asking: was the prefix normalised the way you thought, and did a longer prefix win.

## Protecting it

The endpoint is deliberately unauthenticated, because it is a diagnostic and a diagnostic that needs a token is one nobody
runs. That means **you decide where it may live**:

```csharp
app.MapSafeWebCoreDiagnostics()
    .RequireAuthorization("OperationsOnly");   // or map it only when app.Environment.IsDevelopment()
```

It exposes configuration, not secrets: the policy value, the header names, the prefixes. That is still a map of your
defences, so treat it like a metrics endpoint rather than like a health check. The values it writes are exactly the
values a browser receives on every request, with one difference that matters: `{nonce}` is not substituted, because
there is no request to take a nonce from.
