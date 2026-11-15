# SafeWebCore Documentation

Welcome to the SafeWebCore documentation. SafeWebCore is a **.NET 10 and .NET 11** middleware library that adds security headers to ASP.NET Core applications, targeting an **A+ rating** on [securityheaders.com](https://securityheaders.com) out of the box.

---

## 📘 The manual

The manual is the book-length documentation: 22 chapters that go from the first A+ response to the architecture, the
companion packages and a cookbook.

- **[Read or download the manual as a PDF](SafeWebCore-Manual-1.9.0.pdf)** — 59 pages, A4, with a cover and a contents page.
- **Read the chapters as Markdown** in [`manual/src`](manual/src). Each file is one chapter, in reading order, and it is
  the source of both the GitHub documentation and the PDF.
- **Build it yourself:** `dotnet run --project .\tools\SafeWebCore.Manual`. Chrome or Edge is the only prerequisite beyond
  the .NET SDK; Chapter 20 of the manual describes the pipeline.

| Chapter | What it covers |
|---------|----------------|
| [Preface](manual/src/00-preface.md) | Who the manual is for, how it is organised, and the conventions every chapter uses |
| [1. What SafeWebCore Is](manual/src/01-what-it-is.md) | The six questions the library answers, one middleware, and the nonce as the hinge |
| [2. What SafeWebCore Is Not](manual/src/02-what-it-is-not.md) | The deliberate boundary: not a WAF, no HTML rewriting, no report storage |
| [3. Installation and the First A+ Response](manual/src/03-installation.md) | Install, two calls, read the headers back, and a first test |
| [4. Presets](manual/src/04-presets.md) | Every preset, what each emphasises, and preset-as-path-policy |
| [5. The Options Model](manual/src/05-the-options-model.md) | The whole options shape, the defaults, and startup validation |
| [6. Content Security Policy](manual/src/06-content-security-policy.md) | The directive record, `{nonce}`, the builder, Trusted Types, reporting |
| [7. Nonces and Tag Helpers](manual/src/07-nonces-and-tag-helpers.md) | Generation, `GetCspNonce()`, the Razor helpers, and `'strict-dynamic'` |
| [8. Path Policies](manual/src/08-path-policies.md) | Longest-prefix matching, normalisation, and why duplicates fail |
| [9. Endpoint Overrides](manual/src/09-endpoint-overrides.md) | `SkipNetSecureHeaders`, `WithCspMode`, and the precedence order |
| [10. Report-Only Rollout and Reporting](manual/src/10-report-only-and-reporting.md) | Report-only, `/csp-report`, sinks, NEL, and a rollout that survives review |
| [11. The Diagnostics Endpoint](manual/src/11-diagnostics.md) | Every field of the preview, and how to protect it |
| [12. Telemetry: Events and Metrics](manual/src/12-telemetry.md) | `SecurityEvent`, sinks, the four counters, and what to alert on |
| [13. Custom Policies, Additional Headers and Validation](manual/src/13-custom-policies-and-validation.md) | `IHeaderPolicy`, the header order, the typed builders, the validator |
| [14. Configuration Binding](manual/src/14-configuration-binding.md) | `AddNetSecureHeadersFromConfiguration`, a full `appsettings` example |
| [15. SafeWebCore.FraudDetection](manual/src/15-fraud-detection.md) | Signals, the report, pen-test authorisation, injectable notifications |
| [16. SafeWebCore.JwtBearer](manual/src/16-jwt-bearer.md) | The authority guard, token hardening, and what the package refuses to be |
| [17. Analyzers and the Testing Package](manual/src/17-analyzers-and-testing.md) | `SWC001`–`SWC004`, the header assertions, the test host |
| [18. Architecture and Compatibility](manual/src/18-architecture-and-compatibility.md) | Layers, one resolver, state, the compatibility promise and how it is proven |
| [19. Cookbook](manual/src/19-cookbook.md) | Complete recipes: SPA + API, Blazor, CDN, Swagger, health, tests |
| [20. How This Manual Is Built](manual/src/20-how-this-manual-is-built.md) | The three stages, the Markdown subset, and what the build does not do |
| [21. Option Reference](manual/src/21-option-reference.md) | Every option, its default, and where it is explained |

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
- **[Recipes](recipes/README.md)** — Practical guides for common scenarios (MVC+CDN, Swagger, Blazor, Report-Only, Reverse Proxy, etc.)
- **[Roadmap](roadmap.md)** — Planned work for `v1.4` through `v1.6`, focused on DX, diagnostics, tooling, and observability
- **[Backward Compatibility Policy](development/backward-compatibility-policy.md)** — Rules for additive-only changes, stable defaults, and supported upgrade paths
- **[Spec Kit Workflow](development/spec-kit-workflow.md)** — AI-assisted, spec-driven development setup (Spec Kit constitution, `.specify/` scaffolding, slash commands, guardrails)
- **[Architecture Spine](development/architecture/ARCHITECTURE-SPINE.md)** — The design paradigm, the adopted architecture decisions (AD-1 to AD-19) and what they prevent
- **[SonarCloud Triage](development/sonarcloud-triage.md)** — Analysis scope, triage rules, and the accepted findings that keep the SonarCloud issue list at zero
- **[Mutation Testing](development/mutation-testing.md)** — Stryker.NET setup, how to run a suite, the baseline scores per package and what the surviving mutants mean

### Repository, packages & release

- **[Project Catalog](projects.md)** — Every project documented (libraries, tests, examples, benchmarks)
- **[NuGet Packages & Candidates](nuget-packages.md)** — Packable packages, nuget.org status, readiness, publish order
- **[Release Readiness](release-readiness.md)** — Audit results, go/no-go matrix, blockers, release checklist

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
| **Preview effective headers and path-policy behavior** | [Advanced Configuration](advanced-configuration.md#diagnostics-endpoint-preview-v14) |
| **Test security headers** | [Advanced Configuration](advanced-configuration.md#testing-security-headers) |
| **Use testing helpers** | [SafeWebCore.Testing package](https://www.nuget.org/packages/SafeWebCore.Testing) (preview) |
| **Understand planned roadmap work** | [Roadmap](roadmap.md) |
| **Review compatibility rules before contributing** | [Backward Compatibility Policy](development/backward-compatibility-policy.md) |
| **Triage a SonarCloud finding (or check the accepted ones)** | [SonarCloud Triage](development/sonarcloud-triage.md) |
| **Measure how much the tests actually catch** | [Mutation Testing](development/mutation-testing.md) |
| **Set up or use AI agents (Spec Kit)** | [Spec Kit Workflow](development/spec-kit-workflow.md) |
| **See all projects in the repo** | [Project Catalog](projects.md) |
| **Understand NuGet candidates** | [NuGet Packages & Candidates](nuget-packages.md) |
| **Check if ready to release** | [Release Readiness](release-readiness.md) |

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
