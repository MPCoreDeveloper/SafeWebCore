# SafeWebCore Documentation

Welcome to the SafeWebCore documentation. SafeWebCore is a .NET 10 middleware library that adds security headers to ASP.NET Core applications, targeting an **A+ rating** on [securityheaders.com](https://securityheaders.com) out of the box.

---

## 📖 Table of Contents

### Getting Started

- **[Getting Started](getting-started.md)** — Installation, minimal setup, and verifying your headers

### Guides

- **[Security Headers](security-headers.md)** — Every security header explained with values, rationale, and configuration
- **[CSP Configuration](csp-configuration.md)** — Content Security Policy builder, nonces, directives, and common scenarios
- **[Presets](presets.md)** — Strict A+ preset details, customization examples, and when-not-to-use guidance
- **[Advanced Configuration](advanced-configuration.md)** — Custom policies, CSP reporting, middleware ordering, troubleshooting

### Quick Reference

| I want to... | Go to |
|---------------|-------|
| Get A+ in one line | [Getting Started](getting-started.md) |
| Understand what each header does | [Security Headers](security-headers.md) |
| Configure CSP with nonces | [CSP Configuration](csp-configuration.md) |
| Customize the strict preset | [Presets](presets.md) |
| Add custom headers | [Advanced Configuration](advanced-configuration.md) |
| Set up CSP violation reporting | [Advanced Configuration](advanced-configuration.md#csp-violation-reporting) |
| Fix blocked resources | [Advanced Configuration](advanced-configuration.md#troubleshooting) |

### API Reference

API documentation is generated from XML comments on all public types. Key classes:

| Class | Purpose |
|-------|---------|
| `SecurePresets` | Pre-configured A+ security options |
| `NetSecureHeadersOptions` | All header configuration options |
| `CspOptions` | CSP directive configuration (record) |
| `CspBuilder` | Fluent CSP builder |
| `INonceService` | Nonce generation interface |
| `IHeaderPolicy` | Custom header policy interface |
| `NetSecureHeaders` | Constants (`CspNonceKey`) |
| `ServiceCollectionExtensions` | `AddNetSecureHeaders`, `AddNetSecureHeadersStrictAPlus` |
| `ApplicationBuilderExtensions` | `UseNetSecureHeaders`, `UseCspReport` |
| `CspNonceAttribute` | MVC action filter for nonce injection |
