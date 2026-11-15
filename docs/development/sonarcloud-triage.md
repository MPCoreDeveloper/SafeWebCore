# SonarCloud Triage

The repository keeps its [SonarCloud issue list](https://sonarcloud.io/project/issues?id=MPCoreDeveloper_SafeWebCore) at **zero open issues** with a green quality gate, and — because this is a security library — **all four ratings at A**. That is a release gate, not housekeeping: see principle VII of the [constitution](../../.specify/memory/constitution.md), `AGENTS.md`, and the release checklist in [nuget-packages.md](../nuget-packages.md). This page records how the analysis is scoped, how a new finding is triaged, and which findings are **deliberately accepted**, so nobody has to re-investigate them.

---

## Analysis scope

| Fact | Value |
|------|-------|
| Project | `MPCoreDeveloper_SafeWebCore` |
| Mode | SonarCloud Automatic Analysis (GitHub App) — there is no CI-based scan step |
| **Effective scope** | The project's **Exclusions** setting on the server (set 2026-11-15): `.specify/**`, `.clinerules/**`, `specs/**`, `docs/**`, `artifacts/**`, `**/bin/**`, `**/obj/**` |
| In scope | `src/**`, `tests/**`, `examples/**`, `benchmarks/**` and `.github/**` |
| Mirrored in | [`sonar-project.properties`](../../sonar-project.properties) and [`.sonarcloud.properties`](../../.sonarcloud.properties) — for the record and for a future CI-based scan |
| Quality gate | ratings A on new code, new duplicated lines < 3 %, security hotspots reviewed |
| **Release gate** | **all four overall ratings `1.0` (A) and 0 open issues** — a security library does not ship below A |

**The server-side setting is what counts, and that is where this page used to be wrong.** Automatic Analysis reads
`.sonarcloud.properties` from the repository root and ignores `sonar-project.properties` (that is the scanner's file), but
the project's own **Exclusions** setting takes precedence over both. Until 2026-11-15 that setting was an
organisation-level inheritance still listing the deleted BMad directories (`.agents/**`, `_bmad/**`, `_bmad-output/**`)
plus `docs/**`, so every repo-file exclusion this page described had never removed anything: the analyses of commit
`1b7198e` kept reporting seven findings in `.specify/scripts/powershell/common.ps1` while `.specify/**` sat in both
properties files. The project-level override now carries the real list.

Only product code belongs in scope. The Spec Kit tooling (the `.specify` PowerShell scripts and templates, the
`.clinerules` command files) and the specs it renders are vendored or generated: analysing them once produced 325 of 327
issues, all six vulnerabilities and the 99 % copy-paste blocks, which failed the gate on 58.5 % duplicated lines.
`docs/**` is excluded for the same reason, which is why this page is not part of the analysis.

`.github/**` stays **in** scope on purpose: `githubactions:S7637` reports an unpinned third-party action as a
vulnerability, and that check is what keeps every non-GitHub action pinned to a full commit SHA.

### Verified state — 2026-11-15, all four ratings A

| Metric | Value |
|--------|-------|
| `reliability_rating` | **1.0 (A)** |
| `security_rating` | **1.0 (A)** — was `3.0` (C) until `NuGet/login` was pinned to a commit SHA |
| `sqale_rating` (maintainability) | **1.0 (A)** |
| `security_review_rating` (security hotspots) | **1.0 (A)** — 0 hotspots |
| `bugs` / `vulnerabilities` / `duplicated_lines_density` | `0` / `0` / `0.7 %` |

The step from C to A on security was **one** finding: `githubactions:S7637` on `.github/workflows/publish-nuget.yml` for
`uses: NuGet/login@v1`. GitHub-owned `actions/*` references are exempt from that rule, third-party actions are not, and a
single vulnerability is enough to hold the entire rating at C. It now reads
`uses: NuGet/login@8d196754b4036150537f80ac539e15c2f1028841 # v1`.

## Local pre-check before pushing

SonarCloud only sees what is on `master`, and the analysis lags a push. The C# rules it applies can be run locally against the working tree, because the analyzer package is wired into `Directory.Build.props` behind an opt-in property:

```powershell
dotnet build SafeWebCore.slnx -p:SonarProbe=true -p:TreatWarningsAsErrors=false
```

- `SonarProbe=true` adds `SonarAnalyzer.CSharp` (`10.36.0.15337`) as a private analyzer; without the property the package is not referenced at all, so normal builds stay fast and unchanged.
- `TreatWarningsAsErrors=false` is required: every Sonar finding arrives as a warning, and the repository build normally promotes warnings to errors.
- Expect each finding **twice** — once per target framework — and ignore the `[project]` suffix. Add `| Select-String 'warning S\d+:'` to see only Sonar's diagnostics.
- This is a pre-check, not the source of truth: the dashboard still owns the resolutions, and a rule can be newer than the analyzer version pinned here.

The probe run of **2026-11-15** reported exactly the eight `Won't fix` findings in the table below and nothing else. The two `S3251` false positives do not appear because the code already carries `// NOSONAR` on the `[LoggerMessage]` declarations and call sites.

---

## Triage rules

1. **Fix it in code when the fix is additive and behavior-preserving** — extract the duplicated literal into a constant, drop the unused local, remove the redundant null-forgiving operator, replace a blocking call with its async counterpart. The 17 issues closed that way in the September 2026 pass needed no API change.
2. **Never break the public surface to silence a rule.** A finding on a member listed in `PublicAPI.Shipped.txt` cannot be "fixed" by deleting, renaming or re-meaning it: that is a breaking change and `RS0037` makes it a build error. See [Backward Compatibility Policy](backward-compatibility-policy.md).
3. **Mark, don't hide.** An accepted finding gets a SonarCloud resolution that matches reality (`Won't fix` or `False positive`) **plus a comment** that names the rule, the policy and the plan. A blanket rule suppression would also hide future real findings, so it is not used.
4. **Record it here** in the table below, so the next person sees the decision instead of re-deriving it.
5. **Re-check after a refactor.** SonarCloud re-anchors an issue on the line where the rule still fires; a bigger move of the surrounding code can surface it again as new. Re-run the verification query after such a change and re-apply the marking where needed.

---

## Accepted findings

Ten findings are accepted as-is: eight `Won't fix` and two `False positive`. The replacement for the eight `[Obsolete]`-related ones is a v2.0 decision; the two false positives come from generated code that SonarCloud cannot see.

| Rule | Location | Resolution | Why it stays |
|------|----------|------------|--------------|
| `S1133` | `CspOptions.cs` — `ReportUri` | Won't fix | Shipped public API (`PublicAPI.Shipped.txt`), still emits the `report-uri` directive when configured |
| `S1133` | `CspOptions.cs` — `EnableBlockAllMixedContent` | Won't fix | Shipped public API, still emits `block-all-mixed-content` when enabled |
| `S1133` | `FraudVerdict.cs` — `FakeWestern` | Won't fix | Shipped enum member, still produced by `WesternImpersonationDetector` |
| `S1133` | `WesternDetectorOptions.cs` | Won't fix | Shipped type, "remains fully supported for backward compatibility"; `GeoCulturalConsistencyOptions` ships additively |
| `S1133` | `FraudDetectionServiceCollectionExtensions.cs` | Won't fix | Shipped registration overload; removing it breaks existing registrations |
| `S1133` | `WesternImpersonationDetector.cs` | Won't fix | Shipped type, fully functional; `GeoCulturalConsistencyDetector` ships additively |
| `S2325` | `NonceService.cs` — `TryWriteNonce` | Won't fix | `static` would make existing instance calls fail to compile (`CS0176`); the body already allocates nothing |
| `S2094` | `AnalyzerPackageMarker.cs` | Won't fix | Intentional public marker type for the analyzers package scaffold; the analyzer work is on the roadmap |
| `S3251` | `LoggingSecurityEventSink.cs` — declaration | False positive | The `[LoggerMessage]` source generator supplies the implementation |
| `S3251` | `LoggingSecurityEventSink.cs` — call site | False positive | Same generated implementation, so the call is not elided |

Each of these carries the matching comment in SonarCloud itself, including the evidence for the two false positives.

### Fixed by the local pre-check (2026-11-15)

The pre-check surfaced four findings that were in the working tree but not yet on the dashboard. All four were fixed in code rather than marked, because none of them needed a public-surface change:

| Rule | Location | Fix |
|------|----------|-----|
| `S9422` | `LoggingConfigurationManager.cs` — `_sync` | Guarded with `System.Threading.Lock` instead of `object`; the compiler emits `EnterScope`/`ExitScope` for it, which is cheaper than `Monitor` and marks the field's purpose. Internal field, no API change. |
| `S2699` | `JwtAuthorityValidationGuardTests.cs` — `StartAsyncPermanentFailureWithoutFailFastCompletes` | The test now captures the exception and asserts `Assert.Null(exception)`: "completes without throwing" is an assertion, not a missing one. |
| `S2699` | `JwtAuthorityValidationGuardTests.cs` — `StartAsyncSuccessCompletes` | Same pattern. |
| `S1481` | `NetSecureHeadersMiddlewareTests.cs` — unused local `metrics` | Removed; the metric is observed through a `MeterListener`, and the middleware constructs its own `SafeWebCoreMetrics`, so resolving it changed nothing. |

### Fixed from the dashboard (2026-11-15)

The dashboard's analyzers are newer than the pinned `SonarAnalyzer.CSharp` (`10.36.0.15337`), so four findings existed
only there. All four were fixed in code:

| Rule | Location | Fix |
|------|----------|-----|
| `githubactions:S7637` **(vulnerability)** | `.github/workflows/publish-nuget.yml` — `NuGet/login@v1` | Pinned to the commit of the `v1` tag (`8d196754…`, tag kept in a trailing comment). This single finding held `security_rating` at **C**; `actions/*` references are exempt, third-party actions are not. |
| `csharpsquid:S1125` | `JwtBearerHardeningApplication.cs` — `context.Result?.Succeeded == false` | Replaced with the property pattern `context.Result is { Succeeded: false }`: same semantics (a `null` result still falls through) without the boolean literal. |
| `csharpsquid:S8969` ×3 | `JwtBearerHardeningTests.cs` | Dropped three null-forgiving operators the compiler does not need. |

The lesson for the local pre-check: it covers the C# rules but not YAML/GitHub Actions, and a rule added after the pinned
analyzer version will only show up on the dashboard. Pin **every** third-party action to a commit SHA, not just the ones
a scan happens to flag.

---

## Marking an accepted finding

The marking is done through the Web API so the comment and the resolution stay together. `SONAR_TOKEN` is the same token the project already uses; it never belongs in the repository.

```powershell
$h = @{ Authorization = "Bearer $env:SONAR_TOKEN" }
$issue = 'AaBEzOTH4YzCRoHR2Yha'   # from the dashboard URL or the query below

Invoke-RestMethod -Method Post -Uri 'https://sonarcloud.io/api/issues/add_comment' `
  -Headers $h -Body @{ issue = $issue; text = 'Won''t fix: shipped public API ...' }
Invoke-RestMethod -Method Post -Uri 'https://sonarcloud.io/api/issues/do_transition' `
  -Headers $h -Body @{ issue = $issue; transition = 'wontfix' }   # or 'falsepositive'
```

Verification — the two queries a release check should run:

```powershell
# 1. nothing is open
Invoke-RestMethod 'https://sonarcloud.io/api/issues/search?componentKeys=MPCoreDeveloper_SafeWebCore&resolved=false&ps=1' -Headers $h | Select-Object total

# 2. the split over FIXED / WONTFIX / FALSE-POSITIVE
Invoke-RestMethod 'https://sonarcloud.io/api/issues/search?componentKeys=MPCoreDeveloper_SafeWebCore&resolved=true&ps=1&facets=resolutions' -Headers $h |
  ForEach-Object { $_.facets[0].values }

# 3. the gate itself
Invoke-RestMethod 'https://sonarcloud.io/api/qualitygates/project_status?projectKey=MPCoreDeveloper_SafeWebCore' | Select-Object -ExpandProperty projectStatus -Property status
```

The `FALSE-POSITIVE` and `WONTFIX` counts should match the table above; a higher `FIXED` count is expected because every release adds fixed issues.

---

## Proving a false positive in generated code

`S3251` ("this call is unused") fires on `LoggingSecurityEventSink` because the `[LoggerMessage]` generator writes the method body outside the file SonarCloud analyses. Do not reason about it — generate the file and read it:

```powershell
dotnet build src/SafeWebCore/SafeWebCore.csproj -c Release `
  -p:EmitCompilerGeneratedFiles=true -p:CompilerGeneratedFilesOutputPath=obj/gen
Get-Content src/SafeWebCore/obj/gen/Microsoft.Extensions.Logging.Generators/*/LoggerMessage.g.cs | Select-String LogSecurityEvent
```

The generated file contains the real body:

```csharp
static partial void LogSecurityEvent(ILogger logger, string eventType, string path, DateTimeOffset timestamp)
{
    __LogSecurityEventCallback(logger, eventType, path, timestamp, null);
}
```

`obj/gen` is build output under the already-ignored `obj/`, so the working tree stays clean.

---

## Out of scope

`dotnet format` is not part of CI or of the gate. In the September 2026 pass it reported pre-existing `ENDOFLINE`, `IDE0005` and `IDE1006` diagnostics that predate the SonarCloud work; they are not accepted findings but a separate cleanup, and the SonarCloud gate stays green without them. Treat a `dotnet format` run as a standalone task, not as a release requirement.
