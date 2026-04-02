# SafeWebCore v1.2 Implementation Plan

## Scope Reference

- Milestone: [`v1.2 - Security Rollout & DX`](https://github.com/MPCoreDeveloper/SafeWebCore/milestone/1)
- Roadmap: [`docs/roadmap-v1.2.md`](roadmap-v1.2.md)

---

## Phase 1 — Core rollout safety

### Work items
1. Implement CSP Report-Only option in `NetSecureHeadersOptions` and middleware.
2. Add path-aware policy selection support.
3. Add startup validation for risky/invalid combinations.

### Deliverables
- New/updated options model for report-only behavior.
- Deterministic policy resolution for request paths.
- Validation output with clear guidance for users.

### Acceptance criteria
- Integration tests verify enforce vs report-only header behavior.
- Integration tests verify per-path policy output.
- Invalid combinations are surfaced before serving traffic.

---

## Phase 2 — Razor and developer ergonomics

### Work items
1. Add nonce Tag Helpers for `<script>` and `<style>`.
2. Add typed builders for non-CSP policy areas.
3. Update docs for Razor nonce usage and builder examples.

### Deliverables
- New Tag Helper components and registration guidance.
- Fluent APIs for additional policy types.
- Updated docs and examples.

### Acceptance criteria
- Razor test scenarios prove nonce injection correctness.
- Builder tests validate generated header values.
- Documentation examples compile and match actual APIs.

---

## Phase 3 — Observability and overrides

### Work items
1. Add structured CSP report parsing/validation.
2. Introduce sink abstraction for report handling.
3. Add endpoint-level metadata to skip/override policies.

### Deliverables
- Parsed report model(s) and validation behavior.
- Extensible sink interface and default implementation.
- Endpoint metadata integration with middleware.

### Acceptance criteria
- Report payload tests cover valid/invalid payloads.
- Endpoint metadata integration tests pass.
- No regressions in default middleware behavior.

---

## Phase 4 — Presets, optional headers, and performance

### Work items
1. Add optional extra header support as opt-in.
2. Add app-profile presets (API/MVC/Blazor/SPA).
3. Add benchmark project and baseline measurements.

### Deliverables
- Extended options for optional headers.
- New presets with documentation.
- Benchmark results captured for release notes.

### Acceptance criteria
- Preset tests verify expected defaults.
- Optional headers are disabled by default and configurable.
- Benchmark runs are reproducible and documented.

---

## Cross-cutting quality gates

- Keep public APIs documented with XML comments.
- Keep all existing tests green and add targeted tests per feature.
- Maintain backward compatibility for existing configuration paths.
- Preserve low-allocation behavior in hot middleware paths.

---

## Suggested Work Breakdown Structure (WBS)

- Epic A: CSP rollout safety (`Report-Only` + validation).
- Epic B: Route-aware policy resolution.
- Epic C: Razor nonce tooling.
- Epic D: Reporting pipeline and sinks.
- Epic E: Endpoint metadata control.
- Epic F: Presets, optional headers, benchmarks, docs.
