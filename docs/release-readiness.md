# SafeWebCore — Release Readiness Assessment

> **Superseded for version facts.** This audit is the 2026-07-25 snapshot taken while 1.3.5 was
> published (`cc3148b`). The published set as of 2026-09-29 is: `SafeWebCore` 1.0.0, 1.1.0, 1.2.0,
> 1.3.0, 1.3.5, 1.6.0, 1.7.0, 1.8.0, 1.8.1; `SafeWebCore.FraudDetection` 1.0.0, 1.1.0, 1.1.1;
> `SafeWebCore.JwtBearer` 1.0.0, 1.0.1; `SafeWebCore.Analyzers` and `SafeWebCore.Testing`
> 1.0.0-preview.1, 1.0.0-preview.2. Use
> [nuget-packages.md](nuget-packages.md) for the current version facts and the canonical three-part
> SemVer form.
>
> **1.8.1 shipped on 2026-09-29:** a maintenance patch, tagged `v1.8.1` and published to nuget.org by
> `nuget-publish.yml`, together with `SafeWebCore.FraudDetection` 1.1.1, `SafeWebCore.JwtBearer` 1.0.1
> and both `1.0.0-preview.2` packages. All five versions were verified absent from the flat container
> before tagging, so nothing was skipped as a duplicate — including the JwtBearer package, whose
> badge-corrected `README.md` had been stranded at 1.0.0 since the `v1.8.0` run.
>
> **1.8.0 shipped on 2026-09-28:** tagged `v1.8.0` (on 2026-09-27) and published to nuget.org by
> `nuget-publish.yml` (run `36374600399`), together with `SafeWebCore.FraudDetection` 1.1.0 and both
> `.snupkg` symbol packages. Everything below the release state sections is the frozen 2026-07-25 audit
> and does **not** describe 1.8.0 or 1.8.1.

---

## Release state for 1.8.1 (tagged 2026-09-29)

The live status of the 1.8.1 maintenance patch. The 1.8.0 section below stays as the previous release's
snapshot.

| Item | State |
|------|-------|
| Scope | ✅ **Patch, not a feature release.** Everything since the `v1.8.0` tag is internal — refactors, a SonarCloud cleanup, docs, CI and dependency maintenance. No new public API, no new option, no behavior change |
| Build | ✅ `dotnet build SafeWebCore.slnx -c Release` — **0 warnings, 0 errors** (`TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`) |
| Tests | ✅ 190 passed, 0 failed, 0 skipped — `SafeWebCore.Tests` 125, `SafeWebCore.FraudDetection.Tests` 27, `SafeWebCore.JwtBearer.Tests` 38, matching the pre-change baseline exactly |
| Version | ✅ `<Version>` **1.8.1** in `SafeWebCore.csproj`; `SafeWebCore.FraudDetection` **1.1.1**, `SafeWebCore.JwtBearer` **1.0.1**, `SafeWebCore.Analyzers` and `SafeWebCore.Testing` **1.0.0-preview.2** |
| CHANGELOG | ✅ `[Unreleased]` promoted to `[1.8.1] — 2026-09-29`, with a `Companion packages` section naming all four companion versions and a `Compatibility` section; an empty `[Unreleased]` placeholder and the new compare link were added |
| Package docs | ✅ `README.md` and `PACKAGE.md` state 1.8.1 with the new notes; `docs/getting-started.md` install snippet corrected from the stale `1.3.5`; `docs/nuget-packages.md` status table, per-package identities and release trains all moved to 1.8.1 |
| Release notes | ✅ `<PackageReleaseNotes>` rewritten for core (v1.8.1) and FraudDetection (v1.1.1), and **added** to JwtBearer (v1.0.1), which previously shipped without any |
| Pack | ✅ `dotnet pack` produces `SafeWebCore.1.8.1.nupkg` + `.snupkg`, `SafeWebCore.FraudDetection.1.1.1.nupkg` + `.snupkg`, `SafeWebCore.JwtBearer.1.0.1.nupkg` + `.snupkg`, `SafeWebCore.Analyzers.1.0.0-preview.2.nupkg` and `SafeWebCore.Testing.1.0.0-preview.2.nupkg`. Each nuspec was verified for version, embedded release notes and dependencies |
| PublicAPI | ✅ **Nothing to promote** — all three `PublicAPI.Unshipped.txt` files still hold only `#nullable enable`, because this cycle added no public symbol. `RS0037` continues to guard the unchanged `Shipped.txt` surface |
| Packaging gap closed | ✅ `SafeWebCore.JwtBearer` gained the `IncludeSymbols` / `snupkg` + SourceLink property group that core and FraudDetection already had, plus release notes. This resolves the "next JwtBearer version" task recorded against 1.0.0 |
| Dependency maintenance | ✅ `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.11 → 10.0.12 (consumer-visible floor), `Microsoft.AspNetCore.TestHost` 10.0.11 → 10.0.12, `Microsoft.NET.Test.Sdk` 18.9.0 → 18.10.1, `xunit.v3` 4.0.0 → 4.0.1, `xunit.v3.assert` 4.0.0 → 4.0.1, `coverlet.collector` 10.0.1 → 10.1.0. Only latest **stable** versions were taken; `11.0.0-rc.1` was deliberately not applied because it leaves the `net10.0` line. `Microsoft.AspNetCore.Mvc.Testing` stays intentionally floating on `10.0.*` (resolves to 10.0.12) |
| Tag + push | ✅ Annotated tag `v1.8.1` (`0556127`) points at release commit `0ae2f24` and is pushed to `master`. **Two** workflows ran and both finished **success**: `nuget-publish.yml` on the tag (run `36523275407`) and `nuget-publish-jwtbearer.yml` on the `master` push (run `36523272232`), which triggers on `src/SafeWebCore.JwtBearer/**` |
| Publication | ✅ All five live on nuget.org, `.nupkg` and `.snupkg` alike. The tag run pushed `SafeWebCore` 1.8.1 (+`.snupkg`), `SafeWebCore.FraudDetection` 1.1.1 (+`.snupkg`), `SafeWebCore.Analyzers` and `SafeWebCore.Testing` 1.0.0-preview.2. The JwtBearer workflow won the race and published `SafeWebCore.JwtBearer` 1.0.1 (+`.snupkg`) at 04:49:09Z, so the tag run reported "already exists" and `--skip-duplicate` skipped it — expected and harmless |
| Publication verified | ✅ All five downloaded back from the flat container and their nuspecs inspected: `<version>` correct, `releaseNotes` embedded, `icon.png` + readme present, and `repository commit = 0ae2f24908933ea7695a0cc76e71aa37916f1c75` (the release commit). Dependencies confirmed: `SafeWebCore.JwtBearer` → `Microsoft.AspNetCore.Authentication.JwtBearer` `10.0.12`; `SafeWebCore.Testing` → `SafeWebCore` `1.8.1`, `Microsoft.AspNetCore.Mvc.Testing` `10.0.12`, `xunit.v3.assert` `4.0.1` |
| Git | ✅ `5db999a` (test dependency bumps) and `0ae2f24` (release prep) on `master`, tagged **`v1.8.1`** |

> ⚠️ **Action needed before the next release: the nuget.org API key expires.** Both publish runs logged
> `warn : Your API key expires in 6 days. Visit https://www.nuget.org/account/apikeys to regenerate your API key.`
> Regenerate it and refresh the `NUGET_API_KEY` repository secret, or the next release's push step fails with a 401.
> Note that a *missing* secret is worse than a failing one: the workflows branch on an empty variable, print a
> warning and `exit 0`, so the job goes green while nothing is published.

### Deliberately still open after 1.8.1

- No CI performance gate for the middleware hot path (benchmarks exist but do not run in the pipeline).
- No CI smoke test that installs the freshly packed packages into a throwaway app.
- `benchmarks/SafeWebCore.Benchmarks` is not marked `IsPackable=false`.
- `global.json` pins the test runner but not `sdk.version`, so a build can pick up a preview SDK.
- `Microsoft.AspNetCore.Mvc.Testing` still floats on `10.0.*` in `SafeWebCore.Testing`.

---

## Release state for 1.8.0 (published 2026-09-28)

The live status of the 1.8.0 release. The 2026-07-25 audit below stays frozen as the historical
snapshot.

| Item | State |
|------|-------|
| Build | ✅ `dotnet build SafeWebCore.slnx -c Release --no-incremental` — **0 warnings, 0 errors**. The 25 `RS0016`/`RS0017` public-API-baseline warnings are gone because the declarations were corrected, not suppressed: record members need `override`/`static`, `MeterName` is a `const` field rather than a property, and `JwtAuthorityValidationGuard.Dispose()` was missing from the baseline |
| Tests | ✅ 190 passed, 0 failed, 0 skipped — `SafeWebCore.Tests` 125, `SafeWebCore.FraudDetection.Tests` 27, `SafeWebCore.JwtBearer.Tests` 38 |
| Version | ✅ `<Version>` **1.8.0** in `SafeWebCore.csproj`; `SafeWebCore.FraudDetection` **1.1.0** (three-part) |
| CHANGELOG | ✅ `[Unreleased]` promoted to `[1.8.0] — 2026-09-27`, empty `[Unreleased]` placeholder left, compare links added, stale 1.7.0-era duplicates removed |
| Package docs | ✅ `README.md` and `PACKAGE.md` state 1.8.0 with the new notes; `<PackageReleaseNotes>` rewritten in the csproj (it lands in the nuspec) |
| Pack | ✅ `dotnet pack` produces `SafeWebCore.1.8.0.nupkg` (~89 KB) + `.snupkg` (~26 KB) and `SafeWebCore.FraudDetection.1.1.0.nupkg` (~77 KB) + `.snupkg` (~24 KB). Both nuspecs carry the right version, release notes, `icon.png` and a `repository` element with the release commit; the FraudDetection package now has the same metadata shape as core (verified 2026-09-27) |
| PublicAPI | ✅ Promoted before the tag — 11 core, 55 `SafeWebCore.FraudDetection` and 51 `SafeWebCore.JwtBearer` entries moved from `PublicAPI.Unshipped.txt` to `PublicAPI.Shipped.txt`; only `#nullable enable` remains in the three Unshipped files, so `RS0037` now guards the shipped surface |
| Tag + push | ✅ Tag `v1.8.0` (three-part form; the remote's older tags are mixed) points at release commit `9773184` → `nuget-publish.yml` run `36374600399` packed all five and pushed with `--skip-duplicate`. Only `SafeWebCore` 1.8.0 and `SafeWebCore.FraudDetection` 1.1.0 were new there; `SafeWebCore.JwtBearer` 1.0.0 (published 2026-09-10) and the two `1.0.0-preview.1` packages were skipped as duplicates |
| Publication | ✅ nuget.org carries `SafeWebCore` **1.8.0** and `SafeWebCore.FraudDetection` **1.1.0** (published 2026-09-28 03:40Z) plus both `.snupkg` symbol packages (`/api/v2/symbolpackage`). Verified by downloading both `.nupkg` files back from the flat container: `icon.png`, the readme (`PACKAGE.md` / `README.md`), `repository commit = 9773184` and the 1.8.0/1.1.0 release notes are all present in the published nuspecs. Bear in mind the v3 index lagged the push by ~5 minutes — the flat container is where a new version shows up first |
| Git | ✅ All 1.8.0 work committed on `master`; release prep and the packaging/baseline change are separate commits |
| Static analysis | ✅ SonarCloud Automatic Analysis on `master`: **0 open issues**, quality gate **OK** over 5,798 analysed lines — 0 code smells, 0.0 % duplication, A on maintainability, reliability and security (verified 2026-09-28). The full set is 353 fixed, 8 `Won't fix` and 2 `False positive`; the triage rules, the evidence and the ten accepted findings are recorded in [SonarCloud Triage](development/sonarcloud-triage.md) |

Still genuinely open from the original audit — none of it blocks 1.8.0: a CI performance gate, analyzer
unit tests, symbol/SourceLink parity for `SafeWebCore.JwtBearer` (it needs a higher JwtBearer version to
reach nuget.org), and the optional flip of `RS0016`/`RS0017` from warning to error now that the baseline
is clean. The publish also carried one action item: CI warned that the `NUGET_API_KEY` GitHub secret
expires within days of 2026-09-28, so renew it before the next release (a missing or expired key makes the
push step warn and exit successfully, which looks identical to a successful publish).


**Audit date:** 2026-07-25  
**Branch:** `master` (tracking `origin/master`)  
**HEAD commit at audit start:** `cc3148b` ("new version 1.3.5")  
**Working tree:** **dirty** — large set of modified + untracked files (Unreleased feature work)

---

## Executive summary

| Question | Answer |
|----------|--------|
| Is the **published** SafeWebCore **1.3.5** already released? | **Yes** — on nuget.org |
| Is the **current workspace** ready to publish **as 1.3.5 again**? | **No** — contains Unreleased APIs; must not overwrite 1.3.5 |
| Is the workspace ready for a **new** release train (1.4+ / new packages)? | **Almost** — build & tests are green; packaging works; process/docs/CI gaps remain |
| Ready to publish **FraudDetection 1.0.0** today? | **Conditionally** — code quality OK; packaging polish + changelog/git hygiene first |
| Ready to publish **Analyzers / Testing** previews? | **Conditionally** — OK as preview after pinning story vs core version |

### Overall readiness grade

| Track | Grade | Meaning |
|-------|-------|---------|
| Core **1.3.5** (already shipped) | **Shipped** | No action unless hotfix branch |
| Next **core** release from HEAD | **B− (not go yet)** | Quality bar met; versioning, changelog freeze, CI, git commit required |
| **FraudDetection** first publish | **B** | Strong; finish packaging parity + docs links |
| **Analyzers / Testing** preview | **B** | Acceptable for preview push |

---

## Verification results (this audit)

### Build

| Scope | Configuration | Result |
|-------|---------------|--------|
| `SafeWebCore.slnx` | Release | **Succeeded — 0 warnings, 0 errors** |
| examples/ApiService | Release | Succeeded |
| examples/MinimalApi | Release | Succeeded |
| examples/MvcApp | Release | Succeeded |
| benchmarks/SafeWebCore.Benchmarks | Release | Succeeded |

### Tests

| Project | Passed | Failed | Skipped |
|---------|--------|--------|---------|
| SafeWebCore.Tests | **103** | 0 | 0 |
| SafeWebCore.FraudDetection.Tests | **12** | 0 | 0 |
| **Total** | **115** | **0** | **0** |

### Pack

All four packable projects produced nupkgs under `artifacts/nupkg/` successfully (see [nuget-packages.md](nuget-packages.md)).

### nuget.org presence

| Package | Status |
|---------|--------|
| SafeWebCore | Present: 1.0.0, 1.1.0, 1.2.0, 1.3.0, **1.3.5** |
| SafeWebCore.FraudDetection | Absent |
| SafeWebCore.Analyzers | Absent |
| SafeWebCore.Testing | Absent |

---

## What is release-ready (strengths)

1. **Clean Release builds** with `TreatWarningsAsErrors` and modern analysis level.
2. **Solid automated tests** for core middleware, presets, CSP, diagnostics, metrics; fraud module covered for key paths.
3. **Packaging metadata** for core is production-grade (icon, readme, license, symbols, SourceLink flags, release notes).
4. **Public API tracking** via PublicApiAnalyzers on core + fraud (RS0037 hard error for removals).
5. **Backward compatibility policy** documented and referenced from CONTRIBUTING.
6. **Documentation breadth** is high: getting started, headers, CSP, presets, advanced, recipes, roadmap, examples, benchmarks.
7. **Examples and benchmarks compile**, supporting demos and perf regression work.
8. **Analyzer packaging layout** is correct for Roslyn (`analyzers/dotnet/cs`).
9. **SemVer + Keep a Changelog** discipline exists (`CHANGELOG.md`).
10. **License** (MIT) and repo metadata are consistent.


---

## Blockers / must-fix before publishing from HEAD

### 1. Version identity mismatch (blocker for core)

- nuget.org and csproj say **1.3.5**
- Workspace includes substantial **[Unreleased]** features (config binding, environment helpers, diagnostics endpoint, metrics, analyzer/testing packages, fraud sinks/risk, …)
- Publishing without a version bump would either illegally overwrite 1.3.5 semantics, or ship a package that **claims** 1.3.5 while exposing newer APIs

**Required:** Choose next version(s), bump csproj(s), rewrite CHANGELOG + PACKAGE.md accordingly.

### 2. Uncommitted release surface (blocker for any official release)

`git status` shows many modified tracked files and numerous untracked source/docs/test files. A release must be cut from a **clean, tagged commit** that reviewers can audit.

**Required:** Commit (or PR-merge) the intended release set; tag after publish decision.

### 3. No CI/CD workflows (process blocker for sustainable releases)

`.github/workflows/` is empty / missing. There is no automated restore → build → test on PR, pack verification, public API diff gate, or optional nuget push on tag.

**Required for mature OSS releases:** at least a PR CI workflow. Tag-based publish can follow.

### 4. Solution hygiene gaps

Not in `SafeWebCore.slnx`:

- `SafeWebCore.FraudDetection.Tests`
- benchmarks
- examples (optional)

Fraud tests can be forgotten in local `dotnet test` on the solution alone (this audit had to test that project explicitly).

**Required before FraudDetection publish:** add fraud test project to the solution (minimum).

---

## Non-blocking gaps (should-fix)

| Gap | Impact | Suggested action |
|-----|--------|------------------|
| FraudDetection missing icon / symbols / SourceLink / release notes | Weaker package UX | Mirror core csproj packaging block |
| Testing/Analyzers missing icons | Minor | Optional for preview |
| Floating package versions in Testing (`10.0.*`, `3.2.*`) | Non-reproducible restores | Pin before stable |
| No analyzer unit tests | Rule regressions possible | Add analyzer test project |
| No tests for Testing helpers | Helper regressions | Add small test project or cover via core tests |
| No FraudDetection sample app | Adoption friction | Add example or `docs/recipes/fraud-detection.md` |
| PACKAGE.md archive links still point at old paths | Broken links on nuget.org readme | Fix to `docs/archive/...` |
| PACKAGE.md / README version narrative lag Unreleased | Consumer confusion | Sync on release |
| Git tags `V1.0.0.0` … `V1.3.0.0` vs NuGet `1.3.5` | Hard to map source ↔ package | Adopt `v1.3.5` tags going forward |
| Public API RS0016/RS0017 still warnings-not-errors | Incomplete baseline enforcement | After curation, tighten |
| Test SDK version skew (18.4 vs 18.8) | Mild inconsistency | Align package versions |
| Large dirty tree includes `.tmp` public API file | Noise | Delete `PublicAPI.Shipped.txt.tmp` if leftover |
| Docs “Latest Features (v1.1.0+)” section aged | Docs freshness | Refresh on next release |

---

## Compatibility & API freeze checklist

Before cutting a release:

- [ ] Review `PublicAPI.Unshipped.txt` for core and fraud — only intentional additions
- [ ] Promote Unshipped → Shipped for the release
- [ ] Confirm **no** default/preset behavior changes without opt-in (policy)
- [ ] Confirm new APIs have XML docs
- [ ] Run full test suite (both test projects)
- [ ] Spot-check examples still demonstrate current recommended APIs
- [ ] Update CHANGELOG with version + date; clear Unreleased or leave only true WIP
- [ ] Update PACKAGE.md current version + “New in …”
- [ ] Update root README badges/version mentions if any
- [ ] Update every `**Current version:**` line — `README.md`, `PACKAGE.md`, and the packaged READMEs under `src/` (`SafeWebCore.FraudDetection`, `SafeWebCore.JwtBearer`, `SafeWebCore.Analyzers`, `SafeWebCore.Testing`)

---

## Suggested go / no-go matrix

| Goal | Go? | Conditions |
|------|-----|------------|
| Hotfix 1.3.5 → 1.3.6 | Only from clean 1.3.5 baseline | Do not include Unreleased feature commits |
| Release core **1.4.0** (DX slice) | **No-go until** version bump + commit + changelog + CI-or-manual checklist | Prefer shipping only v1.4-scoped items if you want clean roadmap alignment |
| Release core **1.6.0** (everything Unreleased) | **No-go until** same process; acceptable if changelog clearly lists all themes | Single big drop OK if SemVer + docs honest |
| First publish FraudDetection **1.0.0** | **Go soon** after packaging polish + fraud tests in sln + git clean | Independent of core bump |
| Publish Analyzers **preview.1** | **Go** after README install note + decision on core dependency messaging | Preview prerelease flag clear |
| Publish Testing **preview.1** | **Go after** core version that Testing needs is on nuget.org | Avoid depending on unpublished core APIs |


---

## Minimal path to "release ready" (recommended order)

1. **Decide release train** (see [nuget-packages.md](nuget-packages.md) Train B or C).
2. **Add** `SafeWebCore.FraudDetection.Tests` to `SafeWebCore.slnx`.
3. **Add** GitHub Actions: build + test (+ optional pack).
4. **Bump versions** appropriately; never reuse 1.3.5 for new APIs.
5. **Finalize CHANGELOG** + PACKAGE.md + package READMEs.
6. **Commit** all intended files; ensure `git status` clean.
7. **Run:**

   ```bash
   dotnet build SafeWebCore.slnx -c Release
   dotnet test tests/SafeWebCore.Tests -c Release
   dotnet test tests/SafeWebCore.FraudDetection.Tests -c Release
   dotnet pack <projects> -c Release -o artifacts/nupkg
   ```

8. **Smoke-install** packed packages into a throwaway app.
9. **Tag** (`v1.4.0`, `SafeWebCore.FraudDetection-v1.0.0`, etc. — pick a convention and document it).
10. **Push** to nuget.org; create GitHub Release notes from CHANGELOG.

---

## Security / quality posture for release

| Item | Notes |
|------|-------|
| Warnings as errors | Enabled globally |
| Nullable | Enabled |
| Secrets in repo | None observed in packaging config |
| Middleware hot path | Benchmarks exist; no automated perf gate in CI yet |
| Supply chain | Pack uses SDK defaults; consider locking package versions in Testing |
| Analyzer preview | Does not execute at runtime — low risk |

---

## Audit evidence snapshot

```text
SDK:            .NET 10.0.302
Solution build: 0 warning(s), 0 error(s)
Tests:          115 passed
Pack:           4/4 packable projects OK
nuget.org:      SafeWebCore only
CI workflows:   none
Git:            dirty working tree with Unreleased feature work
```

---

## Related docs

- [Project catalog](projects.md) — every project explained
- [NuGet packages & candidates](nuget-packages.md) — package-level readiness and push order
- [Roadmap](roadmap.md) — feature themes by version band
- [Backward compatibility policy](development/backward-compatibility-policy.md)
- [SonarCloud triage](development/sonarcloud-triage.md) — analysis scope, triage rules and the accepted findings behind the zero-issue gate
