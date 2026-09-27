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

## Addendum — 2026-09-27b: V3 decided

- **V3 is closed by AD-17.** Three-part SemVer is canonical, the four-part form is dropped, `SafeWebCore.FraudDetection` moves to a higher three-part version at its next release, and the release documentation states the canonical form. `docs/nuget-packages.md` moves from hygiene to prerequisite: AD-17 makes correcting it a condition of the next release decision. The file still reports 1.3.5 as the latest published `SafeWebCore` and four packages as unpublished, so the finding is unchanged and now load-bearing.
- **V1 keeps its correction in the Stack section** (all five packages are published) **and V2 keeps its RC-window note** (local SDK 11.0.100-rc.1, CI on 10.0.x, support window ending 2026-10-13). V4 stays current, V5 stays deferred.
- **Nothing in this update changes a version pin.** AD-17 fixes the form and the FraudDetection next-release requirement; the deferred `10.0.12` JwtBearer patch and the floating `Mvc.Testing` reference are untouched.
