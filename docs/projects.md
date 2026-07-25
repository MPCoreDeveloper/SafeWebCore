# SafeWebCore — Project Catalog

Complete inventory of every project in this repository: purpose, target framework, dependencies, packaging status, and how it fits the product.

**Last audited:** 2026-07-25  
**Solution file:** `SafeWebCore.slnx`  
**SDK:** .NET 10 (`10.0.302` verified locally)

---

## Solution layout

```text
SafeWebCore/
├── src/                          # Shipable libraries
│   ├── SafeWebCore/              # Core security-headers middleware (NuGet)
│   ├── SafeWebCore.FraudDetection/  # Optional fraud module (NuGet candidate)
│   ├── SafeWebCore.Analyzers/    # Roslyn analyzers (NuGet preview candidate)
│   └── SafeWebCore.Testing/      # Test helpers (NuGet preview candidate)
├── tests/                        # Unit / integration tests
│   ├── SafeWebCore.Tests/
│   └── SafeWebCore.FraudDetection.Tests/
├── examples/                     # Runnable sample apps (not packed)
│   ├── MinimalApi/
│   ├── MvcApp/
│   └── ApiService/
├── benchmarks/                   # BenchmarkDotNet suites (not packed)
│   └── SafeWebCore.Benchmarks/
├── docs/                         # Product + contributor documentation
├── artifacts/                    # Local pack output
└── SafeWebCore.slnx              # Primary solution
```

### Projects currently in `SafeWebCore.slnx`

| Project | Path |
|---------|------|
| SafeWebCore | `src/SafeWebCore/SafeWebCore.csproj` |
| SafeWebCore.FraudDetection | `src/SafeWebCore.FraudDetection/SafeWebCore.FraudDetection.csproj` |
| SafeWebCore.Analyzers | `src/SafeWebCore.Analyzers/SafeWebCore.Analyzers.csproj` |
| SafeWebCore.Testing | `src/SafeWebCore.Testing/SafeWebCore.Testing.csproj` |
| SafeWebCore.Tests | `tests/SafeWebCore.Tests/SafeWebCore.Tests.csproj` |

### Projects **not** in the solution (still buildable standalone)

| Project | Path | Notes |
|---------|------|-------|
| SafeWebCore.FraudDetection.Tests | `tests/SafeWebCore.FraudDetection.Tests/` | Should be added for contributor hygiene |
| SafeWebCore.Benchmarks | `benchmarks/SafeWebCore.Benchmarks/` | Optional performance project |
| ApiService | `examples/ApiService/` | Sample only |
| MinimalApi | `examples/MinimalApi/` | Sample only |
| MvcApp | `examples/MvcApp/` | Sample only |

---

## Shared build defaults

From root `Directory.Build.props` (applies to all projects unless overridden):

| Setting | Value |
|---------|--------|
| `LangVersion` | `preview` |
| `Nullable` | `enable` |
| `ImplicitUsings` | `enable` |
| `TreatWarningsAsErrors` | `true` |
| `AnalysisLevel` | `latest-recommended` |

Coding standards are further enforced by `.editorconfig`.

---

# Library projects (`src/`)

## 1. SafeWebCore

| Attribute | Value |
|-----------|--------|
| **Path** | `src/SafeWebCore/` |
| **PackageId** | `SafeWebCore` |
| **Version (csproj)** | `1.3.5` |
| **TFM** | `net10.0` |
| **IsPackable** | Yes (default) |
| **Published on nuget.org** | Yes — `1.0.0` … `1.3.5` |
| **Symbols** | Yes (`snupkg`) |
| **Package readme** | Root `PACKAGE.md` |
| **Icon** | Root `icon.png` |
| **License** | MIT |
| **Source files** | ~40 C# files |
| **Public API baseline** | `PublicAPI.Shipped.txt` (~430) + `PublicAPI.Unshipped.txt` (~11) |

### Purpose

Lightweight, high-performance ASP.NET Core middleware that emits security headers targeting an **A+** grade on [securityheaders.com](https://securityheaders.com). Zero-config Strict A+ preset plus full custom configuration.

### Primary responsibilities

- Middleware pipeline integration (`UseNetSecureHeaders`)
- Service registration presets (`AddNetSecureHeaders*`)
- CSP Level 3 + Level 4-ready fluent builder and options
- Per-request cryptographic nonces + Razor TagHelpers
- Path-based policies and endpoint metadata overrides
- CSP violation reporting (`/csp-report`)
- Optional diagnostics endpoint, metrics, and security event sinks
- Startup options validation with actionable error messages

### Key namespaces / areas

| Area | Namespace / folder | Highlights |
|------|--------------------|------------|
| Middleware | `SafeWebCore.Middleware` | `NetSecureHeadersMiddleware` |
| Registration | `SafeWebCore.Extensions` | `AddNetSecureHeaders*`, `UseNetSecureHeaders`, env helpers, diagnostics map |
| Options | `SafeWebCore.Options` | `NetSecureHeadersOptions`, `CspOptions`, path/reporting/additional headers |
| Builders | `SafeWebCore.Builder` | `CspBuilder`, `ReferrerPolicyBuilder`, `PermissionsPolicyBuilder`, CO* builders |
| Presets | `SafeWebCore.Presets` | `StrictAPlus`, `Api`, `Mvc`, `Blazor`, `SpaReverseProxy` |
| CSP report | `SafeWebCore.Infrastructure` | `CspReportMiddleware`, sinks, validators, metrics |
| TagHelpers | `SafeWebCore.TagHelpers` | Script/style nonce injection |
| Attributes | `SafeWebCore.Attributes` | `[CspNonce]`, `[SkipNetSecureHeaders]`, CSP mode |
| Abstractions | `SafeWebCore.Abstractions` | `IHeaderPolicy`, `ICspReportSink`, `ISecurityEventSink` |

### Dependencies

- Framework reference: `Microsoft.AspNetCore.App`
- Build-only: `Microsoft.CodeAnalysis.PublicApiAnalyzers` `5.6.0` (`PrivateAssets=All`)

### Consumers

- All three example apps
- `SafeWebCore.Testing` (project reference)
- `SafeWebCore.Benchmarks` (project reference)
- `SafeWebCore.Tests`

### Documentation

- Root: `README.md`, `PACKAGE.md`, `CHANGELOG.md`
- Guides: `docs/getting-started.md`, `security-headers.md`, `csp-configuration.md`, `presets.md`, `advanced-configuration.md`
- Recipes: `docs/recipes/*`

### Version reality check

The **published** package identity is still **1.3.5**. The workspace contains substantial **Unreleased** work (config binding, environment helpers, diagnostics, metrics, analyzers, testing package) intended for a future **1.4+** line. Do not treat workspace HEAD as identical to nuget.org 1.3.5 without reading `CHANGELOG.md` `[Unreleased]`.

---

## 2. SafeWebCore.FraudDetection

| Attribute | Value |
|-----------|--------|
| **Path** | `src/SafeWebCore.FraudDetection/` |
| **PackageId** | `SafeWebCore.FraudDetection` |
| **Version (csproj)** | `1.0.0` |
| **TFM** | `net10.0` |
| **IsPackable** | Yes |
| **Published on nuget.org** | **No** (404 as of audit) |
| **Symbols** | Not configured (`IncludeSymbols` not set) |
| **Package readme** | `src/SafeWebCore.FraudDetection/README.md` |
| **Icon** | None |
| **License** | MIT |
| **Source files** | ~36 C# files |
| **Public API baseline** | Shipped ~252 + Unshipped ~49 |

### Purpose

Optional advanced fraud-detection module. Analyzes client fingerprints for geo-cultural inconsistency, legacy Western-impersonation signals, and pen-test / scanner traffic. Fully additive; SafeWebCore core does not depend on it.

### Primary responsibilities

- `IFraudDetector.Analyze(ClientFingerprintData)` → `FraudReport`
- Neutral geo-cultural consistency detector (recommended)
- Legacy Western impersonation detector (kept for compatibility)
- Pen-test authorization bypass + notification pipeline
- Options pattern + optional DB-backed `IFraudDetectionConfigurationStore`
- Optional geo-IP enrichment via `IGeoIpService`
- Fraud event sinks (`IFraudEventSink`), logging sink, webhook sink
- Opt-in metrics (`SafeWebCore.FraudDetection` meter)

### Key namespaces / areas

| Area | Folder | Highlights |
|------|--------|------------|
| Abstractions | `Abstractions/` | Detector, sinks, geo-IP, config store, pen-test notify contracts |
| Detection | `Detection/` | Geo + Western detectors, scorer, travel-mode evaluator |
| Models | `Models/` | Fingerprint, report, verdict, risk score/level, triggers |
| Options | `Options/` | Fraud, geo-cultural, Western, pen-test options |
| Extensions | `Extensions/` | DI registration, fingerprint enrichment, webhook helper |
| Infrastructure | `Infrastructure/` | Options resolver/validator, dispatchers, metrics, sinks |

### Dependencies

- Framework reference: `Microsoft.AspNetCore.App`
- Build-only: `Microsoft.CodeAnalysis.PublicApiAnalyzers` `5.6.0`
- **No** project reference to `SafeWebCore` (intentionally decoupled)

### Package readiness notes

- README is thorough and packable
- Missing: package icon, symbol package, `PackageReleaseNotes`, SourceLink flags (present on core)
- First public release candidate at `1.0.0` once docs/versioning/release checklist are finalized

### Consumers

- `SafeWebCore.FraudDetection.Tests`
- End applications that opt in via registration helpers (no examples in repo yet)

### Documentation

- Package README: `src/SafeWebCore.FraudDetection/README.md`
- Roadmap fraud epics: `docs/roadmap.md` (v1.6)


---

## 3. SafeWebCore.Analyzers

| Attribute | Value |
|-----------|--------|
| **Path** | `src/SafeWebCore.Analyzers/` |
| **PackageId** | `SafeWebCore.Analyzers` |
| **Version (csproj)** | `1.0.0-preview.1` |
| **TFM** | `netstandard2.0` (Roslyn analyzer convention) |
| **IsPackable** | Yes (`IncludeBuildOutput=false`, analyzer DLL under `analyzers/dotnet/cs`) |
| **Published on nuget.org** | **No** |
| **Package readme** | `src/SafeWebCore.Analyzers/README.md` |
| **Icon** | None |
| **License** | MIT |
| **Source files** | 5 C# files |

### Purpose

Roslyn analyzers that catch common SafeWebCore integration mistakes at **build time**. Additive and opt-in; does not change runtime behavior.

### Rules (current)

| Id | Summary |
|----|---------|
| **SWC001** | Services registered without `UseNetSecureHeaders()` |
| **SWC002** | Permanent `UseCspReportOnly = true` |
| **SWC003** | `'unsafe-inline'` without nonce |
| **SWC004** | Overly broad CSP sources (`*`, bare `https:`, `unsafe-eval`) |

### Dependencies

- `Microsoft.CodeAnalysis.Analyzers` `5.6.0` (private)
- `Microsoft.CodeAnalysis.CSharp` `5.6.0` (private)
- No runtime dependency on SafeWebCore

### Packaging mechanics

Custom target `AddAnalyzerToPackage` places the analyzer assembly at `analyzers/dotnet/cs/`. `SuppressDependenciesWhenPacking` and `IncludeBuildOutput=false` follow standard analyzer packaging.

### Package readiness notes

- Correctly marked **preview**
- No icon; no release notes property
- Preview is appropriate until rule set and false-positive rate are validated in real apps
- No dedicated analyzer unit-test project yet (gap)

### Documentation

- `src/SafeWebCore.Analyzers/README.md`
- Changelog tooling section under `[Unreleased]`
- Roadmap v1.5 tooling epics

---

## 4. SafeWebCore.Testing

| Attribute | Value |
|-----------|--------|
| **Path** | `src/SafeWebCore.Testing/` |
| **PackageId** | `SafeWebCore.Testing` |
| **Version (csproj)** | `1.0.0-preview.1` |
| **TFM** | `net10.0` |
| **IsPackable** | Yes |
| **Published on nuget.org** | **No** |
| **Package readme** | `src/SafeWebCore.Testing/README.md` |
| **Icon** | None |
| **License** | MIT |
| **Source files** | 3 C# files |

### Purpose

Consumer-facing test helpers for asserting security headers, CSP mode, and nonces in integration tests.

### Surface

| Type | Role |
|------|------|
| `HeaderAssertions` | `AssertHasSecurityHeaders`, CSP enforce/report-only helpers |
| `CspNonceAssertions` | Nonce presence / consistency |
| `TestHostBootstrapExtensions` | Quick `WebApplicationFactory` / `TestServer` bootstrap |

### Dependencies

- Project reference: `SafeWebCore`
- `Microsoft.AspNetCore.Mvc.Testing` `10.0.*`
- `xunit.v3.assert` `3.2.*`

### Package readiness notes

- Correctly marked **preview**
- README is minimal but packable
- Depends on floating `10.0.*` / `3.2.*` — pin exact versions before a stable release
- No dedicated tests for the testing helpers themselves (gap)
- No package icon / release notes / SourceLink

### Documentation

- `src/SafeWebCore.Testing/README.md`
- Referenced from `docs/README.md` quick reference

---

# Test projects (`tests/`)

## 5. SafeWebCore.Tests

| Attribute | Value |
|-----------|--------|
| **Path** | `tests/SafeWebCore.Tests/` |
| **TFM** | `net10.0` |
| **IsPackable** | `false` |
| **In solution** | Yes |
| **Framework** | xUnit v3 (`xunit.v3` 3.2.2) |
| **Host** | `Microsoft.AspNetCore.TestHost` 10.0.10 |
| **Coverage** | `coverlet.collector` 10.0.1 |
| **Latest audit result** | **103 passed**, 0 failed |

### Test files

| File | Focus |
|------|--------|
| `CspBuilderTests.cs` | Fluent CSP builder |
| `CspNonceTagHelpersTests.cs` | Razor nonce TagHelpers |
| `CspReportMiddlewareTests.cs` | Violation reporting pipeline |
| `DiagnosticsEndpointTests.cs` | Opt-in diagnostics endpoint |
| `NetSecureHeadersMiddlewareTests.cs` | Core middleware behavior |
| `NetSecureHeadersOptionsValidationTests.cs` | Startup validation |
| `NonceServiceTests.cs` | Nonce generation |
| `SafeWebCoreMetricsTests.cs` | `System.Diagnostics.Metrics` |
| `SecurePresetsTests.cs` | Preset outputs |
| `ServiceCollectionExtensionsTests.cs` | DI / config / env helpers |
| `TypedPolicyBuildersTests.cs` | Referrer / Permissions / CO* builders |

### Role in release

Primary quality gate for the core package. Must stay green for any SafeWebCore NuGet publish.

---

## 6. SafeWebCore.FraudDetection.Tests

| Attribute | Value |
|-----------|--------|
| **Path** | `tests/SafeWebCore.FraudDetection.Tests/` |
| **TFM** | `net10.0` |
| **IsPackable** | `false` |
| **In solution** | **No** (gap) |
| **Framework** | xUnit v3 3.2.2 |
| **Latest audit result** | **12 passed**, 0 failed |

### Test files

| File | Focus |
|------|--------|
| `WesternImpersonationDetectorFocusedTests.cs` | Legacy + risk score paths |
| `FraudMetricsTests.cs` | Fraud meters |
| `WebhookFraudEventSinkTests.cs` | Webhook sink POST behavior |

### Role in release

Quality gate for `SafeWebCore.FraudDetection` first publish. Should be added to the solution and CI.

### Notes

- Test SDK package version (`18.4.0`) differs from core tests (`18.8.1`) — align when convenient
- Coverage collector not referenced (optional improvement)


---

# Example projects (`examples/`)

All examples:

- Target `net10.0`
- Reference local `src/SafeWebCore` (not NuGet)
- Are **not** packable product artifacts
- Built successfully in Release during audit (0 warnings)

## 7. MinimalApi

| Attribute | Value |
|-----------|--------|
| **Path** | `examples/MinimalApi/` |
| **RootNamespace** | `SafeWebCore.Examples.MinimalApi` |
| **Demonstrates** | Strict A+ one-liner, CSP `with` customization, `GetCspNonce()`, CSP report endpoint, `SkipNetSecureHeaders` for health |

## 8. MvcApp

| Attribute | Value |
|-----------|--------|
| **Path** | `examples/MvcApp/` |
| **RootNamespace** | `SafeWebCore.Examples.MvcApp` |
| **Demonstrates** | MVC preset, typed policy builders, path policies, `[CspNonce]`, TagHelpers, layout nonce patterns |

## 9. ApiService

| Attribute | Value |
|-----------|--------|
| **Path** | `examples/ApiService/` |
| **RootNamespace** | `SafeWebCore.Examples.ApiService` |
| **Demonstrates** | API-oriented security header profile and service-style setup |

### Docs

- `docs/examples.md` — feature matrix and run instructions
- Per-example README files where present

---

# Benchmark project (`benchmarks/`)

## 10. SafeWebCore.Benchmarks

| Attribute | Value |
|-----------|--------|
| **Path** | `benchmarks/SafeWebCore.Benchmarks/` |
| **TFM** | `net10.0` |
| **OutputType** | `Exe` |
| **IsPackable** | No (tooling) |
| **In solution** | No |
| **Library** | BenchmarkDotNet `0.15.8` |
| **Reference** | `SafeWebCore` |
| **Audit build** | Success, 0 warnings |

### Suites

| File | Measures |
|------|----------|
| `CspBuildBenchmarks.cs` | CSP string construction |
| `CspReportParseBenchmarks.cs` | Report parsing |
| `MiddlewarePipelineBenchmarks.cs` | Full middleware hot path |
| `NonceBenchmarks.cs` | Nonce generation / `TryWriteNonce` |
| `PolicyBuilderBenchmarks.cs` | Typed policy builders |
| `PresetBenchmarks.cs` | Preset application |

### Docs

- `docs/benchmarks.md`

---

# Supporting repository assets (not projects)

| Asset | Role |
|-------|------|
| `README.md` | Product landing page |
| `PACKAGE.md` | NuGet readme for **SafeWebCore** (currently documents 1.3.5) |
| `CHANGELOG.md` | Keep a Changelog; large `[Unreleased]` block for next release |
| `CONTRIBUTING.md` | Contributor workflow + public API rules |
| `LICENSE` | MIT |
| `icon.png` | NuGet icon (core package) |
| `docs/*` | Full documentation set |
| `.github/ISSUE_TEMPLATE/*` | Bug / feature templates |
| `.github/PULL_REQUEST_TEMPLATE/*` | PR template |
| `.github/FUNDING.yml` | Sponsors |
| **No** `.github/workflows/*` | **No CI/CD workflows present** (release gap) |

---

# Dependency graph

```text
SafeWebCore.Tests ──────────────► SafeWebCore
SafeWebCore.Testing ────────────► SafeWebCore
SafeWebCore.Benchmarks ─────────► SafeWebCore
examples/* ─────────────────────► SafeWebCore

SafeWebCore.FraudDetection.Tests ► SafeWebCore.FraudDetection

SafeWebCore.Analyzers  (standalone Roslyn package)
SafeWebCore.FraudDetection  (standalone; no dependency on SafeWebCore)
```

---

# Project maturity matrix

| Project | Maturity | Ship as NuGet? | Recommended version policy |
|---------|----------|----------------|----------------------------|
| SafeWebCore | Production (published) | **Yes** (already) | SemVer stable; next feature drop → bump beyond 1.3.5 |
| SafeWebCore.FraudDetection | Production-ready code, unpublished | **Yes** (first release candidate) | `1.0.0` after checklist |
| SafeWebCore.Analyzers | Preview tooling | **Yes (preview only)** | Keep `*-preview.N` until rules proven |
| SafeWebCore.Testing | Preview tooling | **Yes (preview only)** | Keep `*-preview.N`; pin deps before stable |
| *.Tests | Internal | No | — |
| examples/* | Samples | No | — |
| Benchmarks | Internal tooling | No | — |

---

# Related docs

- [NuGet packages & candidates](nuget-packages.md)
- [Release readiness checklist](release-readiness.md)
- [Roadmap](roadmap.md)
- [Backward compatibility policy](development/backward-compatibility-policy.md)
- [Examples guide](examples.md)
- [Benchmarks](benchmarks.md)
