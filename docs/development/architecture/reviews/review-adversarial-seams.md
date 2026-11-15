# Reviewer gate — adversarial seam review against ARCHITECTURE-SPINE.md

Method: construct two units one level down that each obey every AD to the letter and still build incompatibly.

Verdict: **four real holes** (two high, two medium) plus two seams already closed by the spine. Each hole is a missing or too-loose rule, not a wrong one.

## High

**A1 — Two detectors, two risk levels for the same evidence.**
Detector A returns `Risk = RiskScore.FromScoreAndVerdict(score, verdict)`. Detector B returns a `FraudReport` and never sets `Risk`, which defaults to `RiskScore.None` (`Low`). Both comply with AD-13, which fixes only the mapping function's contents. Consumers reading `Risk.Level` now get `Critical` or `Low` for identical evidence. Close it: every detector populates `Risk` through `RiskScore.FromScoreAndVerdict`, and `RiskScore.None` means "no analysis happened", not "low risk".

**A2 — A typed header and a stringly-typed one for the same header name.**
Unit A follows AD-8 end to end. Unit B adds `AdditionalHeaders.Add(new() { Name = "Document-Policy", ... })` and argues its header is not library-owned, since no constant covers it. AD-7 forbids the overlap but cannot test it: "library-owned" has no definition, and the validator only rejects duplicates *inside* `AdditionalHeaders` while emission assigns through the header indexer, so an entry named `Strict-Transport-Security` silently replaces the emitted value. Close it: define library-owned (H2 of the rubric review) and give the validator or an analyzer the overlap check.

## Medium

**A3 — Two units, two per-request keys for one request.**
Unit A stores a matched-policy marker under a new constant. Unit B introduces its own static key for the same data because AD-5 only requires "a constant key". Both comply; the request ends up with two keys and two accessors that can disagree. Close it: one owning type per key, unique key strings, one typed accessor, stated in AD-5.

**A4 — Two JwtBearer post-configure steps, one of which weakens hardening.**
Unit A adds `IPostConfigureOptions<JwtBearerOptions>` that relaxes an audience or metadata requirement "for one environment". Unit B relies on the guard's `FailFast` to catch a bad authority. Post-configure steps run in registration order and the guard only validates the authority, so A's weakening is invisible to B. AD-14 says hardening goes through the pipeline but not that pipeline additions must be additive. Close it: additions must not weaken an applied hardening rule; weakening is an explicit option on the hardening options type.

## Low

**A5 — Two analyzer rules with different ids and severities.** AD-12 and the conventions cover naming but not severity, numbering or per-rule documentation, so one unit can ship an error-severity rule while another ships a warning. Close it with one short AD: warnings only, ascending `SWC0nn`, one descriptor registry, one README section per rule, release-notes file updated, no runtime effect.

**A6 — Two reporting configurations.** Unit A sets `Csp.ReportTo = "default"` with a matching group; unit B posts to a hard-coded `/csp-report` path and names its group differently. The validator catches the `ReportTo`/group mismatch, but the sink path and the group naming are unstated. Fold the reporting contract into AD-7 so the endpoint path and group naming stay a contract rather than a coincidence.

## Seams already closed (no action)

- **Two units choosing CSP enforcement mode**: `CspModeAttribute` per endpoint, then `UseCspReportOnly`, is a single decision path.
- **Two units populating CSP**: `CspBuilder` and direct `opts.Csp with { ... }` both write the same record, and the header is built from that record once, so the result cannot diverge.

## Addendum — 2026-09-27b: the seam decisions taken

The spine was updated after this review: AD-10 was amended and AD-16 to AD-19 added, on the five open questions. Re-checked against the seam inventory above.

- **A2 is closed, and at the layer this review named.** AD-18 makes the validator reject an `AdditionalHeaders` entry whose name matches a `HeaderNames` constant emitted through `AddIfEnabled`, so "library-owned" is a testable predicate rather than an argument, and the sanctioned replacement path is `CustomPolicies` / `IHeaderPolicy`. The rule text also pins the check to the validator, not to the middleware and the diagnostics projection.
- **The dispatcher asymmetry turned out to be a sixth seam, and it is now AD-10's amended rule.** A sink that throws in `SecurityEventDispatcher` ends delivery for every sink registered after it and surfaces only as an unobserved task exception, while `FraudEventDispatcher` catches per sink. One policy now covers both: materialize the sink sequence once, isolate per sink, count the swallowed failure.
- **A seam of the same family: the duplicated verdict mapping.** `DetermineAction` is duplicated verbatim in `GeoCulturalConsistencyDetector` and `WesternImpersonationDetector` with `_ => RecommendedAction.NoAction`, and `RiskScore.FromScoreAndVerdict` falls through to `RiskLevel.Low`. AD-16 makes the mappings fail closed and forbids extending the copies in duplicate; consolidating them into one helper is deferred as mechanical work.
- **A1, A3, A4, A5 and A6 stand as written.** They are unchanged tightening candidates for the next revision, not closed by this update.

## Addendum — 2026-09-27c: the decisions landed

The four rules that were marked *implementation pending* on 2026-09-27b were implemented the same day, so the deltas above are now code rather than intent:

- **A2's guard is live and slightly wider than the review read it.** The validator rejects every header the middleware emits through its typed options — the `AddIfEnabled` set plus `Content-Security-Policy`, `Content-Security-Policy-Report-Only`, `NEL` and `Reporting-Endpoints` — because the CSP/nonce case the rule's *Prevents* line names is not an `AddIfEnabled` emission. The spine's AD-18 Rule was amended to state that set, and three validator tests cover the global scope, a path-policy scope and the still-allowed case.
- **The sixth seam is closed.** `SecurityEventDispatcher` materializes `sinks?.ToArray() ?? []` in its constructor, isolates each sink and counts the failure in `SafeWebCoreMetrics`; `FraudEventDispatcher` counts its own in `SafeWebCoreFraudMetrics`. Both suites now have a test with a throwing first sink.
- **The duplicated verdict mapping is gone, and consolidation is no longer deferred.** `FraudVerdictMapping` is the one place `DetermineAction` and `MaxSeverity` live; `RiskScore.FromScoreAndVerdict` maps an unrecognized verdict to the appended `RiskLevel.Unclassified` and the action mapping to `RecommendedAction.BlockRequest`. The Deferred row was removed.
