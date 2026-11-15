# Contributing to SafeWebCore

Thank you for your interest in contributing!

## How to Contribute

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

## Development Setup

### Prerequisites

- .NET 11 SDK (the .NET 10 runtime is also required, because `dotnet test` runs the net10.0 and net11.0 targets)
- Visual Studio 2026 or later / VS Code with C# Dev Kit

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test
```

### AI-assisted development (Spec Kit)

This repository is set up for [GitHub Spec Kit](https://github.com/github/spec-kit) (spec-driven development), so the
specification, the plan, the tasks and the verification of a change live in version control next to the code instead of
in chat history. The project scaffolding is in `.specify/`, the commands are generated for Cline into
`.clinerules/workflows/`, and feature artifacts are written to `specs/`.

- Install or update the tool: `uv tool install specify-cli --from git+https://github.com/github/spec-kit.git`
  (or `specify self upgrade` for an existing install), then `specify init --here --force` to refresh the shared
  scripts and templates.
- Work in order: `/speckit-specify`, `/speckit-plan`, `/speckit-tasks`, `/speckit-implement`; `/speckit-clarify`,
  `/speckit-checklist` and `/speckit-analyze` are optional quality gates, `/speckit-converge` reconciles a spec with
  existing code.
- GitHub Copilot instead of Cline: `specify integration switch copilot` replaces the generated commands with
  `.github/skills/speckit-*/`; `specify integration switch cline` switches back.
- The [constitution](.specify/memory/constitution.md) is authoritative for what a specification may promise. It does
  not override this file or the backward compatibility policy below; when the two disagree, the stricter rule wins.
- See [docs/development/spec-kit-workflow.md](docs/development/spec-kit-workflow.md) for the full workflow, commands and
  guardrails.

## Coding Standards

- Target .NET 10 and .NET 11 (multi-targeted), C# 15 (the repo builds with the .NET 11 SDK and `LangVersion=preview`)
- Use modern C# features (primary constructors, collection expressions, Lock class)
- Follow the conventions in `.editorconfig`
- Async methods must end with `Async` suffix
- All public APIs must have XML documentation
- All roadmap work must preserve 100% backward compatibility unless an explicit exception is approved
- **SonarCloud must stay all-A**: reliability, security, maintainability and security-hotspot ratings at `1.0` with zero
  open issues, because a security library graded below A undermines the claim it makes. Fix a finding; mark one
  `Won't fix` only when it is shipped API, generated code or vendored tooling, and record it in
  [docs/development/sonarcloud-triage.md](docs/development/sonarcloud-triage.md) (constitution, principle VII).

## Backward Compatibility Policy

This repository treats backward compatibility as a hard requirement for normal releases.

- Read [docs/development/backward-compatibility-policy.md](docs/development/backward-compatibility-policy.md) before changing public APIs, defaults, presets, or configuration behavior.
- Prefer additive, opt-in changes over behavioral changes to existing consumers.
- Keep existing registration methods, presets, and configuration paths working.

### Public API surface changes

We use `Microsoft.CodeAnalysis.PublicApiAnalyzers` to track public surface via `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`.

- Removing or renaming a symbol listed in `PublicAPI.Shipped.txt` is a hard error (`RS0037`).
- During the initial adoption phase, new undeclared public symbols (`RS0016`) and symbols listed but not found (`RS0017`) are warnings.
- Add intentional new public API to `PublicAPI.Unshipped.txt` and promote it to `Shipped.txt` on release.
- See the library `.csproj` files for the current analyzer configuration.

## Reporting Issues

Use [GitHub Issues](https://github.com/MPCoreDeveloper/SafeWebCore/issues) with the provided templates.

## Code of Conduct

Be respectful and constructive. We follow the [Contributor Covenant](https://www.contributor-covenant.org/).
