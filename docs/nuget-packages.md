# SafeWebCore — NuGet Packages & Candidates

This document describes every packable project: identity, contents, publish status, readiness, and recommended release sequencing.

**Last audited:** 2026-07-25  
**Local pack output (verified):** `artifacts/nupkg/`

---

## Quick status

| PackageId | csproj version | nuget.org | Local pack | Recommendation |
|-----------|----------------|-----------|------------|----------------|
| **SafeWebCore** | `1.3.5` | **Published** (`1.0.0`–`1.3.5`) | `SafeWebCore.1.3.5.nupkg` + `.snupkg` | Do **not** republish 1.3.5. Ship next as **1.4.0** (or later) after promoting Unreleased work |
| **SafeWebCore.FraudDetection** | `1.0.0` | **Not published** | `SafeWebCore.FraudDetection.1.0.0.nupkg` | **Primary new package candidate** for first public release |
| **SafeWebCore.Analyzers** | `1.0.0-preview.1` | **Not published** | `SafeWebCore.Analyzers.1.0.0-preview.1.nupkg` | Publish as **preview** only |
| **SafeWebCore.Testing** | `1.0.0-preview.1` | **Not published** | `SafeWebCore.Testing.1.0.0-preview.1.nupkg` | Publish as **preview** only |

Non-packable (not NuGet candidates): test projects, examples, benchmarks.

---

## How to pack locally

From repo root:

```bash
dotnet pack src/SafeWebCore/SafeWebCore.csproj -c Release -o artifacts/nupkg
dotnet pack src/SafeWebCore.FraudDetection/SafeWebCore.FraudDetection.csproj -c Release -o artifacts/nupkg
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
| Current csproj Version | `1.3.5` |
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
| nuget.org latest | **1.3.5** |
| csproj `<Version>` | **1.3.5** |
| Workspace code | Contains **Unreleased** features (config binding, env helpers, diagnostics, metrics, …) documented in `CHANGELOG.md` |

**Implication:** packing the current workspace still produces `SafeWebCore.1.3.5.nupkg`, but the **bits are not identical** to a pure 1.3.5 release if Unreleased APIs are present. Before any publish:

1. Decide the next SemVer (recommended **1.4.0** for the DX wave, or split 1.4 / 1.5 / 1.6 per roadmap).
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

- [ ] Version bump aligned with Unreleased features
- [ ] `PACKAGE.md` still describes 1.3.5; refresh for next release
- [x] Broken doc links in `PACKAGE.md` still point at old `docs/roadmap-v1.2.md` paths (now under `docs/archive/`)
- [ ] CI pack + push workflow missing
- [ ] Git tags use `V1.x.0.0` style; nuget versions use `1.x.y` — standardize tags (`v1.4.0`)


---

# Package 2 — SafeWebCore.FraudDetection (stable candidate, unpublished)

## Identity

| Field | Value |
|-------|--------|
| PackageId | `SafeWebCore.FraudDetection` |
| Version | `1.0.0` |
| Authors | MPCoreDeveloper |
| Company | Posseth Software |
| License | MIT |
| Readme | `src/SafeWebCore.FraudDetection/README.md` |
| Icon | **Missing** |
| TFM | `net10.0` |
| Symbols | **Not enabled** |
| nuget.org | **Not listed** |

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
| Tests pass (12) | Pass |
| Package README quality | Strong |
| XML docs generated | Pass |
| Public API baseline files present | Pass (still in adoption mode for RS0016/17) |
| Package icon | **Fail** — add `icon.png` pack item (can reuse root icon) |
| Symbol package / SourceLink | **Fail** — copy flags from core csproj |
| `PackageReleaseNotes` | **Missing** |
| Example app using the module | **Missing** |
| In solution test project for CI | Fraud tests not in `.slnx` |
| Changelog entry for 1.0.0 publish | Should be explicit before push |
| nuget.org listing | Not yet |

## Pre-publish checklist (FraudDetection 1.0.0)

1. Add package icon + optional SourceLink/symbols parity with core.
2. Add `PackageReleaseNotes` for 1.0.0.
3. Ensure FraudDetection tests are in solution + CI.
4. Confirm public API Unshipped entries intended for 1.0.0 are promoted/shipped as desired.
5. Add a short “Getting started with FraudDetection” link from root README / docs index.
6. Prefer an example or recipe showing registration + `Analyze` + sink.
7. Tag git appropriately; document multi-package versioning (core 1.x vs fraud 1.0.0).
8. `dotnet nuget push artifacts/nupkg/SafeWebCore.FraudDetection.1.0.0.nupkg --source https://api.nuget.org/v3/index.json`

### Versioning strategy note

Core is already at **1.3.5** while FraudDetection starts at **1.0.0**. That is normal for a **separate package identity**. Do not force the same version number across packages unless you deliberately adopt lockstep versioning.

---

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
| nuget.org | **Not listed** |

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

- Safe to publish as **preview** to gather feedback.
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
| nuget.org | **Not listed** |

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

- Publish as preview alongside or after core next release so the dependency version makes sense.
- If publishing while core nuget latest is 1.3.5 but Testing was built against newer APIs, either ship Testing only after core 1.4.0 is on nuget.org, **or** ensure Testing only uses APIs available in published core.

---

## Recommended release trains

### Train A — Patch / no new packages

Only if fixing 1.3.5 without Unreleased features: branch from the 1.3.5 release commit, bump to `1.3.6`, ship core only.

### Train B — Next core feature release (recommended for current workspace)

| Step | Package | Version |
|------|---------|---------|
| 1 | SafeWebCore | **1.4.0** (promote Unreleased DX items; optionally hold 1.5/1.6 items) |
| 2 | SafeWebCore.FraudDetection | **1.0.0** first public |
| 3 | SafeWebCore.Analyzers | **1.0.0-preview.1** |
| 4 | SafeWebCore.Testing | **1.0.0-preview.1** (after core 1.4.0 is live) |

Roadmap mapping reminder:

| Roadmap band | Themes | Likely package impact |
|--------------|--------|------------------------|
| v1.4 DX | Config binding, env helpers, diagnostics, API baseline | Core version bump |
| v1.5 Tooling | Analyzers, Testing, recipes | New preview packages |
| v1.6 Observability | Metrics, fraud sinks/risk | Core + FraudDetection |

If shipping **all** current Unreleased work in one go, a single **SafeWebCore 1.6.0** (or 1.4.0 with a rich changelog) is acceptable **only if** the SemVer story is clear in CHANGELOG/PACKAGE.md. Prefer not to claim 1.3.5 for post-1.3.5 APIs.

### Train C — Fraud-only first publish

Publish **FraudDetection 1.0.0** alone (no core bump). Valid because packages are independent. Still complete FraudDetection checklist first.

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
| CI pack + smoke test install | **Missing** | **Missing** | **Missing** | **Missing** |

---

## Publish commands (manual)

```bash
# After version bumps, changelog, and tests
dotnet pack src/SafeWebCore/SafeWebCore.csproj -c Release -o artifacts/nupkg
dotnet pack src/SafeWebCore.FraudDetection/SafeWebCore.FraudDetection.csproj -c Release -o artifacts/nupkg
dotnet pack src/SafeWebCore.Analyzers/SafeWebCore.Analyzers.csproj -c Release -o artifacts/nupkg
dotnet pack src/SafeWebCore.Testing/SafeWebCore.Testing.csproj -c Release -o artifacts/nupkg

dotnet nuget push artifacts/nupkg/SafeWebCore.<version>.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/nupkg/SafeWebCore.<version>.snupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/nupkg/SafeWebCore.FraudDetection.1.0.0.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
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
