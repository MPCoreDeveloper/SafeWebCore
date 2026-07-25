# 🛡️ SafeWebCore

[![NuGet](https://img.shields.io/nuget/v/SafeWebCore.svg?logo=nuget)](https://www.nuget.org/packages/SafeWebCore)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SafeWebCore.svg?logo=nuget)](https://www.nuget.org/packages/SafeWebCore)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![securityheaders.com](https://img.shields.io/badge/securityheaders.com-A%2B-brightgreen)](https://securityheaders.com)
[![CSP](https://img.shields.io/badge/CSP-Level%203%20%2B%20Level%204-blue?logo=w3c)](https://www.w3.org/TR/CSP3/)
[![Sponsor](https://img.shields.io/badge/Sponsor-❤-ea4aaa?logo=githubsponsors)](https://github.com/sponsors/MPCoreDeveloper)

**SafeWebCore** is a lightweight, high-performance .NET 10 middleware library that adds security headers to your ASP.NET Core applications. It targets an **A+ rating** on [securityheaders.com](https://securityheaders.com) out of the box — zero configuration required.

---

## ✨ Features

- 🔒 **A+ in one line** — `AddNetSecureHeadersStrictAPlus()` configures the strictest security headers instantly
- 🧭 **App-profile presets** — ready-made profiles for API, MVC, Blazor, and SPA reverse-proxy apps
- 🛠️ **Fully custom** — `AddNetSecureHeaders(opts => { ... })` gives you complete control over every header
- ⚙️ **Configuration binding** — `AddNetSecureHeadersFromConfiguration(...)` binds `NetSecureHeadersOptions` directly from configuration
- 🌦️ **Environment-aware rollout** — opt-in helpers can default CSP to report-only outside production for safer rollout
- 🧩 **Nonce-based CSP** — per-request cryptographic nonces for `script-src` and `style-src`
- 🧷 **Razor nonce TagHelpers** — auto-inject nonce attributes on `<script>` and `<style>` when available
- 🛣️ **Path-based policies** — apply different security profiles per route prefix with longest-prefix matching
- 🎯 **Endpoint metadata overrides** — skip headers or force CSP report-only per endpoint
- 🧪 **Startup configuration validation** — invalid combinations fail fast during startup
- 📝 **CSP Report-Only support** — ship policies safely before enforcing
- 🧱 **Typed policy builders** — strongly typed builders for `Referrer-Policy`, `Permissions-Policy`, and COEP/COOP/CORP values
- 🧰 **Optional additional headers** — opt-in support for `Origin-Agent-Cluster`, `X-Robots-Tag`, and `Clear-Site-Data`
- 📋 **Full CSP Level 3** (W3C Recommendation) — all directives including `worker-src`, `manifest-src`, `frame-src`, `script-src-elem/attr`, `style-src-elem/attr`, `report-to`, nonce/hash support, `strict-dynamic`
- 🔮 **CSP Level 4 ready** — Trusted Types (`require-trusted-types-for`, `trusted-types`), `fenced-frame-src` (Privacy Sandbox)
- 🎯 **Fluent CSP Builder** — type-safe, chainable API with full XML documentation for every directive
- ⚡ **Zero-allocation nonce generation** — `stackalloc` + `RandomNumberGenerator` on the hot path, plus `TryWriteNonce(Span<char>)` for fully heap-free scenarios
- 🔍 **`HttpContext.GetCspNonce()`** — discoverable extension method to retrieve the per-request nonce
- 🛑 **Server header removal** — hides server technology from attackers
- 🔌 **Extensible** — add custom `IHeaderPolicy` implementations for any header
- 📊 **CSP violation reporting** — built-in middleware for `/csp-report` endpoint using Reporting API v1

### Typed builders for non-CSP headers

```csharp
using SafeWebCore.Builder;

builder.Services.AddNetSecureHeaders(opts =>
{
    opts.ReferrerPolicyValue = new ReferrerPolicyBuilder()
        .StrictOriginWhenCrossOrigin()
        .Build();

    opts.PermissionsPolicyValue = new PermissionsPolicyBuilder()
        .Disable(PermissionsFeature.Camera)
        .Disable(PermissionsFeature.Microphone)
        .AllowSelf(PermissionsFeature.Geolocation)
        .Build();

    var crossOrigin = new CrossOriginPolicyBuilder()
        .CoepRequireCorp()
        .CoopSameOrigin()
        .CorpSameOrigin()
        .Build();

    opts.CoepValue = crossOrigin.Coep;
    opts.CoopValue = crossOrigin.Coop;
    opts.CorpValue = crossOrigin.Corp;
});
```

### Optional additional headers

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.EnableOriginAgentCluster = true;
    opts.OriginAgentClusterValue = "?1";

    opts.EnableXRobotsTag = true;
    opts.XRobotsTagValue = "noindex, nofollow";

    opts.EnableClearSiteData = true;
    opts.ClearSiteDataValue = "\"cache\", \"cookies\", \"storage\"";
});
```

### CSP Compliance

| Standard | Status | Coverage |
|----------|--------|----------|
| **CSP Level 3** (W3C Recommendation) | ✅ Full | All 22 directives, nonce/hash, `strict-dynamic`, `report-to` |
| **CSP Level 4** (Emerging) | ✅ Ready | Trusted Types, `fenced-frame-src` (Privacy Sandbox) |

---

## 🆕 What's New in v1.6.0

v1.6.0 is a **developer experience, tooling, and observability** release — fully backwards compatible with v1.3.5 and earlier.

| Improvement | Detail |
|-------------|--------|
| **Configuration-based setup** | `AddNetSecureHeadersFromConfiguration(...)` binds `NetSecureHeadersOptions` from `IConfiguration` or a specific section |
| **Environment-aware rollout** | `AddNetSecureHeadersForEnvironment(...)` and `AddNetSecureHeadersStrictAPlusForEnvironment(...)` can default CSP to report-only outside production |
| **Diagnostics preview** | `MapSafeWebCoreDiagnostics(...)` exposes an opt-in JSON preview of effective headers, path-policy resolution, and CSP mode |
| **Metrics** | Opt-in `System.Diagnostics.Metrics` counters for core middleware and fraud detection |
| **Fraud action pipeline** | `IFraudEventSink` / `FraudEvent` for reacting to fraud analysis results (logging, webhooks, custom actions) |
| **New companion packages** | `SafeWebCore.FraudDetection` 1.0.0, `SafeWebCore.Analyzers` preview, and `SafeWebCore.Testing` preview |
| **Better startup validation** | Validation messages now include concrete remediation guidance for CSP mode, path prefixes, additional headers, and reporting endpoints |

See the full [CHANGELOG](CHANGELOG.md) for details.


---

## 🚀 Quick Start

### 1. Install

```bash
dotnet add package SafeWebCore
```

### 2. One-line A+ setup (recommended)

```csharp
using SafeWebCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Adds ALL security headers with the strictest A+ configuration
builder.Services.AddNetSecureHeadersStrictAPlus();

var app = builder.Build();

app.UseNetSecureHeaders();
app.MapGet("/", () => "Hello, secure world!");

app.Run();
```

That's it! Your application now returns these headers on every response:

| Header | Value |
|--------|-------|
| `Strict-Transport-Security` | `max-age=63072000; includeSubDomains; preload` |
| `X-Frame-Options` | `DENY` |
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `no-referrer` |
| `Permissions-Policy` | All recognized features denied (scanner-safe) |
| `Cross-Origin-Embedder-Policy` | `require-corp` |
| `Cross-Origin-Opener-Policy` | `same-origin` |
| `Cross-Origin-Resource-Policy` | `same-origin` |
| `X-DNS-Prefetch-Control` | `off` |
| `X-Permitted-Cross-Domain-Policies` | `none` |
| `Content-Security-Policy` | Nonce-based, strict-dynamic, Trusted Types |
| `Server` | _(removed)_ |
| `X-Powered-By` | _(removed)_ |

### 3. Strict A+ with customization

The preset is intentionally strict. Relax only what your app needs.
CSP directives are **space-separated** — add multiple origins in a single string:

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    // Multiple CDNs — just separate with spaces
    opts.Csp = opts.Csp with { ImgSrc = "'self' https://cdn1.example.com https://cdn2.example.com data:" };

    // Multiple directives at once using 'with { ... }'
    opts.Csp = opts.Csp with
    {
        ConnectSrc = "'self' https://api.example.com wss://ws.example.com",
        FontSrc = "'self' https://fonts.gstatic.com https://cdn.example.com"
    };

    // Non-CSP headers are simple string properties
    opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";
});
```

> 💡 **Tip:** Each CSP directive is one string with space-separated sources. Use a single `with { ... }` block to change multiple directives at once.

### 4. Full manual configuration

For complete control, use `AddNetSecureHeaders` with the fluent CSP builder:

```csharp
using SafeWebCore.Builder;
using SafeWebCore.Extensions;

builder.Services.AddNetSecureHeaders(opts =>
{
    opts.EnableHsts = true;
    opts.HstsValue = "max-age=31536000; includeSubDomains";

    opts.EnableXFrameOptions = true;
    opts.XFrameOptionsValue = "SAMEORIGIN";

    opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";

    // Use the fluent CSP builder
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
```

---

## 🧭 v1.5 Tooling (Current Workspace)

The current workspace includes the completed `v1.5` tooling features (additive, opt-in, 100% backward compatible):

### Analyzers (`SafeWebCore.Analyzers`)

- **SWC001**: Registration without `UseNetSecureHeaders()`
- **SWC002**: Permanent `UseCspReportOnly = true`
- **SWC003**: `'unsafe-inline'` without nonce
- **SWC004**: Overly broad CSP sources

### Testing Helpers (`SafeWebCore.Testing`)

- `AssertHasSecurityHeaders()`
- `AssertHasCspEnforceMode()` / `AssertHasCspReportOnlyMode()`
- `AssertHasNonceInCsp()` / `AssertHasNoNonceInCsp()`
- Bootstrap helpers for `TestServer`

See `docs/recipes/` for practical examples.

---

## 🧭 v1.2 Milestone Progress

The following features are now implemented from the `v1.2` plan.

### CSP Report-Only mode

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.UseCspReportOnly = true;
});
```

This emits `Content-Security-Policy-Report-Only` instead of enforce-mode `Content-Security-Policy`.

### Path-based policy overrides

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.PathPolicies.Add(new PathPolicyOptions
    {
        PathPrefix = "/api",
        Options = new NetSecureHeadersOptions
        {
            ReferrerPolicyValue = "no-referrer",
            UseCspReportOnly = true
        }
    });
});
```

Path policies are matched by prefix and the longest matching prefix wins.

### Startup validation

SafeWebCore validates options during startup and fails fast for invalid configurations, for example:
- `UseCspReportOnly = true` while `EnableCsp = false`
- duplicate path prefixes (normalized)
- empty path policy prefixes

### Razor nonce TagHelpers

Register the TagHelpers in your Razor `_ViewImports.cshtml`:

```razor
@addTagHelper *, SafeWebCore
```

Then use normal tags; nonce is added automatically when available:

```html
<script>
    console.log("nonce is injected automatically");
</script>

<style>
    body { font-family: sans-serif; }
</style>
```

## 🔑 Using CSP Nonces in Razor Views

SafeWebCore generates a unique cryptographic nonce per request. Use it in your scripts and styles:

### With the `[CspNonce]` attribute

```csharp
using SafeWebCore.Attributes;

[CspNonce]
public class HomeController : Controller
{
    public IActionResult Index() => View();
}
```

```html
<!-- In your Razor view -->
<script nonce="@ViewData["CspNonce"]">
    console.log("This script is allowed by CSP");
</script>

<style nonce="@ViewData["CspNonce"]">
    body { font-family: sans-serif; }
</style>
```

### Direct access via `GetCspNonce()` extension *(v1.1.0+)*

```csharp
using SafeWebCore.Extensions;

// In Minimal API
app.MapGet("/page", (HttpContext ctx) =>
{
    var nonce = ctx.GetCspNonce();
    return Results.Content(
        $"<script nonce=\"{nonce}\">console.log('nonce ok');</script>",
        "text/html");
});

// In a controller action
public IActionResult Index()
{
    ViewData["CspNonce"] = HttpContext.GetCspNonce();
    return View();
}
```

---

## ⚡ Benchmarks

SafeWebCore ships a [BenchmarkDotNet](https://benchmarkdotnet.org/) suite covering nonce generation, CSP header assembly, typed policy builders, preset instantiation, the middleware pipeline, and CSP report parsing.

```bash
cd benchmarks/SafeWebCore.Benchmarks
dotnet run -c Release
```

See **[docs/benchmarks.md](docs/benchmarks.md)** for scenario descriptions, running instructions, and result interpretation.

---

## 📖 Examples

Three complete, runnable ASP.NET Core applications demonstrating different integration patterns:

| Example | Framework | Key Features |
|---------|-----------|--------------|
| [**MinimalApi**](examples/MinimalApi/) | Minimal API | One-line A+ setup, inline nonce, CSP reporting, health probes |
| [**MvcApp**](examples/MvcApp/) | MVC + Razor Views | Typed policy builders, path policies, nonce TagHelpers, controller attributes |
| [**ApiService**](examples/ApiService/) | Web API Controllers | Custom CSP report sink, endpoint overrides, API preset |

Each example is fully functional out of the box — just `dotnet run` from the example directory.

```bash
# Try each example
cd examples/MinimalApi && dotnet run
cd examples/MvcApp && dotnet run
cd examples/ApiService && dotnet run
```

See **[examples/README.md](examples/README.md)** for a detailed overview and feature matrix.

---

## 📚 Documentation

| Guide | Description |
|-------|-------------|
| [Getting Started](docs/getting-started.md) | Installation, minimal setup, and verifying your headers |
| [Examples](docs/examples.md) | Three complete sample projects (Minimal API, MVC, Web API) |
| [Security Headers](docs/security-headers.md) | Every security header explained with values and rationale |
| [CSP Configuration](docs/csp-configuration.md) | CSP builder, nonces, directives, and common scenarios |
| [Presets](docs/presets.md) | Strict A+ and app-profile presets, customization examples |
| [Advanced Configuration](docs/advanced-configuration.md) | Custom policies, CSP reporting, endpoint overrides, troubleshooting |
| [Benchmarks](docs/benchmarks.md) | Running benchmarks and interpreting results |

---

## Examples

The [examples/](examples/) directory contains three fully runnable ASP.NET Core applications demonstrating different integration patterns.

| Example | Pattern | Highlights |
|---------|---------|-----------|
| [MinimalApi](examples/MinimalApi/) | Minimal API | StrictAPlus preset, nonce, SkipNetSecureHeaders |
| [MvcApp](examples/MvcApp/) | MVC + Razor | Typed builders, path policies, CspNonce attribute, TagHelpers |
| [ApiService](examples/ApiService/) | Web API | API preset, custom ICspReportSink, endpoint overrides |


