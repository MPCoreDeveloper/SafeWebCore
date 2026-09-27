# Reviewer gate — technology and version fitness against ARCHITECTURE-SPINE.md

Every named technology and version in the spine was checked against reality: the local build and test run of 2026-09-27 (restore succeeded, so each pinned package exists), the .NET support policy page, and the NuGet flat-container API.

Verdict: **stack is sound, two statements in the draft are wrong and one question is understated.** No technology in the spine is obsolete or unfitting.

## High

**V1 — "Only SafeWebCore is on nuget.org" is false, and it was taken from a stale document.**
NuGet flat-container API, checked 2026-09-27:

| Package | Published versions |
| --- | --- |
| `SafeWebCore` | 1.0.0, 1.1.0, 1.2.0, 1.3.0, 1.3.5, 1.6.0, 1.7.0 |
| `SafeWebCore.FraudDetection` | 1.0.0 |
| `SafeWebCore.JwtBearer` | 1.0.0 |
| `SafeWebCore.Analyzers` | 1.0.0-preview.1 |
| `SafeWebCore.Testing` | 1.0.0-preview.1 |

All five are published. `docs/nuget-packages.md` (audited 2026-07-25) says the latest published `SafeWebCore` is 1.3.5, marks `FraudDetection` "Not published", and recommends shipping "next as 1.4.0" — all three are wrong, and the drift predates the 1.6.0 and 1.7.0 releases. Fix the Stack note, and raise the documentation item from hygiene to a real risk: it tells a maintainer not to republish versions that are already public.

## Medium

**V2 — The local SDK is on an unsupported-after-2026-10-13 release candidate.** `11.0.100-rc.1.26425.128` per `dotnet --version`. The published packages come from CI on `10.0.x`, so shipped artifacts are unaffected, but the spine should say the RC window next to the SDK row so nobody treats the local toolchain as durable. .NET 11 reaches GA on 2026-11-10.

**V3 — The version-form question is a release hazard, not a style question.** NuGet normalizes `1.0.0.0` to `1.0.0`, and `SafeWebCore.FraudDetection` 1.0.0 is already published. Keeping the four-part `<Version>` in that csproj means the next release reuses an existing package version. State this in the Open Question.

## Low

**V4 — Verified current, no action.** `net10.0` is LTS: released 2025-11-11, latest patch 10.0.12, end of support 2028-11-14. `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.11 exists and restores; 10.0.12 is the current patch, already noted as deferred hygiene.

**V5 — `Microsoft.AspNetCore.Mvc.Testing` `10.0.*` floats.** Under `Deterministic=true` a floating restore still makes a build non-reproducible across machines and dates. Already listed under Deferred; keep it there.
