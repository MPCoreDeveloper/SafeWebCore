# SafeWebCore — Release Readiness Assessment

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
