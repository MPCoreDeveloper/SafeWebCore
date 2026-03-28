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
- 🛠️ **Fully custom** — `AddNetSecureHeaders(opts => { ... })` gives you complete control over every header
- 🧩 **Nonce-based CSP** — per-request cryptographic nonces for `script-src` and `style-src`
- 📋 **Full CSP Level 3** (W3C Recommendation) — all directives including `worker-src`, `manifest-src`, `frame-src`, `script-src-elem/attr`, `style-src-elem/attr`, `report-to`, nonce/hash support, `strict-dynamic`
- 🔮 **CSP Level 4 ready** — Trusted Types (`require-trusted-types-for`, `trusted-types`), `fenced-frame-src` (Privacy Sandbox)
- 🎯 **Fluent CSP Builder** — type-safe, chainable API with full XML documentation for every directive
- ⚡ **Zero-allocation nonce generation** — `stackalloc` + `RandomNumberGenerator` on the hot path
- 🛑 **Server header removal** — hides server technology from attackers
- 🔌 **Extensible** — add custom `IHeaderPolicy` implementations for any header
- 📊 **CSP violation reporting** — built-in middleware for `/csp-report` endpoint using Reporting API v1

### CSP Compliance

| Standard | Status | Coverage |
|----------|--------|----------|
| **CSP Level 3** (W3C Recommendation) | ✅ Full | All 22 directives, nonce/hash, `strict-dynamic`, `report-to` |
| **CSP Level 4** (Emerging) | ✅ Ready | Trusted Types, `fenced-frame-src` (Privacy Sandbox) |
| Deprecated directives | ✅ Handled | `report-uri`, `block-all-mixed-content` marked `[Obsolete]` |

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
| `Permissions-Policy` | All features denied |
| `Cross-Origin-Embedder-Policy` | `require-corp` |
| `Cross-Origin-Opener-Policy` | `same-origin` |
| `Cross-Origin-Resource-Policy` | `same-origin` |
| `X-DNS-Prefetch-Control` | `off` |
| `X-Permitted-Cross-Domain-Policies` | `none` |
| `Content-Security-Policy` | Nonce-based, strict-dynamic, Trusted Types |
| `Server` | _(removed)_ |

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

### Direct access via `HttpContext.Items`

```csharp
var nonce = HttpContext.Items[NetSecureHeaders.CspNonceKey] as string;
```

---

## 📊 CSP Violation Reporting

Enable the built-in CSP report endpoint to catch policy violations:

```csharp
var app = builder.Build();

app.UseCspReport();           // Handles POST /csp-report
app.UseNetSecureHeaders();

app.Run();
```

Configure the CSP to send reports:

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.Csp = opts.Csp with { ReportTo = "default" };
});
```

Violations are logged at `Warning` level via `ILogger`.

---

## 📁 Project Structure

```
SafeWebCore/
├── src/SafeWebCore/
│   ├── Abstractions/          # IHeaderPolicy interface
│   ├── Attributes/            # [CspNonce] action filter
│   ├── Builder/               # Fluent CspBuilder
│   ├── Constants/             # Header name constants
│   ├── Extensions/            # DI and middleware extensions
│   ├── Infrastructure/        # NonceService, CspReportMiddleware
│   ├── Middleware/             # NetSecureHeadersMiddleware
│   ├── Options/               # NetSecureHeadersOptions, CspOptions
│   └── Presets/               # SecurePresets (Strict A+)
├── tests/SafeWebCore.Tests/   # xUnit v3 tests
├── docs/                      # Documentation
└── .github/                   # CI, issue templates
```

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [Getting Started](docs/getting-started.md) | Installation, first setup, verification |
| [Security Headers](docs/security-headers.md) | Every header explained with rationale |
| [CSP Configuration](docs/csp-configuration.md) | CSP builder, nonces, directives guide |
| [Presets](docs/presets.md) | Strict A+ preset details and customization |
| [Advanced Configuration](docs/advanced-configuration.md) | Custom policies, reporting, per-route config |

---

## 🏗️ Building & Testing

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet tool install -g dotnet-coverage
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test
```

---

## ✅ Validate Your Security Headers

After deploying your application, verify your headers with these tools:

### 1. [securityheaders.com](https://securityheaders.com/)

Scans **all** response headers and grades your site **A+** through **F**. Validates HSTS, CSP, X-Frame-Options, Permissions-Policy, Referrer-Policy, and more.

> With SafeWebCore's Strict A+ preset you should score **A+** immediately.

### 2. [Google CSP Evaluator](https://csp-evaluator.withgoogle.com/)

Paste your `Content-Security-Policy` header value to check for common CSP misconfigurations:
- ❌ Missing `object-src 'none'`
- ❌ `'unsafe-inline'` without a nonce or hash
- ❌ Missing `'strict-dynamic'` for trust propagation
- ❌ Overly permissive wildcards (`*`, `https:`)
- ✅ Nonce-based policy with `'strict-dynamic'` (SafeWebCore default)

### 3. Browser DevTools

Open **DevTools → Network tab → Response Headers** to inspect headers on every request. Any CSP violations will also appear in the **Console** tab.

---

## 🤝 Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## 📝 Changelog

See [CHANGELOG.md](CHANGELOG.md) for release history.
