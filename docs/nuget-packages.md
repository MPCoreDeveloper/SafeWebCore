# SafeWebCore — NuGet Packages & Candidates

This document describes every packable project: identity, contents, publish status, readiness, and recommended release sequencing.

**Last audited:** 2026-09-29 (the **1.8.1** patch release was prepared, packed, tagged `v1.8.1` and published the same day; all five versions were re-verified against the flat-container API afterwards, and each package was downloaded back from nuget.org and its nuspec inspected)
**Local pack output (verified):** `artifacts/nupkg/`

> **Canonical version form:** `<Version>` is three-part SemVer — `1.2.3`, optionally with a `-preview.N`
> suffix. The four-part form is not used, and a version nuget.org already carries is never reused.
> The published set below is the source of truth for "what is already taken".

---

## Quick status

| PackageId | csproj version | nuget.org (verified 2026-09-29) | Local pack | Recommendation |
|-----------|----------------|--------------------------------|------------|----------------|
| **SafeWebCore** | `1.8.1` | **Published**: `1.0.0`, `1.1.0`, `1.2.0`, `1.3.0`, `1.3.5`, `1.6.0`, `1.7.0`, `1.8.0` (2026-09-28), **`1.8.1`** (2026-09-29) | `SafeWebCore.1.8.1.nupkg` + `.snupkg` | **1.8.1 released** — maintenance patch on the 1.8.0 line, tagged `v1.8.1`; internal refactors, docs and dependency maintenance only |
| **SafeWebCore.FraudDetection** | `1.1.1` | **Published**: `1.0.0`, `1.1.0` (2026-09-28), **`1.1.1`** (2026-09-29) | `SafeWebCore.FraudDetection.1.1.1.nupkg` + `.snupkg` | **1.1.1 released** with the `v1.8.1` tag — three-part, because `1.0.0.0` normalizes to the published `1.0.0` |
| **SafeWebCore.JwtBearer** | `1.0.1` | **Published**: `1.0.0` (2026-09-10), **`1.0.1`** (2026-09-29) | `SafeWebCore.JwtBearer.1.0.1.nupkg` | **1.0.1 released** — the `Microsoft.AspNetCore.Authentication.JwtBearer` floor moves 10.0.11 → 10.0.12 and the nuspec now carries release notes. This is the bump that finally ships the badge-corrected packaged `README.md` |
| **SafeWebCore.Analyzers** | `1.0.0-preview.2` | **Published**: `1.0.0-preview.1`, **`1.0.0-preview.2`** (2026-09-29) | `SafeWebCore.Analyzers.1.0.0-preview.2.nupkg` | Preview published with the SonarCloud cleanup; keep publishing as **preview** only |
| **SafeWebCore.Testing** | `1.0.0-preview.2` | **Published**: `1.0.0-preview.1`, **`1.0.0-preview.2`** (2026-09-29) | `SafeWebCore.Testing.1.0.0-preview.2.nupkg` | Preview published with the SonarCloud cleanup and `xunit.v3.assert` 4.0.1; keep publishing as **preview** only |

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

Pack results for the 1.8.0 release prep (2026-09-27, verified from the files actually produced):

| File | Size | Notes |
|------|------|-------|
| `SafeWebCore.1.8.0.nupkg` | ~89 KB | nuspec `<version>` is `1.8.0` and the v1.8.0 `releaseNotes` are embedded; contents: `PACKAGE.md`, `icon.png`, `lib/net10.0/SafeWebCore.dll` + `.xml` |
| `SafeWebCore.1.8.0.snupkg` | ~26 KB | symbols / SourceLink |
| `SafeWebCore.FraudDetection.1.1.0.nupkg` | ~77 KB | three-part version; contents: `README.md`, `icon.png`, `lib/net10.0/SafeWebCore.FraudDetection.dll` + `.xml` |
| `SafeWebCore.FraudDetection.1.1.0.snupkg` | ~24 KB | symbols / SourceLink — parity with core since 2026-09-27 |

Pack results for the **1.8.1** release prep (2026-09-29, verified from the files actually produced and from the nuspecs inside them):

| File | Size | Notes |
|------|------|-------|
| `SafeWebCore.1.8.1.nupkg` | 89.5 KB | nuspec `<version>` is `1.8.1` and the v1.8.1 `releaseNotes` are embedded; contents: `PACKAGE.md`, `icon.png`, `lib/net10.0/SafeWebCore.dll` + `.xml`; no package dependencies |
| `SafeWebCore.1.8.1.snupkg` | 25.8 KB | symbols / SourceLink |
| `SafeWebCore.FraudDetection.1.1.1.nupkg` | 77.6 KB | three-part version; contents: `README.md`, `icon.png`, `lib/net10.0/SafeWebCore.FraudDetection.dll` + `.xml`; no package dependencies |
| `SafeWebCore.FraudDetection.1.1.1.snupkg` | 23.3 KB | symbols / SourceLink |
| `SafeWebCore.JwtBearer.1.0.1.nupkg` | 52.3 KB | carries the new v1.0.1 `releaseNotes`, `README.md` + `icon.png`; one dependency: `Microsoft.AspNetCore.Authentication.JwtBearer` `10.0.12` |
| `SafeWebCore.JwtBearer.1.0.1.snupkg` | 19.0 KB | **new** — the symbol package this package never had before 1.0.1 |
| `SafeWebCore.Analyzers.1.0.0-preview.2.nupkg` | 36.3 KB | analyzer-only layout (`analyzers/dotnet/cs/…`), `README.md` + `icon.png`, no dependencies |
| `SafeWebCore.Testing.1.0.0-preview.2.nupkg` | 34.0 KB | depends on `SafeWebCore` `1.8.1`, `Microsoft.AspNetCore.Mvc.Testing` `10.0.12` and `xunit.v3.assert` `4.0.1` |

(All sizes and nuspec contents measured from the files this release actually produced, not estimated.)

### Publication evidence (verified 2026-09-29)

All five packages were downloaded back from the flat container after the push and their nuspecs inspected:

| Package | Published | `repository commit` | Notes |
|---------|-----------|---------------------|-------|
| `SafeWebCore` 1.8.1 + `.snupkg` | ✅ | `0ae2f24` | release notes, `PACKAGE.md`, `icon.png`, no dependencies |
| `SafeWebCore.FraudDetection` 1.1.1 + `.snupkg` | ✅ | `0ae2f24` | release notes, `README.md`, `icon.png`, no dependencies |
| `SafeWebCore.JwtBearer` 1.0.1 + `.snupkg` | ✅ | `0ae2f24` | release notes, dependency `Microsoft.AspNetCore.Authentication.JwtBearer` `10.0.12` |
| `SafeWebCore.Analyzers` 1.0.0-preview.2 | ✅ | `0ae2f24` | analyzer-only layout, `README.md`, `icon.png`, no dependencies |
| `SafeWebCore.Testing` 1.0.0-preview.2 | ✅ | `0ae2f24` | dependencies `SafeWebCore` `1.8.1`, `Microsoft.AspNetCore.Mvc.Testing` `10.0.12`, `xunit.v3.assert` `4.0.1` |

Flat-container indexes after publication:

```text
safewebcore                    ... 1.7.0, 1.8.0, 1.8.1
safewebcore.frauddetection     1.0.0, 1.1.0, 1.1.1
safewebcore.jwtbearer          1.0.0, 1.0.1
safewebcore.analyzers          1.0.0-preview.1, 1.0.0-preview.2
safewebcore.testing            1.0.0-preview.1, 1.0.0-preview.2
```

The suffix `0ae2f24` in every nuspec is the `v1.8.1` release commit, so the published bits provably come from the tag and not from a later working tree.

> ⚠️ **The nuget.org API key expires soon.** Both publish runs logged
> `warn : Your API key expires in 6 days`. Regenerate it at
> <https://www.nuget.org/account/apikeys> and refresh the `NUGET_API_KEY` repository secret before the next
> release, or the push step returns a 401. An *empty* secret is worse: the workflows print a warning and
> `exit 0`, so the job passes while nothing is published.

---

# Package 1 — SafeWebCore (stable, already published)

## Identity

| Field | Value |
|-------|--------|
| PackageId | `SafeWebCore` |
| Current csproj Version | `1.8.1` (tagged `v1.8.1` on 2026-09-29; `1.8.0` was the previous release) |
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

Plus companion `SafeWebCore.1.8.1.snupkg` for symbols/SourceLink debugging.

## What consumers get

ASP.NET Core middleware and helpers for security headers, full CSP Level 3 + Level 4-ready directives, nonces, TagHelpers, path policies, endpoint overrides, CSP reporting, and presets (StrictAPlus / Api / Mvc / Blazor / SpaReverseProxy).

```bash
dotnet add package SafeWebCore
```

## Critical versioning note

| Layer | State |
|-------|--------|
| nuget.org latest | **1.8.1** (published 2026-09-29) |
| csproj `<Version>` | **1.8.1** (bumped 2026-09-29) |
| Workspace code | The **1.8.1** work, dated `[1.8.1] — 2026-09-29` in `CHANGELOG.md` |

**Why 1.8.1 and not 1.9.0:** everything since the `v1.8.0` tag is internal — refactors, a SonarCloud cleanup, docs, CI and dependency maintenance. There is no new public API, no new option and no behavior change, so SemVer says PATCH. The strongest signal is the baselines: all three `PublicAPI.Unshipped.txt` files still hold only `#nullable enable`, so nothing needed promoting into `Shipped.txt` this cycle.

**Implication:** `SafeWebCore.1.8.1.nupkg` is published (2026-09-29) and is a genuinely new version — the published 1.8.0 keeps its own bits. Release state:

1. ✅ **1.8.1** picked and set as `<Version>` in `SafeWebCore.csproj`. Never reuse a version nuget.org already carries.
2. ✅ `CHANGELOG.md` `[Unreleased]` promoted to the dated `[1.8.1] — 2026-09-29` section, with a `Companion packages` section naming the other four versions and an empty `[Unreleased]` placeholder left for the next cycle.
3. ✅ `PACKAGE.md` and `README.md` state 1.8.1 with the new notes, and `<PackageReleaseNotes>` in the csproj carries the 1.8.1 summary that lands in the nuspec.
4. ✅ **No PublicAPI promotion needed** — no public symbol was added, so the three Unshipped baselines stay as they are and the frozen `Shipped.txt` surface is untouched.
5. ✅ Companion versions bumped in the same commit: `SafeWebCore.FraudDetection` **1.1.1**, `SafeWebCore.JwtBearer` **1.0.1** (the only one with a consumer-visible dependency change), `SafeWebCore.Analyzers` and `SafeWebCore.Testing` **1.0.0-preview.2**.
6. Never overwrite an already-published version on nuget.org.

## Packaging strengths

- Full metadata (description, tags, license, repo, icon, readme)
- XML docs included
- Deterministic build + SourceLink flags
- Public API analyzers for compatibility
- Release notes embedded in nuspec metadata

## Packaging gaps for next release

- [x] Version bumped for the change set being released — **1.8.1** in `SafeWebCore.csproj` (2026-09-29)
- [x] `PACKAGE.md` and `README.md` state the current version (both say 1.8.1)
- [x] `docs/getting-started.md` install snippet corrected from the stale `Version="1.3.5"` to the shipped `1.8.1`
- [x] Broken doc links in `PACKAGE.md` still point at old `docs/roadmap-v1.2.md` paths (now under `docs/archive/`)
- [x] CI pack + push workflow present — `.github/workflows/ci.yml` (build/test/pack on push + PR) and `nuget-publish.yml` (pack + `dotnet nuget push` on a `v*` tag, with `--skip-duplicate`)
- [x] Git tags are mixed: the remote carries `V1.0.0.0`, `V1.1.0.0`, `V1.2.0.0`, `V1.3.0.0`, `V1.6.0.0.0`, `v1.7.0` and `v1.8.0` (there is no tag for 1.3.5). This release is tagged **`v1.8.1`**; the three-part `vX.Y.Z` form is canonical from here on (spine AD-17)
- [x] PublicAPI promotion checked for 1.8.1 — **nothing to promote**: all three `PublicAPI.Unshipped.txt` files still hold only `#nullable enable`, because this cycle added no public symbol. The 11 core entries promoted on 2026-09-27 keep the `Shipped.txt` baseline claiming exactly the surface that shipped through 1.7.0 and 1.8.0
- [x] Promoted `PublicAPI.Unshipped.txt` → `PublicAPI.Shipped.txt` before the tag — **11 core entries** moved on 2026-09-27, so the 450-line shipped baseline claims exactly the surface that shipped through 1.7.0 and 1.8.0
- [x] Public API baselines curated instead of suppressed: the 25 `RS0016`/`RS0017` warnings were fixed in the declarations themselves (record members need `override`/`static`, `MeterName` is a `const` field and not a property, `JwtAuthorityValidationGuard.Dispose()` was missing), so the whole solution now builds **0 warnings / 0 errors**. The `WarningsNotAsErrors` exemption for `RS0016`/`RS0017` therefore hides nothing anymore and can be dropped to enforce the surface as errors


---

# Package 2 — SafeWebCore.FraudDetection (stable, published)

## Identity

| Field | Value |
|-------|--------|
| PackageId | `SafeWebCore.FraudDetection` |
| Version | `1.1.1` (csproj; tagged `v1.8.1` on 2026-09-29 — `1.0.0` was the first release, `1.1.0` the previous one) |
| Authors | MPCoreDeveloper |
| Company | Posseth Software |
| License | MIT |
| Readme | `src/SafeWebCore.FraudDetection/README.md` |
| Icon | `icon.png` — reuses the root icon, packed since 2026-09-27 |
| TFM | `net10.0` |
| Symbols | `.snupkg` + SourceLink — parity with core since 2026-09-27 |
| Release notes | `PackageReleaseNotes` for 1.1.1 embedded in the nuspec |
| nuget.org | **Published** — `1.0.0`, `1.1.0` (2026-09-28), `1.1.1` (2026-09-29) |

## Package contents (verified)

```text
SafeWebCore.FraudDetection.nuspec
README.md
icon.png
lib/net10.0/SafeWebCore.FraudDetection.dll
lib/net10.0/SafeWebCore.FraudDetection.xml
```

Symbol package: `SafeWebCore.FraudDetection.1.1.1.snupkg` → `lib/net10.0/SafeWebCore.FraudDetection.pdb`.

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
| Builds Release, 0 warnings | Pass — solution-wide **0 warnings / 0 errors** since the baseline curation (2026-09-27) |
| Packs successfully | Pass |
| Tests pass (27 in `SafeWebCore.FraudDetection.Tests`, 2026-09-27) | Pass |
| Package README quality | Strong |
| XML docs generated | Pass |
| Public API baseline files present | Pass — curated and promoted (55 entries in `PublicAPI.Shipped.txt`, only `#nullable enable` left in Unshipped) |
| Package icon | ✅ `icon.png` packed since 2026-09-27 |
| Symbol package / SourceLink | ✅ `.snupkg` + SourceLink since 2026-09-27 |
| `PackageReleaseNotes` | ✅ Present for 1.1.1 in the nuspec |
| Example app using the module | **Missing** |
| In solution test project for CI | ✅ `tests/SafeWebCore.FraudDetection.Tests` is in `SafeWebCore.slnx` |
| Changelog entry for the next publish | ✅ `[1.8.1]` section plus the embedded release notes |
| nuget.org listing | ✅ `1.0.0` and `1.1.0` published (2026-09-28, with the `v1.8.0` tag); `1.1.1` with `v1.8.1` (2026-09-29) |

## Checklist for the next release (FraudDetection 1.1.1)

1. ~~Add package icon + SourceLink/symbols parity with core~~ — done 2026-09-27 (`icon.png`, `.snupkg`, the SourceLink property group).
2. ~~Add `PackageReleaseNotes` for 1.1.0~~ — done 2026-09-27 (additive `RiskLevel.Unclassified`, the fail-closed mappings and the sink-failure counter).
3. ~~Confirm public API Unshipped entries intended for 1.1.0 are promoted/shipped~~ — done 2026-09-27 (all 55 entries promoted into `PublicAPI.Shipped.txt`).
4. Add a short “Getting started with FraudDetection” link from root README / docs index.
5. Prefer an example or recipe showing registration + `Analyze` + sink.
6. ~~Tag git appropriately; document multi-package versioning~~ — done: `v1.8.0` covers core 1.8.0 + fraud 1.1.0, and `v1.8.1` covers core 1.8.1 + fraud 1.1.1 (see the versioning note below).
7. The tag push publishes it (`nuget-publish.yml` pushes every package with `--skip-duplicate`); the manual equivalent stays: `dotnet nuget push artifacts/nuget/SafeWebCore.FraudDetection.1.1.1.nupkg --source https://api.nuget.org/v3/index.json`

### Versioning strategy note

Core ships **1.8.1** and FraudDetection **1.1.1** from the same `v1.8.1` tag (**1.8.0** and **1.1.0** respectively were the latest published before it). That is normal for a **separate package identity**. Do not force the same version number across packages unless you deliberately adopt lockstep versioning. The one hard rule is that `<Version>` stays three-part SemVer, because NuGet normalizes `1.0.0.0` to `1.0.0` — which FraudDetection already published.

---

# Package 2b — SafeWebCore.JwtBearer (published companion)

## Identity

| Field | Value |
|-------|--------|
| PackageId | `SafeWebCore.JwtBearer` |
| Version | `1.0.1` (csproj; tagged `v1.8.1` on 2026-09-29) |
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
| Builds in solution | ✅ (0 warnings / 0 errors solution-wide since 2026-09-27) |
| Unit + integration tests (xUnit v3) | ✅ (38 as of 2026-09-27, incl. Stephan's reproduction) |
| Public API baseline (`PublicAPI.*.txt`) | ✅ Curated 2026-09-27 — the 2 `RS0016`/`RS0017` warnings came from the declaration form in `JwtAuthorityValidationGuard`, and all 51 entries were promoted into `PublicAPI.Shipped.txt` before the `v1.8.0` tag |
| README + icon packed | ✅ (verified) |
| `ci.yml` / `nuget-publish.yml` pack step | ✅ (added) |
| Symbol package / SourceLink | ✅ Enabled 2026-09-29 — 1.0.1 adds the `IncludeSymbols` / `snupkg` + SourceLink property group, so this package now ships a `.snupkg` like core and FraudDetection. This closes the gap recorded against 1.0.0 |
| NuGet dependency | `Microsoft.AspNetCore.Authentication.JwtBearer` **10.0.12** (raised from 10.0.11 in 1.0.1) |
| nuget.org listing | ✅ `1.0.0` published 2026-09-10; `1.0.1` with the `v1.8.1` tag (2026-09-29) |

### Packaging gap closed in 1.0.1

`SafeWebCore.JwtBearer.csproj` had no `IncludeSymbols`/SourceLink property group, so 1.0.0 shipped without a
`.snupkg`. The `v1.8.0` tag repacked it but `--skip-duplicate` skipped the already-published 1.0.0, so this was
carried as a next-version task. **1.0.1 is that version and closes it:** the property group now matches core and
FraudDetection, the release notes are embedded in the nuspec, and the badge-corrected `README.md` finally reaches
nuget.org instead of being skipped as a duplicate.

### Publish command

```bash
dotnet pack src/SafeWebCore.JwtBearer/SafeWebCore.JwtBearer.csproj -c Release -o artifacts/nupkg
dotnet nuget push artifacts/nupkg/SafeWebCore.JwtBearer.1.0.1.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
```

# Package 3 — SafeWebCore.Analyzers (preview candidate)

## Identity

| Field | Value |
|-------|--------|
| PackageId | `SafeWebCore.Analyzers` |
| Version | `1.0.0-preview.2` (tagged `v1.8.1` on 2026-09-29) |
| TFM | `netstandard2.0` |
| Packaging style | Analyzer-only (`IncludeBuildOutput=false`) |
| DLL path in nupkg | `analyzers/dotnet/cs/SafeWebCore.Analyzers.dll` |
| Readme | `src/SafeWebCore.Analyzers/README.md` |
| nuget.org | **Published** — `1.0.0-preview.1`, `1.0.0-preview.2` (2026-09-29) |

## Package contents (verified)

```text
SafeWebCore.Analyzers.nuspec
README.md
analyzers/dotnet/cs/SafeWebCore.Analyzers.dll
```

No `lib/` folder (correct for pure analyzers).

## Rules shipped since preview.1 (unchanged in preview.2)

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

- Published as **preview** (`1.0.0-preview.2`); subsequent releases stay preview until analyzer unit tests exist.
- Document install with `PrivateAssets=all`.
- Do **not** mark stable until analyzer unit tests exist and false-positive review is done on sample apps.

```xml
<PackageReference Include="SafeWebCore.Analyzers" Version="1.0.0-preview.2">
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
| Version | `1.0.0-preview.2` (tagged `v1.8.1` on 2026-09-29) |
| TFM | `net10.0` |
| Depends on | `SafeWebCore` (project → becomes package dependency on pack) |
| Also depends on | `Microsoft.AspNetCore.Mvc.Testing` `10.0.*` (floating), `xunit.v3.assert` `4.0.1` (pinned) |
| Readme | `src/SafeWebCore.Testing/README.md` |
| nuget.org | **Published** — `1.0.0-preview.1`, `1.0.0-preview.2` (2026-09-29) |

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
| Floating dependency versions (`10.0.*`) | **Risk** — only `Microsoft.AspNetCore.Mvc.Testing` still floats; pin it before stable. `xunit.v3.assert` is pinned to `4.0.1` |
| Package dependency on SafeWebCore version | Keep aligned when releasing |
| Tests for helpers | **Gap** |
| Icon / release notes / symbols | Optional for preview |

## Publish guidance

- Published as preview (`1.0.0-preview.2`, with the `v1.8.1` tag on 2026-09-29).
- The `SafeWebCore` dependency resolves to a version that is live on nuget.org in the same batch: `1.0.0-preview.2` packs a dependency on core **1.8.1**, and both ship from the `v1.8.1` tag, so no consumer can resolve a core version that does not exist yet.

---

## Recommended release trains

### Train A — Maintenance patch (**selected for the current workspace**)

Everything since the `v1.8.0` tag is internal, so the honest SemVer step is a **patch**, not a feature release: bump core to `1.8.1`, bump each companion whose bits actually changed, and ship the set from one `v1.8.1` tag.

| Step | Package | Version | Prep state (2026-09-29) |
|------|---------|---------|--------------------------|
| 1 | SafeWebCore | **1.8.1** | ✅ `<Version>` bumped, CHANGELOG dated, `README.md` / `PACKAGE.md` / `docs/nuget-packages.md` updated, `PackageReleaseNotes` rewritten; ✅ no PublicAPI promotion needed (nothing new shipped) |
| 2 | SafeWebCore.FraudDetection | **1.1.1** | ✅ three-part `<Version>`, changelog names 1.1.1, release notes refreshed |
| 3 | SafeWebCore.JwtBearer | **1.0.1** | ✅ dependency floor 10.0.11 → 10.0.12; ✅ `PackageReleaseNotes` added; ✅ symbol package + SourceLink parity closes the 1.0.0 gap |
| 4 | SafeWebCore.Analyzers | **1.0.0-preview.2** | ✅ bumped so the SonarCloud cleanup actually ships — `1.0.0-preview.1` is already on nuget.org and would be skipped as a duplicate |
| 5 | SafeWebCore.Testing | **1.0.0-preview.2** | ✅ bumped for the cleanup + `xunit.v3.assert` 4.0.1; packs a dependency on core 1.8.1 and ships in the same push |

### Train B — Next core feature release (the `v1.8.0` line, already shipped)

Kept for reference. `v1.8.0` shipped core **1.8.0** + FraudDetection **1.1.0** on 2026-09-28. Use this shape again when the workspace finally carries additive public API: a `<Version>` above 1.8.1, changelog dated, `README.md`/`PACKAGE.md` updated, `PackageReleaseNotes` rewritten, and the `PublicAPI.Unshipped.txt` → `Shipped.txt` promotion done in the release commit.

Note: `nuget-publish.yml` packs and pushes **every** package found in `artifacts/nuget` on a `v*` tag. Tagging `v1.8.1` therefore publishes all five — core 1.8.1, FraudDetection 1.1.1, JwtBearer 1.0.1 and both `1.0.0-preview.2` packages. That is exactly why every version in the table above was bumped: an unchanged version is silently skipped by `--skip-duplicate`, which is how the JwtBearer `README.md` fix stayed unpublished through the `v1.8.0` run.

There is a **second** publish workflow: `nuget-publish-jwtbearer.yml` triggers on any `master` push that touches
`src/SafeWebCore.JwtBearer/**`, packs **only** that project and pushes it. It exists so a JwtBearer change does not
have to wait for a whole release train. In the `v1.8.1` release that workflow won the race (it published 1.0.1 at
04:49:09Z, ~13 s before the tag run), so the tag run's own JwtBearer push was reported as "already exists" and
skipped. Both orders are harmless, but it means **a JwtBearer version can go live on nuget.org from a plain
`master` push** — do not merge a JwtBearer change with a half-prepared version number, and remember that a
`README.md`-only edit under that folder still publishes the package.

Verified 2026-09-29 against the NuGet flat-container API before tagging: none of the five versions in the `v1.8.1` batch — `SafeWebCore` **1.8.1**, `SafeWebCore.FraudDetection` **1.1.1**, `SafeWebCore.JwtBearer` **1.0.1**, `SafeWebCore.Analyzers` and `SafeWebCore.Testing` **1.0.0-preview.2** — existed on nuget.org, so all five are genuinely new and `--skip-duplicate` has nothing to skip. This also settles the JwtBearer `README.md` item left over from the `v1.8.0` run: 1.0.0 was code-equal to HEAD and only its packaged README differed, by five badge lines, so nuget.org kept the badge-less README until a higher version existed. **1.0.1 is that version.**

Verified 2026-09-27 against the NuGet registration API: of the five packages that tag packs, only `SafeWebCore` **1.8.0** and `SafeWebCore.FraudDetection` **1.1.0** are new. The other three are already live and are skipped — `SafeWebCore.JwtBearer` `1.0.0` (published **2026-09-10**), `SafeWebCore.Analyzers` and `SafeWebCore.Testing` `1.0.0-preview.1`. The JwtBearer package is code-equal to HEAD (verified by extracting `RequireSigningKeys` and `PeriodicValidationInterval` from the published `lib/net10.0/SafeWebCore.JwtBearer.dll` and its XML doc); only its packaged `README.md` differs, by five badge lines committed after that publish, so nuget.org keeps the badge-less README until that package gets a higher version.

Roadmap mapping reminder:

| Roadmap band | Themes | Likely package impact |
|--------------|--------|------------------------|
| v1.4 DX | Config binding, env helpers, diagnostics, API baseline | Core version bump |
| v1.5 Tooling | Analyzers, Testing, recipes | New preview packages |
| v1.6 Observability | Metrics, fraud sinks/risk | Core + FraudDetection |

If shipping **all** current work in one go, a single **SafeWebCore 1.8.1** is the honest number for what changed since 1.8.0 (internal refactors, docs and dependency maintenance), and the changelog says exactly that. Never reuse 1.8.0 or any version listed in Quick status for later work.

### Train C — Fraud-only release

Publish **FraudDetection 1.1.1** alone (no core bump). Valid because packages are independent — `1.1.0` is already live, so `1.1.1` is the next number. Note that `nuget-publish.yml` pushes everything it packs, so a fraud-only release needs a trimmed pack step or a manual `dotnet nuget push`.

---

## Shared packaging standards (target state)

| Standard | Core | Fraud | JwtBearer | Analyzers | Testing |
|----------|------|-------|-----------|-----------|---------|
| MIT license expression | Yes | Yes | Yes | Yes | Yes |
| RepositoryUrl / ProjectUrl | Yes | Yes | Yes | Yes | Yes |
| PackageReadmeFile | Yes | Yes | Yes | Yes | Yes |
| PackageIcon | Yes | Yes | Yes | Optional | Yes |
| GenerateDocumentationFile | Yes | Yes | Yes | Yes | Yes |
| IncludeSymbols + snupkg | Yes | Yes | **Yes (since 1.0.1)** | N/A (analyzer) | Optional |
| SourceLink / Deterministic | Yes | Yes | **Yes (since 1.0.1)** | Optional | Optional |
| PublicApiAnalyzers | Yes | Yes | Yes | N/A | N/A |
| PackageReleaseNotes | Yes | Yes | **Yes (since 1.0.1)** | Add when stable | Add when stable |
| CI pack | Yes (`.github/workflows/ci.yml`) | Yes | Yes | Yes | Yes |
| CI smoke test that installs the packed packages | **Missing** | **Missing** | **Missing** | **Missing** | **Missing** |

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
dotnet nuget push artifacts/nupkg/SafeWebCore.FraudDetection.1.1.1.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/nupkg/SafeWebCore.JwtBearer.1.0.1.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/nupkg/SafeWebCore.Analyzers.1.0.0-preview.2.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/nupkg/SafeWebCore.Testing.1.0.0-preview.2.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
```

---

## Related docs

- [Project catalog](projects.md)
- [Release readiness](release-readiness.md)
- [Roadmap](roadmap.md)
- [Backward compatibility policy](development/backward-compatibility-policy.md)
- Root `PACKAGE.md` (NuGet readme for core)
- Per-package READMEs under `src/*/`
