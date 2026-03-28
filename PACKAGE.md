# 🛡️ SafeWebCore

A lightweight, high-performance .NET 10 middleware library that adds security headers to your ASP.NET Core applications. Targets an **A+ rating** on [securityheaders.com](https://securityheaders.com) out of the box.

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
| `Permissions-Policy` | All 29 browser features denied |
| `Cross-Origin-Embedder-Policy` | `require-corp` |
| `Cross-Origin-Opener-Policy` | `same-origin` |
| `Cross-Origin-Resource-Policy` | `same-origin` |
| `Server` | _(removed)_ |

## Features

- 🔒 **Strict A+ preset** — one-line setup with the strictest security headers
- 🛠️ **Fully custom** — configure every header and CSP directive individually
- 🧩 **Nonce-based CSP** — per-request cryptographic nonces for scripts and styles
- 📋 **Full CSP Level 3** (W3C Recommendation) — all 22 directives, nonce/hash support, `strict-dynamic`, `report-to`, `worker-src`, `frame-src`, `manifest-src`, `script-src-elem/attr`, `style-src-elem/attr`
- 🔮 **CSP Level 4 ready** — Trusted Types (`require-trusted-types-for`, `trusted-types`), `fenced-frame-src` (Privacy Sandbox)
- 🎯 **Fluent CSP Builder** — type-safe, chainable API with full XML documentation
- ⚡ **Zero-allocation nonce generation** — `stackalloc` + `RandomNumberGenerator`
- 🔌 **Extensible** — custom `IHeaderPolicy` implementations
- 📊 **CSP violation reporting** — built-in `/csp-report` endpoint using Reporting API v1

## Documentation

Full documentation: [github.com/MPCoreDeveloper/SafeWebCore/docs](https://github.com/MPCoreDeveloper/SafeWebCore/tree/master/docs)

## License

MIT — see [LICENSE](https://github.com/MPCoreDeveloper/SafeWebCore/blob/master/LICENSE)
