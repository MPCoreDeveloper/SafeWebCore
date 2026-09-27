# SafeWebCore — NuGet Packages & Candidates

This document describes every packable project: identity, contents, publish status, readiness, and recommended release sequencing.

**Last audited:** 2026-09-27 (version facts re-verified against the nuget.org flat-container API)
**Local pack output (verified):** `artifacts/nupkg/`

> **Canonical version form:** `<Version>` is three-part SemVer — `1.2.3`, optionally with a `-preview.N`
> suffix. The four-part form is not used, and a version nuget.org already carries is never reused.
> The published set below is the source of truth for "what is already taken".

---

## Quick status

| PackageId | csproj version | nuget.org (verified 2026-09-27) | Local pack | Recommendation |
|-----------|----------------|--------------------------------|------------|----------------|
| **SafeWebCore** | `1.7.0` | **Published**: `1.0.0`, `1.1.0`, `1.2.0`, `1.3.0`, `1.3.5`, `1.6.0`, `1.7.0` | `SafeWebCore.1.7.0.nupkg` + `.snupkg` | Do **not** reuse a published version. Next release is **1.8.0** with the Unreleased work |
| **SafeWebCore.FraudDetection** | `1.1.0` | **Published**: `1.0.0` | `SafeWebCore.FraudDetection.1.1.0.nupkg` | Next release **1.1.0** — three-part, because `1.0.0.0` normalizes to the published `1.0.0` |
| **SafeWebCore.JwtBearer** | `1.0.0` | **Published**: `1.0.0` | `SafeWebCore.JwtBearer.1.0.0.nupkg` | Published — JWT authority fail-fast + token hardening |
| **SafeWebCore.Analyzers** | `1.0.0-preview.1` | **Published**: `1.0.0-preview.1` | `SafeWebCore.Analyzers.1.0.0-preview.1.nupkg` | Preview published; keep publishing as **preview** only |
| **SafeWebCore.Testing** | `1.0.0-preview.1` | **Published**: `1.0.0-preview.1` | `SafeWebCore.Testing.1.0.0-preview.1.nupkg` | Preview published; keep publishing as **preview** only |

Non-packable (not NuGet candidates): test projects, examples, benchmarks.

---

## How to pack locally

From repo root:

```bash
dotnet pack src/SafeWebCore/SafeWebCore.csproj -c Release -o artifacts/nupkg
dotnet pack src/SafeWebCore.FraudDetection/SafeWebCore.FraudDetection.csproj -c Release -o artifacts/nupkg
dotnet pack src/SafeWebCore.JwtBearer/SafeWebCore.JwtBearer.csproj -c Release -o artifacts/nupkg

dotnet pack src/SafeWebCore.Analyzers/SafeWebCore.Analyzers.csproj -c Release -o artifacts/nupkg
dotnet pack src/SafeWebCore.Testing/SafeWebCore.Testing.csproj -c Release -o artifacts/nupkg
```

Audit pack results (2026-07-25):

| File | Approx. size |
|------|--------------|
| `SafeWebCore.1.3.5.nupkg` | ~89 KB |
| `SafeWebCore.1.3.5.snupkg` | ~27 KB |
| `SafeWebCore.FraudDetection.1.0.0.nupkg` | ~51 KB |
| `SafeWebCore.Analyzers.1.0.0-preview.1.nupkg` | ~10 KB |
| `SafeWebCore.Testing.1.0.0-preview.1.nupkg` | ~8 KB |

---

# Package 1 — SafeWebCore (stable, already published)

## Identity

| Field | Value |
|-------|--------|
| PackageId | `SafeWebCore` |
| Current csproj Version | `1.7.0` |
| Authors | MPCoreDeveloper |
| Company | Posseth Software |
| License | MIT (`PackageLicenseExpression`) |
| Project URL | https://github.com/MPCoreDeveloper/SafeWebCore |
| NuGet gallery | https://www.nuget.org/packages/SafeWebCore |
| Readme in package | `PACKAGE.md` (repo root) |
| Icon | `icon.png` |
| TFM | `net10.0` |
| Symbols | `snupkg` enabled |

## Package contents (verified)

```text
SafeWebCore.nuspec
PACKAGE.md
icon.png
lib/net10.0/SafeWebCore.dll
lib/net10.0/SafeWebCore.xml
```

Plus companion `SafeWebCore.1.3.5.snupkg` for symbols/SourceLink debugging.

## What consumers get

ASP.NET Core middleware and helpers for security headers, full CSP Level 3 + Level 4-ready directives, nonces, TagHelpers, path policies, endpoint overrides, CSP reporting, and presets (StrictAPlus / Api / Mvc / Blazor / SpaReverseProxy).

```bash
dotnet add package SafeWebCore
```

## Critical versioning note

| Layer | State |
|-------|--------|
| nuget.org latest | **1.7.0** |
| csproj `<Version>` | **1.7.0** |
| Workspace code | Contains **Unreleased** work (the sink-isolation amendment, the fail-closed fraud mappings, the `AdditionalHeaders` guard, …) documented in `CHANGELOG.md` |

**Implication:** packing the current workspace still produces `SafeWebCore.1.7.0.nupkg`, but the **bits are not identical** to the published 1.7.0 once the Unreleased work is in. Before any publish:

1. Pick the next version — **1.8.0** for the additive work that is Unreleased today (`SecurityEventSinkFailures`, the `SecurityEventDispatcher` overload). Never reuse a version nuget.org already carries.
2. Bump `<Version>` in `SafeWebCore.csproj`.
3. Move `CHANGELOG.md` `[Unreleased]` into a dated section.
4. Update `PACKAGE.md` “Current version” and release notes.
5. Promote `PublicAPI.Unshipped.txt` → `PublicAPI.Shipped.txt` for intentional new surface.
6. Never overwrite an already-published version on nuget.org.

## Packaging strengths

- Full metadata (description, tags, license, repo, icon, readme)
- XML docs included
- Deterministic build + SourceLink flags
- Public API analyzers for compatibility
- Release notes embedded in nuspec metadata

## Packaging gaps for next release

- [ ] Version bump aligned with Unreleased features (next: **1.8.0**)
- [x] `PACKAGE.md` and `README.md` state the current version (both say 1.7.0)
- [x] Broken doc links in `PACKAGE.md` still point at old `docs/roadmap-v1.2.md` paths (now under `docs/archive/`)
- [ ] CI pack + push workflow missing
- [ ] Git tags use `V1.x.0.0` style; nuget versions use `1.x.y` — standardize tags (`v1.4.0`)


---

# Package 2 — SafeWebCore.FraudDetection (stable, published)

## Identity

| Field | Value |
|-------|--------|
| PackageId | `SafeWebCore.FraudDetection` |
| Version | `1.1.0` (csproj; published on nuget.org is `1.0.0`) |
| Authors | MPCoreDeveloper |
| Company | Posseth Software |
| License | MIT |
| Readme | `src/SafeWebCore.FraudDetection/README.md` |
| Icon | **Missing** |
| TFM | `net10.0` |
| Symbols | **Not enabled** |
| nuget.org | **Published** — `1.0.0` |

## Package contents (verified)

```text
SafeWebCore.FraudDetection.nuspec
README.md
lib/net10.0/SafeWebCore.FraudDetection.dll
lib/net10.0/SafeWebCore.FraudDetection.xml
```

## What consumers get

Optional fraud module:

- Geo-cultural consistency detection (region-neutral, recommended)
- Legacy Western impersonation detection (compat)
- Pen-test / scanner detection + authorized bypass notifications
- Options + optional DB configuration store
- Optional `IGeoIpService` enrichment
- `IFraudEventSink` pipeline (logging + webhook helpers)
- Opt-in metrics meter `SafeWebCore.FraudDetection`

```bash
dotnet add package SafeWebCore.FraudDetection
```

**Does not** depend on the `SafeWebCore` package — can be used alone.

## Readiness scorecard

| Check | Status |
|-------|--------|
| Builds Release, 0 warnings | Pass |
| Packs successfully | Pass |
| Tests pass (27 in `SafeWebCore.FraudDetection.Tests`, 2026-09-27) | Pass |
| Package README quality | Strong |
| XML docs generated | Pass |
| Public API baseline files present | Pass (still in adoption mode for RS0016/17) |
| Package icon | **Fail** — add `icon.png` pack item (can reuse root icon) |
| Symbol package / SourceLink | **Fail** — copy flags from core csproj |
| `PackageReleaseNotes` | **Missing** |
| Example app using the module | **Missing** |
| In solution test project for CI | ✅ `tests/SafeWebCore.FraudDetection.Tests` is in `SafeWebCore.slnx` |
| Changelog entry for the next publish | Should be explicit before push |
| nuget.org listing | ✅ `1.0.0` published |

## Checklist for the next release (FraudDetection 1.1.0)

1. Add package icon + optional SourceLink/symbols parity with core.
2. Add `PackageReleaseNotes` for 1.1.0 (additive `RiskLevel.Unclassified`, the fail-closed mappings and the sink-failure counter).
3. Confirm public API Unshipped entries intended for 1.1.0 are promoted/shipped as desired.
4. Add a short “Getting started with FraudDetection” link from root README / docs index.
5. Prefer an example or recipe showing registration + `Analyze` + sink.
6. Tag git appropriately; document multi-package versioning (core 1.x vs fraud 1.1.0).
7. `dotnet nuget push artifacts/nupkg/SafeWebCore.FraudDetection.1.1.0.nupkg --source https://api.nuget.org/v3/index.json`

### Versioning strategy note

Core is at **1.7.0** while FraudDetection moves to **1.1.0**. That is normal for a **separate package identity**. Do not force the same version number across packages unless you deliberately adopt lockstep versioning. The one hard rule is that `<Version>` stays three-part SemVer, because NuGet normalizes `1.0.0.0` to `1.0.0` — which FraudDetection already published.

---

# Package 2b — SafeWebCore.JwtBearer (published companion)

## Identity

| Field | Value |
|-------|--------|
| PackageId | `SafeWebCore.JwtBearer` |
| Version | `1.0.0` (csproj) |
| Authors / Company | MPCoreDeveloper / Posseth Software |
| License | MIT |
| Project URL | https://github.com/MPCoreDeveloper/SafeWebCore |
| Readme in package | `src/SafeWebCore.JwtBearer/README.md` |
| Icon | `icon.png` |
| TFM | `net10.0` |

## Purpose

Solves [dotnet/aspnetcore#67991](https://github.com/dotnet/aspnetcore/issues/67991)
(reported by **Stephan van Rooij**): a misspelled JWT authority silently 401s every request
because the metadata failure is logged only at Information level, below the default log
threshold. The module adds a startup authority guard (fail fast or loud Error), optional token
hardening, and runtime metadata-failure logging — before the .NET team's planned fix in
**.NET 12 Planning**.

## What consumers get

```bash
dotnet add package SafeWebCore.JwtBearer
```

- `AddJwtBearerAuthorityValidation` — startup authority guard (Stephan's fix).
- `AddJwtBearerHardening` — optional token-validation hardening (algorithms, typ, audience/issuer, max lifetime, ...).
- `AddSafeWebCoreJwtBearer` — one-line registration (AddJwtBearer + hardening + guard).

## Readiness scorecard

| Check | Status |
|-------|--------|
| Builds in solution | ✅ |
| Unit + integration tests (xUnit v3) | ✅ (33, incl. Stephan's reproduction) |
| Public API baseline (`PublicAPI.*.txt`) | ✅ |
| README + icon packed | ✅ (verified) |
| `ci.yml` / `nuget-publish.yml` pack step | ✅ (added) |

### Publish command

```bash
dotnet pack src/SafeWebCore.JwtBearer/SafeWebCore.JwtBearer.csproj -c Release -o artifacts/nupkg
dotnet nuget push artifacts/nupkg/SafeWebCore.JwtBearer.1.0.0.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
```

# Package 3 — SafeWebCore.Analyzers (preview candidate)

## Identity

| Field | Value |
|-------|--------|
| PackageId | `SafeWebCore.Analyzers` |
| Version | `1.0.0-preview.1` |
| TFM | `netstandard2.0` |
| Packaging style | Analyzer-only (`IncludeBuildOutput=false`) |
| DLL path in nupkg | `analyzers/dotnet/cs/SafeWebCore.Analyzers.dll` |
| Readme | `src/SafeWebCore.Analyzers/README.md` |
| nuget.org | **Published** — `1.0.0-preview.1` |

## Package contents (verified)

```text
SafeWebCore.Analyzers.nuspec
README.md
analyzers/dotnet/cs/SafeWebCore.Analyzers.dll
```

No `lib/` folder (correct for pure analyzers).

## Rules shipped in preview.1

| Id | Intent |
|----|--------|
| SWC001 | Registration without middleware |
| SWC002 | Permanent report-only CSP |
| SWC003 | unsafe-inline without nonce |
| SWC004 | Overly broad CSP sources |

## Readiness scorecard

| Check | Status |
|-------|--------|
| Builds / packs | Pass |
| Analyzer packaging layout | Pass |
| Preview versioning | Pass |
| README documents rules | Pass |
| Dedicated analyzer tests | **Gap** |
| Icon / release notes | Optional for preview |

## Publish guidance

- Published as **preview** (`1.0.0-preview.1`); subsequent releases stay preview until analyzer unit tests exist.
- Document install with `PrivateAssets=all`.
- Do **not** mark stable until analyzer unit tests exist and false-positive review is done on sample apps.

```xml
<PackageReference Include="SafeWebCore.Analyzers" Version="1.0.0-preview.1">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```


---

# Package 4 — SafeWebCore.Testing (preview candidate)

## Identity

| Field | Value |
|-------|--------|
| PackageId | `SafeWebCore.Testing` |
| Version | `1.0.0-preview.1` |
| TFM | `net10.0` |
| Depends on | `SafeWebCore` (project → becomes package dependency on pack) |
| Also depends on | `Microsoft.AspNetCore.Mvc.Testing` `10.0.*`, `xunit.v3.assert` `3.2.*` |
| Readme | `src/SafeWebCore.Testing/README.md` |
| nuget.org | **Published** — `1.0.0-preview.1` |

## Package contents (verified)

```text
SafeWebCore.Testing.nuspec
README.md
lib/net10.0/SafeWebCore.Testing.dll
lib/net10.0/SafeWebCore.Testing.xml
```

## API surface (small, focused)

- Header assertions (security headers present / expected values)
- CSP enforce vs report-only assertions
- Nonce assertions
- Test host bootstrap extensions

## Readiness scorecard

| Check | Status |
|-------|--------|
| Builds / packs | Pass |
| Preview version | Pass |
| README example | Minimal but usable |
| Floating dependency versions (`10.0.*`, `3.2.*`) | **Risk** — pin before stable |
| Package dependency on SafeWebCore version | Keep aligned when releasing |
| Tests for helpers | **Gap** |
| Icon / release notes / symbols | Optional for preview |

## Publish guidance

- Published as preview (`1.0.0-preview.1`).
- For the next Testing release, keep the `SafeWebCore` dependency version aligned with a version that is on nuget.org (1.7.0 today) — or ship Testing only after core 1.8.0 is live if it needs newer APIs.

---

## Recommended release trains

### Train A — Patch / no new packages

Only if fixing 1.7.0 without Unreleased features: branch from the 1.7.0 release commit, bump to `1.7.1`, ship core only.

### Train B — Next core feature release (recommended for current workspace)

| Step | Package | Version |
|------|---------|---------|
| 1 | SafeWebCore | **1.8.0** (promote the Unreleased additive work) |
| 2 | SafeWebCore.FraudDetection | **1.1.0** |
| 3 | SafeWebCore.Analyzers | next `1.0.0-preview.N` |
| 4 | SafeWebCore.Testing | next `1.0.0-preview.N` (after core 1.8.0 is live) |

Roadmap mapping reminder:

| Roadmap band | Themes | Likely package impact |
|--------------|--------|------------------------|
| v1.4 DX | Config binding, env helpers, diagnostics, API baseline | Core version bump |
| v1.5 Tooling | Analyzers, Testing, recipes | New preview packages |
| v1.6 Observability | Metrics, fraud sinks/risk | Core + FraudDetection |

If shipping **all** current Unreleased work in one go, a single **SafeWebCore 1.8.0** is the honest number for the additive surface (new counters, the `SecurityEventDispatcher` overload), with the tightened behaviour called out in the changelog. Never reuse 1.7.0 or any version listed in Quick status for post-release APIs.

### Train C — Fraud-only release

Publish **FraudDetection 1.1.0** alone (no core bump). Valid because packages are independent — 1.0.0 is already live, so 1.1.0 is the next number. Still complete the FraudDetection checklist first.

---

## Shared packaging standards (target state)

| Standard | Core | Fraud | Analyzers | Testing |
|----------|------|-------|-----------|---------|
| MIT license expression | Yes | Yes | Yes | Yes |
| RepositoryUrl / ProjectUrl | Yes | Yes | Yes | Yes |
| PackageReadmeFile | Yes | Yes | Yes | Yes |
| PackageIcon | Yes | **Add** | Optional | Optional |
| GenerateDocumentationFile | Yes | Yes | Yes | Yes |
| IncludeSymbols + snupkg | Yes | **Add** | N/A (analyzer) | Optional |
| SourceLink / Deterministic | Yes | **Add** | Optional | Optional |
| PublicApiAnalyzers | Yes | Yes | N/A | N/A |
| PackageReleaseNotes | Yes | **Add** | Add when stable | Add when stable |
| CI pack | Yes (`.github/workflows/ci.yml`) | Yes | Yes | Yes |
| CI smoke test that installs the packed packages | **Missing** | **Missing** | **Missing** | **Missing** |

---

## Publish commands (manual)

```bash
# After version bumps, changelog, and tests
dotnet pack src/SafeWebCore/SafeWebCore.csproj -c Release -o artifacts/nupkg
dotnet pack src/SafeWebCore.FraudDetection/SafeWebCore.FraudDetection.csproj -c Release -o artifacts/nupkg
dotnet pack src/SafeWebCore.JwtBearer/SafeWebCore.JwtBearer.csproj -c Release -o artifacts/nupkg

dotnet pack src/SafeWebCore.Analyzers/SafeWebCore.Analyzers.csproj -c Release -o artifacts/nupkg
dotnet pack src/SafeWebCore.Testing/SafeWebCore.Testing.csproj -c Release -o artifacts/nupkg

dotnet nuget push artifacts/nupkg/SafeWebCore.<version>.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/nupkg/SafeWebCore.<version>.snupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/nupkg/SafeWebCore.FraudDetection.1.1.0.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/nupkg/SafeWebCore.Analyzers.1.0.0-preview.1.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/nupkg/SafeWebCore.Testing.1.0.0-preview.1.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
```

---

## Related docs

- [Project catalog](projects.md)
- [Release readiness](release-readiness.md)
- [Roadmap](roadmap.md)
- [Backward compatibility policy](development/backward-compatibility-policy.md)
- Root `PACKAGE.md` (NuGet readme for core)
- Per-package READMEs under `src/*/`
