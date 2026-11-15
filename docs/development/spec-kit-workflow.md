# Spec Kit workflow

SafeWebCore is set up for [GitHub Spec Kit](https://github.com/github/spec-kit) — spec-driven development where the
specification, the plan, the task list and the verification of a change are files in this repository instead of chat
history.

- **Installed:** the `specify` CLI `1.2.2` (`specify-cli`, installed as a [uv](https://docs.astral.sh/uv/) tool), project
  scope, script type `ps`.
- **Agent integration:** `cline` — the default and the one Spec Kit declares multi-install safe. The generated commands
  live in `.clinerules/workflows/` and appear to Cline as the `/speckit-*` slash commands.
- **GitHub Copilot:** the Copilot integration is not multi-install safe, so it cannot coexist with Cline. Install it on
  demand with `specify integration switch copilot` (it writes `.github/skills/speckit-*/` and removes the Cline command
  files) and return with `specify integration switch cline`.
- **Health:** `specify integration status` reports `OK` (`cline`, multi-install safe yes, no modified or missing managed
  files) for this repository.

---

## What the setup added

| Path | Commit to git | Purpose |
|------|---------------|---------|
| `.specify/memory/constitution.md` | yes | The project constitution: principles I–VI, additional constraints, workflow rules, governance. |
| `.specify/templates/` | yes | The `spec`, `plan`, `tasks`, `checklist` and `constitution` templates the commands fill in. |
| `.specify/scripts/powershell/` | yes | Scripts the commands call: `create-new-feature.ps1`, `setup-plan.ps1`, `setup-tasks.ps1`, `check-prerequisites.ps1`, `resolve-template.ps1`, `common.ps1`. |
| `.specify/workflows/` | yes | The bundled `speckit` "Full SDD Cycle" workflow (`workflow.yml`) and its registry. |
| `.specify/integrations/` | yes | Manifests with the file list and hashes of the installed integration; `upgrade` and `status` read them. |
| `.specify/init-options.json`, `.specify/integration.json` | yes | The chosen integration and script type, and the schema state of the install. |
| `.specify/.gitignore` | yes | Ignores machine-local state only: `feature.json`, extension `local-config.yml`, downloaded integration packages. |
| `.clinerules/workflows/speckit-*.md` | yes | The ten commands Cline runs (`speckit-specify`, `speckit-plan`, `speckit-tasks`, `speckit-implement`, ...). |
| `specs/` | yes | One directory per feature: `spec.md`, `plan.md`, `tasks.md`, plus `research.md`, `data-model.md`, `contracts/`, `quickstart.md` and `checklists/` when the plan needs them. Created by the first `/speckit-specify`. |

## Prerequisites

| Tool | Why |
|------|-----|
| `specify` CLI | Generates the scaffolding and runs the `specify ...` commands. Install or update with `uv tool install specify-cli --from git+https://github.com/github/spec-kit.git` (or `specify self upgrade`). |
| PowerShell | The scripts in `.specify/scripts/powershell` are the `ps` flavour, this project's `--script ps` choice. Windows PowerShell 5.1 and `pwsh` both run them. |
| .NET 11 SDK | Building and testing SafeWebCore on both targets (see [CONTRIBUTING.md](../../CONTRIBUTING.md)); the .NET 10 runtime is needed as well for the net10.0 test leg. |

Node.js is **not** required.

## Commands

| Situation | Command | Writes |
|-----------|---------|--------|
| Establish or amend the principles a specification must satisfy | `/speckit-constitution` | `.specify/memory/constitution.md` |
| A change you understand, planned before code | `/speckit-specify` | `specs/NNN-short-name/spec.md` |
| Ambiguity in the spec before planning | `/speckit-clarify` | amendments to `spec.md` |
| Technical approach and verification strategy | `/speckit-plan` | `plan.md` (+ `research.md`, `data-model.md`, `contracts/`, `quickstart.md` when relevant) |
| Requirement-quality gates | `/speckit-checklist` | `specs/NNN-short-name/checklists/` |
| Break the plan into reviewable work | `/speckit-tasks` | `tasks.md` |
| Consistency check before coding | `/speckit-analyze` | a report in the conversation, no files |
| Build it | `/speckit-implement` | code, tests and documentation |
| The code is older than the spec | `/speckit-converge` | remaining work appended to `tasks.md` |
| Turn tasks into GitHub issues | `/speckit-taskstoissues` | GitHub issues |

The same cycle is also defined as an automation: `specify workflow list` shows the bundled `Full SDD Cycle (speckit)`
workflow, and `specify workflow run speckit` runs specify → plan → tasks → implement, pausing at a review gate between
the phases.

## Working with it

1. **Constitution first, and rarely after that.** [`.specify/memory/constitution.md`](../../.specify/memory/constitution.md)
   already states principles I–VI for this repository (backward compatibility, additive public surface, tests as the
   contract, documentation in lockstep, zero-allocation hot path, headers only where they belong). Run
   `/speckit-constitution` only when a principle is genuinely missing, not to restate a feature.
2. **Branch, then specify.** Feature directories are numbered sequentially (`001-`, `002-`, ...). Branch creation is a
   hook of the optional `git` extension and this repository has no extensions installed, so create the branch yourself
   as described in [CONTRIBUTING.md](../../CONTRIBUTING.md) before running `/speckit-specify`.
3. **Plan carries the Constitution Check.** `/speckit-plan` must state which principles the change touches and how it
   satisfies them; a `VIOLATION` needs a written justification and maintainer approval before implementation.
4. **Specs describe observable behavior** — headers, status codes, configuration — never internal structure. That keeps
   the tests of principle III derivable from the spec instead of from the implementation.
5. **Implement ends with the repository's own gates**: `dotnet test` green, warnings not introduced, XML documentation on
   new public API, `PublicAPI.Unshipped.txt` updated, `CHANGELOG.md` entry, docs in step.
6. **Keep the scaffolding fresh** without losing the constitution: `specify self check` (is a newer CLI available?),
   `specify self upgrade`, then `specify init --here --force` to re-copy shared scripts and templates — read the diff
   before committing, because the constitution and the generated commands are project files, not vendor files.

Verified commands, useful for troubleshooting:

```powershell
specify check                 # are the agent tools I need available?
specify integration status    # is the installed integration healthy? (OK for this repository)
specify integration list      # every integration, its status and whether it is multi-install safe
specify workflow list         # installed automation workflows
```

## Guardrails this repository already has

Spec Kit does not replace the repository rules; the commands and the constitution must respect them:

- **100% backward compatibility** — see the [Backward Compatibility Policy](backward-compatibility-policy.md) and the
  `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` tracking. Prefer additive, opt-in changes; a specification that
  needs a break stops at principle I and goes back to `/speckit-specify`.
- **Targets** — .NET 10 + .NET 11 (multi-targeted), C# 15 on the net11.0 target, xUnit v3 for tests, the `Async` suffix
  on asynchronous methods, XML documentation on public APIs, conventions from `.editorconfig`.
- **Existing instructions** — `.github/copilot-instructions.md` stays authoritative for GitHub Copilot and
  [`AGENTS.md`](../../AGENTS.md) for every coding agent; the constitution governs what a specification may promise.
- **Architecture decisions** — the [architecture spine](architecture/ARCHITECTURE-SPINE.md) (AD-1 to AD-19) is binding
  on plans: no sideways package references, one composition root, additive-only public API, and the rest.

## Repository tooling and code-analysis scope

Everything the setup added is committed, but none of it is product code: it is PowerShell, YAML and Markdown, and it
would otherwise enter the automated code metrics next to a few thousand lines of C#. Two root files keep it out.

| File | Effect |
|------|--------|
| `.gitattributes` | Marks `.specify/**` and `.clinerules/**` as `linguist-vendored` and `specs/**` plus `docs/**` as `linguist-documentation`, so GitHub's language bar reports the product (C#) instead of the tooling. The files stay in the tree and in the repository. |
| `sonar-project.properties`, `.sonarcloud.properties` | Declare the product-only scope (`sonar.sources` / `sonar.exclusions`) for the record and for a future CI-based scan. The scope Automatic Analysis actually applies is the project's **Exclusions** setting on the server — see [SonarCloud Triage](sonarcloud-triage.md). |

Adding another integration, or switching to Copilot, brings generated files with it (`.github/skills/**` for Copilot);
extend both entries so the metrics keep measuring the product.
