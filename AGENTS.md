# SafeWebCore

.NET 10 / .NET 11 security-header middleware for ASP.NET Core (multi-targeted, C# 15 on the net11.0 target), aiming for
an A+ on securityheaders.com. Libraries in `src/` (SafeWebCore, .FraudDetection, .JwtBearer, .Analyzers, .Testing),
xUnit v3 tests in `tests/`, samples in `examples/`, BenchmarkDotNet in `benchmarks/`. Docs index: `docs/README.md`.
Spec-driven workflow: Spec Kit constitution `.specify/memory/constitution.md`, commands `.clinerules/workflows/`,
feature specs `specs/`, guide `docs/development/spec-kit-workflow.md`.

## Policy

- **100% backward compatibility for normal releases**: additive, opt-in changes only; never drop, rename or re-mean
  public API, existing defaults, presets or configuration paths. Read
  `docs/development/backward-compatibility-policy.md` before changing any of them.
- Never remove or rename a symbol listed in `PublicAPI.Shipped.txt` (RS0037 is a build error); add intentional new
  public API to `PublicAPI.Unshipped.txt`. Baselines exist for `SafeWebCore`, `SafeWebCore.FraudDetection` and
  `SafeWebCore.JwtBearer` only.
- New public API ships with XML documentation.
- `SafeWebCore.FraudDetection` notifications stay injectable and event-driven so a consumer can plug in its own mail
  module: never hard-code a mail client, never reduce notifications to logging only.
- API endpoints emit only the response headers they need; do not add browser-oriented headers to API routes.
- Contributions land on `master` through a pull request (`CONTRIBUTING.md`); CI (`.github/workflows/ci.yml`) restores,
  builds and tests in Release on pushes and pull requests to `master`/`main`, then packs all five packages.
- Publishing is keyless: `.github/workflows/publish-nuget.yml` uses nuget.org **trusted publishing** (GitHub OIDC
  exchanged for a one-hour API key) on a `v*` tag or a manual dispatch with the `publish` input. There is no API-key
  secret; only `NUGET_USER` is configured, and the nuget.org policy must name `publish-nuget.yml` exactly.

## Where things are

- Feature docs to keep in step with code changes: `docs/getting-started.md`, `docs/security-headers.md`,
  `docs/csp-configuration.md`, `docs/presets.md`, `docs/advanced-configuration.md`, `docs/projects.md`.
- Package and publish facts: `docs/nuget-packages.md`; release gate: `docs/release-readiness.md`.

## Running and verifying

- Toolchain comes from the projects, not your shell: .NET 11 SDK, `LangVersion=preview`, `Nullable` and
  `ImplicitUsings` on, `TreatWarningsAsErrors=true` (`Directory.Build.props`), xUnit v3 on the
  Microsoft.Testing.Platform runner (`global.json`). Every runtime, test and example project multi-targets
  `net10.0;net11.0`; `SafeWebCore.Analyzers` stays `netstandard2.0` because Roslyn loads it into the compiler host.
- `dotnet test` runs all three suites on both target frameworks (~380 test runs, seconds); the full run must be green
  before you call a change done. Both the .NET 10 and the .NET 11 runtime must be installed for that.
- Warnings break the build; RS0016/RS0017 are the only downgraded diagnostics, by design during the PublicAPI rollout. Sonar's C# rules can be previewed before pushing with `dotnet build SafeWebCore.slnx -p:SonarProbe=true -p:TreatWarningsAsErrors=false` (see `docs/development/sonarcloud-triage.md`).
- Benchmarks live in `benchmarks/SafeWebCore.Benchmarks` — run them explicitly, never inside the normal test loop. The
  host targets `net10.0` only: the pinned BenchmarkDotNet `0.15.8` does not recognise `net11.0`.
- Mutation testing is explicit as well: `dotnet tool restore`, then `dotnet stryker -O ../../artifacts/stryker/<name>`
  from a test project folder (configs in `tests/*/stryker-config.json`, baseline scores in
  `docs/development/mutation-testing.md`). Never wire it into `dotnet test` or CI.
- Adding a target framework is an all-or-nothing change: every runtime, test and example project moves together, and
  the README badge, `docs/projects.md`, `docs/nuget-packages.md` and `CHANGELOG.md` move with it.

## Conventions that differ from defaults

- Async methods end in `Async`; `.editorconfig` reports it as a suggestion only, so nothing stops you from forgetting.
- Commits are Conventional Commits with a module scope, as in the history: `fix(JwtBearer): ...`, `docs(README): ...`.
- `CHANGELOG.md` (Keep a Changelog + SemVer) gains an entry for user-visible changes; the PR checklist asks for it
  but no check fails when it is missing.

