<!-- bmad:context -->
<!-- Verified 2026-09-27 against 3411c52. Managed by bmad-project-context; edits inside this block are replaced on refresh. Keep anything you want preserved outside the markers. -->

# SafeWebCore

.NET 10 / C# 14 security-header middleware for ASP.NET Core, aiming for an A+ on securityheaders.com. Libraries in
`src/` (SafeWebCore, .FraudDetection, .JwtBearer, .Analyzers, .Testing), xUnit v3 tests in `tests/`, samples in
`examples/`, BenchmarkDotNet in `benchmarks/`. Docs index: `docs/README.md`. BMad skills: `.agents/skills`,
planning artifacts: `_bmad-output/`, workflow: `docs/development/bmad-workflow.md`.

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

## Where things are

- Feature docs to keep in step with code changes: `docs/getting-started.md`, `docs/security-headers.md`,
  `docs/csp-configuration.md`, `docs/presets.md`, `docs/advanced-configuration.md`, `docs/projects.md`.
- Package and publish facts: `docs/nuget-packages.md`; release gate: `docs/release-readiness.md`.

## Running and verifying

- Toolchain comes from the projects, not your shell: .NET 10 SDK, `LangVersion=preview`, `Nullable` and
  `ImplicitUsings` on, `TreatWarningsAsErrors=true` (`Directory.Build.props`), xUnit v3 on the
  Microsoft.Testing.Platform runner (`global.json`).
- `dotnet test` runs all three suites (~168 tests, seconds); the full suite must be green before you call a change done.
- Warnings break the build; RS0016/RS0017 are the only downgraded diagnostics, by design during the PublicAPI rollout.
- Benchmarks live in `benchmarks/SafeWebCore.Benchmarks` — run them explicitly, never inside the normal test loop.

## Conventions that differ from defaults

- Async methods end in `Async`; `.editorconfig` reports it as a suggestion only, so nothing stops you from forgetting.
- Commits are Conventional Commits with a module scope, as in the history: `fix(JwtBearer): ...`, `docs(README): ...`.
- `CHANGELOG.md` (Keep a Changelog + SemVer) gains an entry for user-visible changes; the PR checklist asks for it
  but no check fails when it is missing.

<!-- /bmad:context -->
