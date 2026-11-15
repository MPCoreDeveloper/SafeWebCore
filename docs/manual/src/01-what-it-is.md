# 1. What SafeWebCore Is

SafeWebCore is a middleware family that answers one question on every response: *which security headers should this
response carry, and with which values?* It answers it from configuration, once per request, and it writes the result
before the body is produced so that no later component has to remember to do it.

It answers six questions for a caller:

1. *What is the strictest thing this application can say?* — `AddNetSecureHeadersStrictAPlus()`, one line, the preset
   that grades A+ on securityheaders.com (Chapter 4).
2. *What does a browser-facing application need?* — the `Mvc`, `Blazor` and `SpaReverseProxy` presets, each a curated
   combination of headers and CSP directives rather than a pile of switches (Chapter 4).
3. *How do I write a policy that my own assets can live with?* — `CspOptions` and the fluent `CspBuilder`, a nonce that
   every `<script>` and `<style>` can quote, and `'strict-dynamic'` so that a nonced bootstrap script may load what it
   needs (Chapters 6 and 7).
4. *How do I apply a different policy to `/api` than to the site?* — path policies with longest-prefix matching
   (Chapter 8), and per-endpoint overrides for the exceptions that do not deserve a path policy (Chapter 9).
5. *How do I roll this out without breaking the site?* — report-only mode, a violation endpoint and a report sink
   (Chapter 10), plus a diagnostics endpoint that previews the effective policy without sending a request (Chapter 11).
6. *How do I know it is still doing what I configured?* — structured events and `System.Diagnostics.Metrics` counters,
   both opt-in (Chapter 12).

Everything else in the API exists to make those answers predictable: a validated options model, presets as data, one
composition root, and a public surface that is tracked symbol by symbol.

## One middleware, one decision

Every response passes the same pipeline: resolve the effective options, generate a nonce, write the headers, hand over to
the next middleware. There is no second code path for a preset, a path policy or a diagnostics preview — the preview
calls the same resolver, which is why it can be trusted to describe what will be sent.

```csharp
using SafeWebCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNetSecureHeadersStrictAPlus();

var app = builder.Build();

app.UseNetSecureHeaders();

app.MapGet("/", () => "Hello, headers.");

app.Run();
```

The response to `/` now carries HSTS, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`,
`Permissions-Policy`, the cross-origin trio, and a `Content-Security-Policy` whose `script-src` is
`'nonce-<per request>' 'strict-dynamic' https:`.

## The nonce is the hinge

A nonce-based policy is the only strict policy that survives a modern front end. The middleware generates 256 bits of
randomness per request, base64-encodes it into 44 characters, exposes it as `HttpContext.GetCspNonce()` and substitutes
it into the `{nonce}` placeholder of the policy. A `<script>` without the matching `nonce` attribute does not run, and an
injected `<script>` cannot guess the value.

```csharp
app.Use(async (context, next) =>
{
    var nonce = context.GetCspNonce();   // null when CSP is disabled
    context.Response.Headers["X-Trace"] = nonce is null ? "no-csp" : "csp";
    await next();
});
```

## The family

SafeWebCore is not one package but a set of independent siblings. None of them references another, except the testing
helpers, which reference the core package on purpose.

| Package | Answers |
| --- | --- |
| `SafeWebCore` | The headers, the CSP, the presets, path policies, overrides, reporting, diagnostics and telemetry |
| `SafeWebCore.FraudDetection` | Who is on the other end, and does their story hold together (Chapter 15) |
| `SafeWebCore.JwtBearer` | Is the token authority reachable and configured, and is the token itself hardened (Chapter 16) |
| `SafeWebCore.Analyzers` | Does this code configure the middleware in a way that cannot work (Chapter 17) |
| `SafeWebCore.Testing` | Assertions and a test host for a suite that has to prove the headers (Chapter 17) |

## What a graded response looks like

The point of the library is not that headers exist; it is that they are the *right* headers with values that a security
scanner accepts. `StrictAPlus` is defined as data in `SecurePresets`, and the first chapter of any review should be to
read that method: it is the specification of "A+", in code.
