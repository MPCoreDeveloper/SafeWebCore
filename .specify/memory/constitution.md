# SafeWebCore Constitution

SafeWebCore is a .NET 10 security-header middleware family for ASP.NET Core that aims for an A+ on
[securityheaders.com](https://securityheaders.com) out of the box. This constitution states the non-negotiable rules every
specification, plan, task list and pull request in this repository is checked against. It is derived from
[`AGENTS.md`](../../AGENTS.md), [Backward Compatibility Policy](../../docs/development/backward-compatibility-policy.md)
and [CONTRIBUTING.md](../../CONTRIBUTING.md), which stay the detailed references.

## Core Principles

### I. Backward Compatibility Is a Hard Gate (NON-NEGOTIABLE)

Every normal release keeps 100% backward compatibility for existing consumers: no public API, option, preset,
configuration path, header value or default that a consumer depends on may be dropped, renamed or re-meant.

- A specification that needs a breaking change is rejected, or re-scoped as additive; a true major-version decision
  must be named explicitly as an approved exception to the policy before planning starts.
- Existing registration methods, preset helpers, option names and semantics, default values and
  `appsettings.json` binding patterns keep working exactly as before.
- New behavior arrives as a new option, overload or helper alongside the old path, never as a re-interpretation of
  the old one.
- Removing or renaming a symbol listed in `PublicAPI.Shipped.txt` is a build error (`RS0037`) and therefore a plan
  defect, not a review finding.

**Rationale:** consumers upgrade a security middleware inside production applications; a silent behavior change is a
security incident, not an inconvenience.

### II. Additive, Opt-In Public Surface

New capability enters the public API only deliberately, documented and tracked.

- New public types and members ship with XML documentation in the same change.
- Intentional new public API goes to `PublicAPI.Unshipped.txt` for `SafeWebCore`, `SafeWebCore.FraudDetection` and
  `SafeWebCore.JwtBearer` (the only three packages with baselines) and is promoted to `PublicAPI.Shipped.txt` at
  release.
- `RS0016` and `RS0017` stay warnings during the PublicAPI rollout; a plan may not rely on them being errors, and may
  not silence them with `#pragma` or `NoWarn`.
- `SafeWebCore.Analyzers` and `SafeWebCore.Testing` stay outside the runtime graph; nothing in the family references
  the analyzers.

**Rationale:** the package family is published on nuget.org; the baseline files are the only mechanical proof that the
promise in Principle I holds.

### III. Tests Are the Contract

A change is done when the tests that describe it exist and the whole suite is green.

- Tests use xUnit v3 on the Microsoft.Testing.Platform runner, one suite per package under `tests/`, and every suite runs
  on every target framework the libraries ship (`net10.0` and `net11.0`).
- `dotnet test` runs all suites and must pass before a change is called done; a red suite is never "expected", never
  skipped and never excluded to get a green run.
- Warnings break the build (`TreatWarningsAsErrors=true`), so a plan that expects new warnings is a plan defect.
- Security-relevant behavior is proven by assertions on emitted headers, not by inspection: every new header, directive
  or option needs a test that reads the response.
- Benchmarks in `benchmarks/SafeWebCore.Benchmarks` are run explicitly during performance work and never inside the
  normal test loop.

**Rationale:** the library's observable behavior is a set of response headers; only assertions make a change reviewable.

### IV. Documentation Ships With the Change

User-visible behavior and repository workflow are documented in the same pull request that changes them.

- Feature docs stay in step: `docs/getting-started.md`, `docs/security-headers.md`, `docs/csp-configuration.md`,
  `docs/presets.md`, `docs/advanced-configuration.md`, `docs/projects.md`, plus `docs/README.md` as the index.
- `CHANGELOG.md` (Keep a Changelog + Semantic Versioning) gains an entry for user-visible changes, and each entry states
  the compatibility verdict.
- Public API without XML documentation is treated as undocumented API and fails review.
- Documentation never promises behavior the tests do not cover.

**Rationale:** the documentation is the product's contract with consumers; a header table that drifts from the emitted
headers is worse than no table.

### V. Zero-Allocation Hot Path

Middleware cost is paid on every request, so per-request allocation is designed, not discovered.

- Prefer `Span<char>`/`TryWrite...` paths and startup-only computation (pre-built header and CSP templates) over
  per-request string building.
- A change that adds allocation to the request path must state the allocation and justify it in the plan.
- Performance claims are backed by a benchmark, never estimated.

**Rationale:** the middleware sits in front of every response; a per-request allocation multiplied by production traffic
is a real regression.

### VI. Headers Only Where They Belong

Browser-oriented headers are not decoration to spread across every endpoint.

- API endpoints emit only the response headers they need; browser-oriented headers (CSP, frame, referrer, permissions
  policy) are not added to API routes.
- A preset stays the curated combination it documents; a specification may not quietly widen a preset's output.
- Report-only behavior is opt-in through the `ForEnvironment` overloads; production enforcement is not silently
  downgraded.
- In `SafeWebCore.FraudDetection`, notifications stay injectable and event-driven: never hard-code a mail client and
  never reduce notifications to logging only, so a consumer can plug in its own mail module.

**Rationale:** a header on the wrong route is a false sense of security or a broken client, and a hard-coded
notification channel takes an integration decision away from the consumer.

## Additional Constraints

- **Toolchain comes from the projects, not the shell**: .NET 11 SDK, C# 15 (`LangVersion=preview`), `Nullable` and
  `ImplicitUsings` on, `TreatWarningsAsErrors=true` in `Directory.Build.props`, xUnit v3 on the
  Microsoft.Testing.Platform runner via `global.json`. Runtime, test and example projects multi-target `net10.0` and
  `net11.0`; `SafeWebCore.Analyzers` stays `netstandard2.0` because Roslyn loads it into the compiler host.
- **Package independence**: no `ProjectReference` or `PackageReference` between `SafeWebCore.*` packages, except
  `SafeWebCore.Testing` → `SafeWebCore`. Shared behavior is promoted into `SafeWebCore` or deliberately duplicated.
- **Naming and style** follow `.editorconfig`; asynchronous methods end in `Async` (reported as a suggestion, so the
  reviewer is the gate).
- **Commits** are Conventional Commits with a module scope, as in the history: `fix(JwtBearer): ...`,
  `docs(README): ...`.
- **Contributions** land on `master` through a pull request; CI (`.github/workflows/ci.yml`) restores, builds and tests
  in Release and then packs all five packages.
- **Repository tooling is not product code**: `.specify/**`, `.clinerules/**`, `docs/**` and `specs/**` are excluded from
  the SonarCloud analysis scope and from GitHub's language statistics; product code lives in `src/`, `tests/`,
  `examples/` and `benchmarks/`.

## Development Workflow

Work is planned and reviewed through the Spec Kit workflow in `.specify/`, driven by the commands installed in
`.clinerules/workflows/`:

| Step | Command | Produces |
|------|---------|----------|
| Principles | `/speckit-constitution` | amendments to this document, only when a principle is genuinely missing |
| Specify | `/speckit-specify` | `specs/<NNN>-<short-name>/spec.md` — what and why, with measurable success criteria |
| Clarify *(optional)* | `/speckit-clarify` | resolved ambiguities recorded in the spec |
| Plan | `/speckit-plan` | `plan.md` — technical approach, constitution check, verification strategy |
| Checklist *(optional)* | `/speckit-checklist` | requirement-quality checklists |
| Tasks | `/speckit-tasks` | `tasks.md` — ordered, reviewable tasks |
| Analyze *(optional)* | `/speckit-analyze` | cross-artifact consistency report before implementation |
| Implement | `/speckit-implement` | the change, with tests and documentation |
| Converge | `/speckit-converge` | remaining work appended as tasks when the codebase and a spec differ |

Rules for the workflow:

1. Every plan carries a **Constitution Check** section that names the principles it touches and how it satisfies them; a
   `VIOLATION` must be justified and approved by the maintainer before implementation.
2. A plan that requires a breaking change stops at Principle I and returns to `/speckit-specify`.
3. Specs describe behavior in terms of observable responses (headers, status, configuration), never in terms of internal
   implementation, so the tests mandated by Principle III follow directly from them.
4. `specs/` keeps its artifacts in version control; a specification is amended, not replaced, when scope is corrected.
5. `AGENTS.md` and `CONTRIBUTING.md` remain the runtime guidance for daily work; this constitution governs what a
   specification may promise.
6. Local and CI agree: `dotnet test` locally and the Release build in CI are both green before a pull request is merged.

## Governance

- This constitution supersedes ad-hoc practice for anything a specification or plan promises. Where it conflicts with
  `AGENTS.md`, `CONTRIBUTING.md` or the Backward Compatibility Policy, the stricter rule wins and the conflict is
  reported instead of silently resolved.
- Amendments require a pull request that states the reason, the affected principles and the migration impact for
  existing specs and plans, maintainer approval, and a `CHANGELOG.md` entry when consumer-visible behavior is involved.
- Versioning is semantic for the governance itself: MAJOR for a removed or re-meant principle, MINOR for a new principle
  or section, PATCH for wording that does not change meaning.
- Every pull request review verifies compliance; complexity must be justified in the plan, and a reviewer may reject a
  plan that adds a mechanism the spec does not need.
- Runtime development guidance lives in [`AGENTS.md`](../../AGENTS.md); feature workflow guidance lives in
  [docs/development/spec-kit-workflow.md](../../docs/development/spec-kit-workflow.md).

**Version**: 1.1.0 | **Ratified**: 2026-11-15 | **Last Amended**: 2026-11-15
