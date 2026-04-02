# SafeWebCore Documentation

Welcome to the SafeWebCore documentation. SafeWebCore is a .NET 10 middleware library that adds security headers to ASP.NET Core applications, targeting an **A+ rating** on [securityheaders.com](https://securityheaders.com) out of the box.

---

## 📖 Table of Contents

### Getting Started

- **[Getting Started](getting-started.md)** — Installation, minimal setup (v1.1.0+), verifying headers, using nonces

### Core Guides

- **[Security Headers](security-headers.md)** — Every header explained, defaults vs strict A+, header comparison table
- **[CSP Configuration](csp-configuration.md)** — Full CSP Level 3 & Level 4 reference, builder, nonces, performance optimizations (v1.1.0+)
- **[Presets](presets.md)** — All five presets (StrictAPlus, Api, Mvc, Blazor, SpaReverseProxy), comparison, decision guide
- **[Advanced Configuration](advanced-configuration.md)** — Path policies, report-only, CSP reporting, custom sinks, endpoint overrides, testing, troubleshooting

### Examples & Patterns

- **[Examples](examples.md)** — Three runnable projects (MinimalApi, MvcApp, ApiService) with feature matrix

### Performance & Reference

- **[Benchmarks](benchmarks.md)** — Running BenchmarkDotNet suites, result interpretation, creating new benchmarks

### Archived

- **[Archive](archive/)** — Obsolete v1.2 planning documents (implementation plan, roadmap)

---

## 📋 Quick Reference

| I want to... | Go to |
|---------------|-------|
| **Get A+ in one line** | [Getting Started](getting-started.md#minimal-setup-a-in-3-lines) |
| **Run a working example** | [Examples](examples.md) |
| **Configure everything custom** | [Getting Started](getting-started.md#fully-custom-setup) |
| **Access CSP nonce** | [Getting Started](getting-started.md#using-csp-nonces-v110) |
| **Understand what each header does** | [Security Headers](security-headers.md) |
| **Configure CSP with advanced directives** | [CSP Configuration](csp-configuration.md) |
| **Choose the right preset** | [Presets](presets.md#choosing-your-preset) |
| **Use path-based policies** | [Advanced Configuration](advanced-configuration.md#path-based-security-policies) |
| **Set up CSP violation reporting** | [Advanced Configuration](advanced-configuration.md#csp-violation-reporting-v110) |
| **Debug CSP violations** | [Advanced Configuration](advanced-configuration.md#troubleshooting) |
| **Create custom headers** | [Advanced Configuration](advanced-configuration.md#custom-header-policies) |
| **Add endpoint overrides** | [Advanced Configuration](advanced-configuration.md#endpoint-level-overrides) |
| **Test security headers** | [Advanced Configuration](advanced-configuration.md#testing-security-headers) |

---

## 🚀 Latest Features (v1.1.0+)

| Feature | Link |
|---------|------|
| Per-request nonce access via `HttpContext.GetCspNonce()` | [Getting Started](getting-started.md#using-csp-nonces-v110) |
| Zero-allocation nonce generation `TryWriteNonce(Span<char>)` | [Getting Started](getting-started.md#zero-allocation-nonce-generation-v110) |
| Pre-built CSP template (startup-only computation) | [CSP Configuration](csp-configuration.md#pre-built-csp-template-startup-only) |
| TagHelper nonce auto-injection | [Getting Started](getting-started.md#in-razor-views-with-taghelpers-v110) |
| All v1.2 features now shipped | [Presets](presets.md) and [Advanced Configuration](advanced-configuration.md) |

---

## 📚 API Reference

Generated from XML documentation comments. Key classes:

| Class | Purpose |
|-------|---------|
| `SecurePresets` | Pre-configured security option sets |
| `NetSecureHeadersOptions` | Root configuration for all headers |
| `CspOptions` | CSP directive configuration (C# record) |
| `CspBuilder` | Fluent API for CSP configuration |
| `ReferrerPolicyBuilder` | Typed builder for Referrer-Policy |
| `PermissionsPolicyBuilder` | Typed builder for Permissions-Policy |
| `CrossOriginPolicyBuilder` | Typed builder for COEP/COOP/CORP |
| `NonceService` | Nonce generation (`GenerateNonce()`, `TryWriteNonce()`) |
| `ICspReportSink` | Custom CSP violation handling |
| `IHeaderPolicy` | Custom header implementations |

---

## 🔗 Important Links

- **GitHub:** [MPCoreDeveloper/SafeWebCore](https://github.com/MPCoreDeveloper/SafeWebCore)
- **NuGet:** [SafeWebCore](https://www.nuget.org/packages/SafeWebCore)
- **Security Grades:**
  - [securityheaders.com](https://securityheaders.com) — Full header scanning
  - [Google CSP Evaluator](https://csp-evaluator.withgoogle.com/) — CSP analysis
- **Standards:**
  - [CSP Level 3 W3C Recommendation](https://www.w3.org/TR/CSP3/)
  - [CSP Level 4 Draft](https://w3c.github.io/webappsec-csp/)
  - [MDN: Content Security Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)
