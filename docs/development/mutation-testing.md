# Mutation testing (Stryker.NET)

Mutation testing asks a different question than code coverage. Coverage says a line ran; mutation testing says the
tests would *notice* if that line changed. Stryker.NET rewrites the compiled library one small change at a time
(`>=` → `>`, a string literal → `""`, a statement deleted) and reruns the suite: a mutant that makes a test fail is
**killed**, one that survives is a hole in the suite.

This repository runs mutation testing explicitly, like the benchmarks — never inside `dotnet test` and never in CI.

## Tooling

Stryker.NET is pinned as a **local** tool so the version travels with the repository:

```bash
dotnet tool restore          # once per clone, and after a pull that changes .config/dotnet-tools.json
dotnet stryker --version     # 5.1.0
```

## Running a suite

Stryker must run **from the test project directory**: a `stryker-config.json` lives there and is picked up
automatically.

```bash
cd tests/SafeWebCore.Tests
dotnet stryker -O ../../artifacts/stryker/safewebcore
```

| Test project | Mutates | Config file |
|--------------|---------|-------------|
| `tests/SafeWebCore.Tests` | `src/SafeWebCore` | `tests/SafeWebCore.Tests/stryker-config.json` |
| `tests/SafeWebCore.FraudDetection.Tests` | `src/SafeWebCore.FraudDetection` | `tests/SafeWebCore.FraudDetection.Tests/stryker-config.json` |
| `tests/SafeWebCore.JwtBearer.Tests` | `src/SafeWebCore.JwtBearer` | `tests/SafeWebCore.JwtBearer.Tests/stryker-config.json` |

`SafeWebCore.Analyzers` and `SafeWebCore.Testing` have no test project, so nothing mutates them.

Reports land in `artifacts/stryker/<name>/<timestamp>/reports/` (git-ignored): `mutation-report.json` for tooling,
`mutation-report.html` to browse a file's mutants, plus the console table.

## Why the configuration looks the way it does

| Setting | Value | Reason |
|---------|-------|--------|
| `project` | e.g. `SafeWebCore.csproj` | Names the project under test explicitly, even though each test project has one project reference. |
| `target-framework` | `net10.0` | The libraries multi-target `net10.0;net11.0` and Stryker mutates one build at a time. Both targets compile the same sources, so one target covers the code. |
| `test-runner` | `mtp` | xUnit v3 runs on the Microsoft.Testing.Platform (`global.json`), not vstest. Without this, Stryker finds zero tests. |
| `mutation-level` | `Standard` | The default mutator set. `Advanced` costs several times the runtime for little extra signal on this code. |
| `thresholds` | high 80, low 60, **break 0** | Reported so a score is readable, but a run never fails: today's scores are a backlog, not a gate. |
| `-O` on the command line | `artifacts/stryker/<name>` | `output` is command-line-only in 5.1.0; it is rejected inside `stryker-config.json`. |

Two practical constraints:

- **Run from the test project folder.** From the repository root Stryker picks up `SafeWebCore.slnx` and then rejects
  `--test-project` with *"Test projects cannot be specified when running Stryker in solution context"*. One config file
  per test project avoids solution mode entirely.
- **`test-runner: mtp` is not optional.** The xUnit v3 projects have no vstest adapter configured, so the default runner
  reports no tests; the config sets the runner for you.

## Baseline results — 2026-11-15

| Package | Mutation score | Killed | Survived | No coverage | Compile error | Wall time |
|---------|----------------|--------|----------|-------------|---------------|-----------|
| `SafeWebCore` | **53.53 %** | 440 | 255 | 127 | 49 | ~30 s |
| `SafeWebCore.JwtBearer` | **57.43 %** | 142 + 1 timeout | 66 | 40 | 51 | ~30 s |
| `SafeWebCore.FraudDetection` | **20.68 %** | 131 + 2 timeouts | 354 | 156 | 21 | ~27 s |

The score is `(killed + timeout) / (killed + timeout + survived + no coverage)`; a `Compile error` mutant is excluded.
Compile errors come from mutations Stryker cannot compile — `Append()` → `Prepend()` (no such member on
`IHeaderDictionary`), `Count()` → `Sum()` on a non-numeric type — and say nothing about the tests.

### What the survivors say

| Area | Surviving mutations | Reading |
|------|---------------------|---------|
| `SecurePresets` | 45 string mutations | Tests assert that a preset emits a header, not its exact value. Pinning the documented values (for example `default-src 'self'` against the mutant `default-src ''`) kills them. |
| `NetSecureHeadersMiddleware`, `ServiceCollectionExtensions` | statement mutations | Deleting one `options.X = ...` assignment or one registration line goes unnoticed, because assertions cover the resulting header set rather than the option-by-option wiring. |
| `NetSecureHeadersOptionsValidator` | no coverage + logical | Validation failures (duplicate prefixes, library-owned header names) are only partly exercised. |
| `WesternDetectorOptions` | 134 survived | Option defaults and assignments are barely asserted; asserting the documented defaults kills most of them. |
| `SuspicionScorer` | 89 survived | Score weights are not pinned: a changed weight still produces a "suspicious" verdict, so no test fails. |

Every survivor maps to a test somebody could add, which is why the score is recorded per package instead of as one
number.

## Reading a report

- **Killed** — the suite noticed the change; the good case.
- **Survived** — the change is invisible to the tests: add an assertion, or accept it here with a reason.
- **No coverage** — no test reaches the code: a missing scenario, or dead code.
- **Timeout** — counts as detected: the mutant made the suite hang, which is a failure too.
- **Compile error** — neither killed nor survived; ignore it when reading a score.

Improve one file at a time and re-run that suite (about 30 seconds each). Chasing the percentage without naming the
survivor wastes time: the score only moves when a *specific* mutant dies.

## Why this is not in CI

A run is fast today (~30 s per suite) but grows with the test count, and a mutation score is a trend, not a gate. The
configs carry `break: 0` so a run can never fail a build by itself. If this ever becomes a nightly check, add a separate
`workflow_dispatch` workflow that runs `dotnet tool restore` plus the three commands above and uploads
`artifacts/stryker/**` — do not add it to `.github/workflows/ci.yml`, which every pull request waits for.

