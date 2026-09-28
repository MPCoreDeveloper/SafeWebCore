# BMad Method workflow

SafeWebCore is set up for the [BMad Method](https://docs.bmad-method.org) (Agile AI-Driven Development) so that
planning, implementation, review and verification stay explicit and versioned in this repository instead of
living in chat history.

- **Installed:** BMad Method `6.13.0-next` from `bmad-code-org/BMAD-METHOD` (skills channel), project scope.
- **Runtime:** `_bmad/` created by `bmad setup` (`status: created`, `current: true`).
- **Tools:** the same skill files serve **Cline** and **GitHub Copilot**, because both read project skills from `.agents/skills`.

---

## What the setup added

| Path | Commit to git | Purpose |
|------|---------------|---------|
| `.agents/skills/bmad-*/`, `.agents/skills/bmod-*/` | yes | The 30 installed BMad skills (agents, planning, implementation, review, validation skills). |
| `skills-lock.json` | yes | Installed skill versions + source; used by `npx skills` to update. |
| `_bmad/config.toml` | yes | Team configuration (project name, output folders, artifact locations). |
| `_bmad/scripts/`, `_bmad/method/scripts/`, `_bmad/core-tools/scripts/` | yes | Runtime scripts the skills call (`setup.py`, `knowledge.py`, `tickets.py`, ...). Byte-identical copies of the packaged scripts; refreshed by `bmad setup`. |
| `_bmad/custom/` | yes | Team overrides. Personal answers go to `config.user.toml` and are ignored through `_bmad/custom/.gitignore`. |
| `_bmad-output/` | yes | Planning and implementation artifacts (briefs, specs, PRDs, epics, stories, retrospectives). Created on the first artifact. |

Verified BMad commands, useful for troubleshooting:

```bash
npx skills list                                                                          # installed skills and their agents
uv run --no-cache .agents/skills/bmad/scripts/setup.py --project-root . --skill .agents/skills/bmad --status
uv run --no-cache .agents/skills/bmad/scripts/knowledge.py --content --root .agents/skills
```

## Prerequisites

| Tool | Why |
|------|-----|
| Node.js 22+ / npm + Git | Installing and updating the skills (`npx skills`). |
| [uv](https://docs.astral.sh/uv/) | Running the BMad Python scripts. Required: `bmad setup` stops without it and BMad is never written another way. |
| .NET 10 SDK | Building and testing SafeWebCore (see [CONTRIBUTING.md](../../CONTRIBUTING.md)). |

## Install, update, repair

```bash
# install (already done in this repository)
npx skills add bmad-code-org/BMAD-METHOD --skill '*' --agent cline --agent github-copilot --copy -y

# update the skills, then refresh the project runtime
npx skills update
# then ask your coding agent to run: bmad setup

# status / doctor (read-only)
# ask your coding agent to run: bmad status
```

`--copy` is used instead of the default symlinks because creating symlinks on Windows requires Developer Mode or
elevated rights. Setup is an upsert: it repairs stale `_bmad` scripts and only asks questions that are still open.

## Working with it

Invoke a skill by name in your coding agent; a bare story, issue link or change request counts as input for `bmad-build`.

| Situation | Skill |
|-----------|-------|
| Set up or refresh the repository's agent instructions (`AGENTS.md`) | `bmad-project-context` |
| You know the change and it fits one session | `bmad-build` |
| Planning before a larger change | `bmad-product-brief`, `bmad-prd`, `bmad-spec` |
| Architecture decisions that keep parts consistent | `bmad-architecture` |
| Review a diff, PR or artifact that is not yours | `bmad-review` or `bmad-code-review` |
| Test an existing feature that has no coverage | `bmad-qa-generate-e2e-tests` |
| Close out an epic | `bmad-retrospective` |
| What to do next | `bmad` (help / status) |

`bmad-project-context` is the recommended first step for an existing codebase like this one and **has** been run:
`AGENTS.md` holds the verified project context. It is a conversational skill: you bring the rules (for example
backward compatibility, xUnit v3, XML documentation on public APIs) and it verifies the rest against the repository.
Re-run it when conventions, commands or the project layout change.

## Guardrails this repository already has

BMad does not replace the existing repository rules; the skills must respect them:

- **100% backward compatibility** — see [Backward Compatibility Policy](backward-compatibility-policy.md) and the
  `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` tracking. Prefer additive, opt-in changes.
- **Targets** — .NET 10 / C# 14, xUnit v3 for tests, `Async` suffix on asynchronous methods, XML documentation on
  public APIs, conventions from `.editorconfig`.
- **Existing instructions** — `.github/copilot-instructions.md` stays authoritative for Copilot; add new team rules to
  `AGENTS.md` through `bmad-project-context` instead of duplicating them in several files.

## Repository tooling and code-analysis scope

Everything the setup added is committed, but none of it is product code: it is Python, HTML and Markdown, and it
outweighs the C# sources in bytes. Two root files keep it out of the automated code metrics.

| File | Effect |
|------|--------|
| `.gitattributes` | Marks `.agents/**` and `_bmad/**` as `linguist-vendored` and `_bmad-output/**` plus `docs/**` as `linguist-documentation`, so GitHub's language bar reports the product (C#) instead of the tooling. The files stay in the tree and in the repository. |
| `sonar-project.properties` | `sonar.exclusions` for `.agents/**`, `_bmad/**`, `_bmad-output/**`, `docs/**` and build output, so the SonarQube Cloud quality gate (Automatic Analysis, GitHub App) only judges SafeWebCore's own code. |

Why this matters: `_bmad/scripts/` is a byte-identical copy of `.agents/skills/bmad/scripts/` (17 of 17 files,
262 KB). Analysing both trees counted every runtime script twice, which produced 99 % copy-paste blocks, 58.5 %
duplicated lines on "new code", all six reported vulnerabilities and 325 of 327 issues — none of them in
SafeWebCore. Keep both entries if you rename or remove these folders.
