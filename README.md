# 🛡️ SafeWebCore

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![securityheaders.com](https://img.shields.io/badge/securityheaders.com-A%2B-brightgreen)](https://securityheaders.com)

**SafeWebCore** is a lightweight, high-performance .NET 10 middleware library that adds security headers to your ASP.NET Core applications. It targets an **A+ rating** on [securityheaders.com](https://securityheaders.com) out of the box — zero configuration required.

---

## ✨ Features

- 🔒 **A+ in one line** — `AddNetSecureHeadersStrictAPlus()` configures the strictest security headers instantly
- 🧩 **Nonce-based CSP** — per-request cryptographic nonces for `script-src` and `style-src`
- 📋 **CSP Level 3** — Trusted Types, `strict-dynamic`, `script-src-elem/attr`, `style-src-elem/attr`, `worker-src`, `fenced-frame-src`
- 🎯 **Fluent CSP Builder** — type-safe, chainable API for building Content Security Policy
- ⚡ **Zero-allocation nonce generation** — `stackalloc` + `RandomNumberGenerator` on the hot path
- 🛑 **Server header removal** — hides server technology from attackers
- 🔌 **Extensible** — add custom `IHeaderPolicy` implementations for any header
- 📊 **CSP violation reporting** — built-in middleware for `/csp-report` endpoint

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

The preset is intentionally strict. Relax only what your app needs:

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    // Allow images from your CDN
    opts.Csp = opts.Csp with { ImgSrc = "'self' https://cdn.example.com" };

    // Allow API calls to your backend
    opts.Csp = opts.Csp with { ConnectSrc = "'self' https://api.example.com" };

    // Use strict-origin-when-cross-origin instead of no-referrer
    opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";
});
```

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

## 🤝 Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## 📝 Changelog

See [CHANGELOG.md](CHANGELOG.md) for release history.
