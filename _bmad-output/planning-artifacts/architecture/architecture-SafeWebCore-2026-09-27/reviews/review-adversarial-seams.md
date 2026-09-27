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
