# 🛡️ SafeWebCore

A lightweight, high-performance .NET 10 middleware library that adds security headers to your ASP.NET Core applications. Targets an **A+ rating** on [securityheaders.com](https://securityheaders.com) out of the box.

**Current version:** 1.6.0

New in 1.6.0:
- AddNetSecureHeadersFromConfiguration(...) for direct appsettings.json binding.
- AddNetSecureHeadersForEnvironment(...) and AddNetSecureHeadersStrictAPlusForEnvironment(...) for safer non-production CSP rollout.
- MapSafeWebCoreDiagnostics(...) for opt-in effective header and path-policy preview.
- Opt-in System.Diagnostics.Metrics counters (SafeWebCore meter).
- New companion packages: SafeWebCore.FraudDetection (1.0.0), SafeWebCore.Analyzers (preview), and SafeWebCore.Testing (preview).
- Practical recipe docs under docs/recipes/.
- More actionable startup validation messages with concrete remediation guidance.

## Backward Compatibility Goal

SafeWebCore keeps a strict **100% backward compatibility** contract. New capabilities are additive and opt-in, so existing configurations keep their current behavior.

## Two Ways to Use SafeWebCore

### Option 1 — Strict A+ Preset (fastest)

One line for the strictest A+ configuration. Defined in `ServiceCollectionExtensions.AddNetSecureHeadersStrictAPlus()`.

```csharp
using SafeWebCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNetSecureHeadersStrictAPlus();

var app = builder.Build();
app.UseNetSecureHeaders();
app.Run();
```

Customize the preset — CSP directives are **space-separated**, add multiple origins in one string:

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    // Single origin
    opts.Csp = opts.Csp with { ImgSrc = "'self' https://cdn.example.com" };

    // Multiple origins — just separate with spaces
    opts.Csp = opts.Csp with { ImgSrc = "'self' https://cdn1.example.com https://cdn2.example.com data:" };

    // Multiple directives at once
    opts.Csp = opts.Csp with
    {
        ConnectSrc = "'self' https://api.example.com wss://ws.example.com",
        FontSrc = "'self' https://fonts.gstatic.com https://cdn.example.com"
    };

    // Non-CSP headers
    opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";
});
```

### Option 2 — Fully Custom Configuration

Full control over every header via `ServiceCollectionExtensions.AddNetSecureHeaders()`:

```csharp
using SafeWebCore.Builder;
using SafeWebCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNetSecureHeaders(opts =>
{
    // Transport security
    opts.EnableHsts = true;
    opts.HstsValue = "max-age=31536000; includeSubDomains";

    // Framing
    opts.EnableXFrameOptions = true;
    opts.XFrameOptionsValue = "SAMEORIGIN";

    // MIME sniffing
    opts.EnableXContentTypeOptions = true;
    opts.XContentTypeOptionsValue = "nosniff";

    // Referrer
    opts.EnableReferrerPolicy = true;
    opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";

    // Permissions
    opts.EnablePermissionsPolicy = true;
    opts.PermissionsPolicyValue = "camera=(), microphone=(), geolocation=()";

    // Cross-Origin isolation
    opts.EnableCoep = true;
    opts.CoepValue = "require-corp";
    opts.EnableCoop = true;
    opts.CoopValue = "same-origin";
    opts.EnableCorp = true;
    opts.CorpValue = "same-origin";

    // Server header
    opts.RemoveServerHeader = true;

    // CSP — use the fluent builder
    opts.Csp = new CspBuilder()
        .DefaultSrc("'none'")
        .ScriptSrc("'nonce-{nonce}' 'strict-dynamic' https:")
        .StyleSrc("'nonce-{nonce}'")
        .ImgSrc("'self' https: data:")
        .FontSrc("'self' https://fonts.gstatic.com")
        .ConnectSrc("'self' wss://realtime.example.com")
        .FrameAncestors("'none'")
        .BaseUri("'none'")
        .FormAction("'self'")
        .UpgradeInsecureRequests()
        .Build();
});

var app = builder.Build();
app.UseNetSecureHeaders();
app.Run();
```

Both methods are defined in **`SafeWebCore.Extensions.ServiceCollectionExtensions`**.

## Strict A+ Headers

| Header | Strict A+ Value |
|--------|-----------------|
| `Strict-Transport-Security` | `max-age=63072000; includeSubDomains; preload` |
| `Content-Security-Policy` | Nonce-based, `strict-dynamic`, Trusted Types |
| `X-Frame-Options` | `DENY` |
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `no-referrer` |
| `Permissions-Policy` | All recognized features denied (scanner-safe, modern Chromium tokens only) |
| `Cross-Origin-Embedder-Policy` | `require-corp` |
| `Cross-Origin-Opener-Policy` | `same-origin` |
| `Cross-Origin-Resource-Policy` | `same-origin` |
| `Server` | _(removed)_ |
| `X-Powered-By` | _(removed)_ |

## Features

- 🔒 **Strict A+ preset** — one-line setup with the strictest security headers
- 🌐 **Browser-safe Permissions-Policy** — preset emits only scanner-recognised tokens; invalid directives (e.g. identity-credentials-get, otp-credentials, publickey-credentials-create, window-management) and stale tokens removed to pass securityheaders.com checks without warnings
- 🛠️ **Fully custom** — configure every header and CSP directive individually
- 🧩 **Nonce-based CSP** — per-request cryptographic nonces for scripts and styles
- 🧷 **Razor nonce TagHelpers** — auto-add nonce to `<script>` and `<style>` in Razor views
- 🛣️ **Path-based policies** — assign different security profiles per route prefix (longest-prefix wins)
- 🧪 **Startup validation** — fail fast on invalid combinations and duplicate path policies
- 📝 **CSP Report-Only mode** — safely test policy changes before hard enforcement
- 🧱 **Typed policy builders** — strongly typed builders for `Referrer-Policy`, `Permissions-Policy`, and COEP/COOP/CORP
- 🧭 **First-class upcoming header support** — configure non-standard or emerging headers through `AdditionalHeaders` (opt-in)
- 📡 **First-class Reporting API endpoint support** — emit `Reporting-Endpoints` from typed `ReportingEndpoints` options (opt-in)
- 📋 **Full CSP Level 3** (W3C Recommendation) — all 22 directives, nonce/hash support, `strict-dynamic`, `report-to`, `worker-src`, `frame-src`, `manifest-src`, `script-src-elem/attr`, `style-src-elem/attr`
- 🔮 **CSP Level 4 ready** — Trusted Types (`require-trusted-types-for`, `trusted-types`), `fenced-frame-src` (Privacy Sandbox)
- 🎯 **Fluent CSP Builder** — type-safe, chainable API with full XML documentation
- ⚡ **Zero-allocation nonce generation** — `stackalloc` + `RandomNumberGenerator`, plus `TryWriteNonce(Span<char>)` for fully heap-free scenarios *(v1.1.0)*
- 🔍 **`HttpContext.GetCspNonce()`** — discoverable extension method to retrieve the per-request nonce *(v1.1.0)*
- 🚀 **Pre-built CSP template** — CSP header string computed once at startup, not per-request *(v1.1.0)*
- 🔌 **Extensible** — custom `IHeaderPolicy` implementations
- 📊 **CSP violation reporting** — built-in `/csp-report` endpoint using Reporting API v1

## First-class Upcoming Headers (Opt-in)

Use `AdditionalHeaders` when you want to emit upcoming or non-standard headers without writing a custom policy type:

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.AdditionalHeaders.Add(new()
    {
        Name = "Document-Policy",
        Value = "force-load-at-top"
    });
});
```

## First-class Reporting Endpoints (Opt-in)

Use `ReportingEndpoints` to emit the `Reporting-Endpoints` response header and map endpoint groups used by CSP `report-to`:

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.Csp = opts.Csp with { ReportTo = "default" };

    opts.ReportingEndpoints.Add(new()
    {
        Group = "default",
        Url = "https://reports.example.com/csp"
    });
});
```

Emitted header value:

```text
Reporting-Endpoints: default="https://reports.example.com/csp"
```

## Validate Your Headers

After deploying, test your security headers with:

- **[securityheaders.com](https://securityheaders.com/)** — Grades all response headers A+ through F. With the Strict A+ preset you should score **A+** immediately.
- **[Google CSP Evaluator](https://csp-evaluator.withgoogle.com/)** — Paste your `Content-Security-Policy` value to check for misconfigurations (missing `object-src`, `'unsafe-inline'` without nonce, missing `'strict-dynamic'`, etc.).

## Documentation

Full documentation: [github.com/MPCoreDeveloper/SafeWebCore/docs](https://github.com/MPCoreDeveloper/SafeWebCore/tree/master/docs)

Planning documents:
- [Current Roadmap](https://github.com/MPCoreDeveloper/SafeWebCore/blob/master/docs/roadmap.md) — active planning for v1.4 → v1.6
- [v1.2 Roadmap (archived / completed)](https://github.com/MPCoreDeveloper/SafeWebCore/blob/master/docs/archive/roadmap-v1.2.md)
- [v1.2 Implementation Plan (archived / completed)](https://github.com/MPCoreDeveloper/SafeWebCore/blob/master/docs/archive/implementation-plan-v1.2.md)

## License

MIT — see [LICENSE](https://github.com/MPCoreDeveloper/SafeWebCore/blob/master/LICENSE)
