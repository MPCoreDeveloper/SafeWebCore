# Reviewer gate — rubric walker against ARCHITECTURE-SPINE.md

Reviewed: draft of 2026-09-27, altitude `feature`, brownfield ratify run.
Verdict: **passes with rework** — no critical finding; two high findings both concern claims that the code contradicts, plus three medium tightenings. The spine's paradigm, altitude and Deferred set are right.

## High

**H1 — AD-7 claims one header-emission table; the code has two.**
`src/SafeWebCore/Infrastructure/NetSecureHeadersDiagnosticsService.cs` keeps a parallel table: its own `_defaultCspTemplate = options.Value.Csp.Build()`, its own `ResolvePolicy`, its own `BuildHeaders` with one `AddStandardHeader(...)` call per header, and its own `BuildWarnings`. A builder who follows AD-8 and adds a header only to the middleware leaves the diagnostics preview (and its warnings) silently wrong. Restate AD-7 as "response emission happens in one place" and make the diagnostics projection an explicit step of AD-8's header path.

**H2 — "Library-owned header" and per-request key ownership are undefined, so two ADs are unenforceable as written.**
AD-7 forbids `AdditionalHeaders` from restating a library-owned header and AD-5 requires a constant key, but neither says who owns a name. Two units can each add a differently named key or claim their header is not yet library-owned. Define both: a header is library-owned once a `HeaderNames` constant exists and it is emitted through `AddIfEnabled`; a per-request key belongs to the type that owns the feature, is unique, and is reachable only through its own typed accessor.

## Medium

**M1 — AD-1 and AD-2 are review-enforced only.** Nothing in `tests/` asserts the layer direction or the absence of sideways project references, so a violation lands silently. Either add an architecture test or state plainly that these two rely on review.

**M2 — AD-12 contradicts the code: "performs no I/O" is too strong.** Both detectors call `GeoIpEnricher.Enrich`, which calls `IGeoIpService.GetCountryCode` and `GetTimezone` when a service is registered. The enforceable rule is narrower and true: a detector may consult the injected geo service and nothing else — no notifications, no mail or HTTP, no persistence — and enriching before `Analyze` stays the recommended path that the library documents.

**M3 — AD-10 overstates sink independence.** `FraudEventDispatcher` snapshots `_sinks = sinks.ToArray()`, so delivery order is registration order. Say that the order is deterministic but must not be relied on, instead of implying there is none.

## Low

**L1 — `lint_spine.py` placeholder finding on `{nonce}` is a false positive (verified).** The literal is the runtime placeholder replaced by `cspTemplate.Replace("{nonce}", nonce, StringComparison.Ordinal)` in `NetSecureHeadersMiddleware`. Triage: ignore, keep the token, record the decision in the memlog. No other mechanical finding; the spine carries no template comment and every AD has Binds, Prevents and Rule.

**L2 — No `Capability → Architecture Map`.** Correct for this run: no spec drove the spine.

**L3 — Operational envelope present and complete.** Build, CI, release, runtime and environment behaviour are all stated, so the dimension the gate usually finds silent is covered.

## Addendum — 2026-09-27b: post-decision re-check

- **M3 is closed.** AD-10 no longer implies the sink order is incidental: it states registration order as deterministic and non-reliable, and adds the per-sink isolation policy that the original finding only nudged.
- **H1, H2, M1, M2 and L1 to L3 are unchanged.** The AD-8 lockstep step, the ownership definitions in AD-5 and AD-7 and the narrowed AD-12 wording all survive this edit; adding four rules after them did not reopen any of them.
- **Dimensions re-checked after the update.** Nineteen ADs, each with Binds, Prevents and Rule; the four new ones also carry Evidence. Four of the nineteen — the AD-10 amendment, AD-16, AD-17 and AD-18 — are marked *implementation pending*, so a ratified rule is not read as shipped behaviour, which keeps the spine's claims true against the code as it stands. Deferred still holds twelve named items: the dispatcher row was replaced by the verdict-mapping consolidation, so the count is unchanged. The capability-map and operational-envelope findings are unaffected.
- **Verdict unchanged: passes.** The four new decisions close the questions this run raised without loosening an existing rule; the only rule text that changed in meaning is AD-10, and it changed in the stricter direction.

## Addendum — 2026-09-27c: implementation pass

The four *implementation pending* rules were implemented on 2026-09-27, in the same change as this note:

- **The status column the previous addendum described is gone.** AD-10, AD-16, AD-17 and AD-18 now read *implemented 2026-09-27*, so the "[what] the rule claims" against "[what] the code does" gap that this review's high findings turned on is closed rather than flagged. Every rule is now backed by code and by tests: `SecurityEventDispatcherTests` and `FraudEventDispatcherTests` (per-sink isolation plus the two failure counters), `FraudVerdictMappingTests` (the fail-closed arms and the pinned enum ordinals) and three new validator tests.
- **The Deferred set is now ten items, not twelve.** The verdict-mapping consolidation and the `docs/nuget-packages.md` correction both landed, so their rows were removed from the spine's table; the deck's Deferred slide was updated to match.
- **One new consistency-convention entry.** `InternalsVisibleTo` is now granted to `SafeWebCore.FraudDetection.Tests` as well as `SafeWebCore.JwtBearer.Tests`, for the internal `FraudVerdictMapping` and the internal `FraudEventDispatcher`. The row was widened to "the matching test project" rather than left to contradict the csproj.
- **Verdict unchanged: passes.** The implemented change is additive on the public surface (`RiskLevel.Unclassified`, one dispatcher constructor overload, two counters) and tightening in behaviour, which is what the ratified rules asked for.

