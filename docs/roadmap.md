# SafeWebCore Roadmap (`v1.4` → `v1.6`)

This roadmap defines the next development phases for `SafeWebCore` and `SafeWebCore.FraudDetection`.

It is based on the following hard requirements:

- **100% backward compatibility**
- Target framework remains **.NET 10**
- Tests use **xUnit v3**
- New features must be **additive**
- Existing public APIs, configuration paths, and presets must remain supported
- No existing default behavior may change unless introduced behind a **new opt-in API**

---

## Roadmap goals

The next releases should increase developer value by improving:

- **Developer experience**
- **Configuration simplicity**
- **Diagnostics**
- **Tooling**
- **Observability**
- **Fraud-detection extensibility**

The goal is to evolve SafeWebCore from a strong runtime middleware library into a broader developer security experience platform, without breaking current consumers.

---

## Non-negotiable compatibility rules

The following rules apply to all roadmap items:

1. No removal of supported public APIs
2. No renaming of supported public APIs
3. No behavior-changing default modifications without opt-in
4. Existing presets must keep their documented behavior unless a new preset/helper is introduced
5. Existing configuration styles must remain supported
6. New functionality should prefer:
   - new extension methods
   - new packages
   - new opt-in services
   - new additive options
7. Hot-path runtime performance must not regress materially
8. Public APIs must keep XML documentation coverage
9. All roadmap items require tests and documentation updates

---

# `v1.4 - DX Foundation`

## Release goal

Improve onboarding, configuration, diagnostics, and release safety.

---

## Epic 1 — Compatibility and release safety

### 1.1 Formal backward compatibility policy
Create a documented compatibility policy for `SafeWebCore` and `SafeWebCore.FraudDetection`.

**Deliverables**
- `docs/development/backward-compatibility-policy.md`
- link from `CONTRIBUTING.md`

**Acceptance criteria**
- policy defines supported compatibility guarantees
- policy covers APIs, defaults, presets, and configuration
- contributors can use it during design and review

---

### 1.2 Public API baseline checks
Add public API compatibility validation to detect accidental breaking changes.

**Deliverables**
- public API baseline for:
  - `SafeWebCore`
  - `SafeWebCore.FraudDetection`

**Acceptance criteria**
- baseline mechanism exists
- API delta can be validated locally and/or in CI
- current public surface is recorded

**Status (implemented)**
- `Microsoft.CodeAnalysis.PublicApiAnalyzers` added to both libraries.
- `PublicAPI.Shipped.txt` + `PublicAPI.Unshipped.txt` present (seeded; surface can be curated over time).
- `RS0037` treated as hard error (removals/renames are breaking).
- `RS0016` / `RS0017` treated as warnings during initial adoption (via `NoWarn` / `WarningsNotAsErrors`).
- Both projects build cleanly with the analyzer enabled.
- Guidance added to backward-compatibility policy and contributor docs.

---



### 1.3 Solution hygiene
Improve contributor workflow by including test projects in the solution.

**Deliverables**
- include `tests/SafeWebCore.Tests/SafeWebCore.Tests.csproj` in `SafeWebCore.slnx`

**Acceptance criteria**
- solution opens cleanly in Visual Studio
- test project is visible and runnable
- no package/runtime behavior changes

---

## Epic 2 — Developer-first configuration

### 2.1 Add `AddNetSecureHeadersFromConfiguration(...)`
Introduce a new additive registration helper that binds options directly from configuration.

**Why**
Many consumers prefer `appsettings.json`-driven setup over inline code configuration.

**Deliverables**
- new configuration registration extension method
- docs and examples

**Acceptance criteria**
- existing registration APIs remain unchanged
- uses existing `NetSecureHeadersOptions`
- docs show minimal example
- behavior is fully backward compatible

---

### 2.2 Add optional environment-aware registration helper
Introduce a new opt-in helper for safe rollout across development, staging, and production.

**Example direction**
- Development: optional `Report-Only`
- Staging: reporting + diagnostics
- Production: enforce mode

**Deliverables**
- new helper API
- tests for environment-based behavior
- documentation

**Acceptance criteria**
- no existing defaults change
- feature is opt-in only
- rollout behavior is clearly documented

---

### 2.3 Improve validation messages
Expand option validation so misconfigurations are easier to understand and fix.

**Focus areas**
- CSP
- NEL
- reporting
- conflicting options
- path policies

**Deliverables**
- improved validation messages
- tests for new validation paths

**Acceptance criteria**
- messages explain cause and suggested fix
- valid configurations behave unchanged
- diagnostics quality improves without changing API shape

**Status (implemented)**
- Expanded `NetSecureHeadersOptionsValidator` with NEL JSON structure validation (`report_to` / `max_age` guidance) and `Csp.ReportTo` ↔ `ReportingEndpoints` consistency check.
- All failure messages now include actionable `Fix: ...` suggestions for CSP, NEL, Reporting, AdditionalHeaders, and PathPolicies.
- Added 4 new validation tests (NEL empty/invalid, ReportTo mismatch, valid happy path) plus improved assertions on existing messages.
- Fully additive; no behavior change for previously valid configurations.

---

## Epic 3 — Diagnostics tooling

### 3.1 Internal diagnostics model
Add an internal diagnostics service/model that computes effective SafeWebCore output.

**Capabilities**
- list enabled headers
- show effective CSP mode
- show active preset metadata
- resolve path policies
- preview effective header output

**Deliverables**
- diagnostics model/service
- tests

**Acceptance criteria**
- no runtime impact unless explicitly used
- logic is deterministic and testable

---

### 3.2 Opt-in diagnostics endpoint mapping API
Add a new developer-facing, opt-in diagnostics endpoint.

**Possible API**
- `MapSafeWebCoreDiagnostics()`

**Possible output**
- active headers
- CSP preview
- effective path policy
- hosting/proxy warnings

**Deliverables**
- endpoint mapping API
- docs and tests

**Acceptance criteria**
- endpoint is never enabled by default
- production usage guidance is documented
- backward compatibility preserved

---

### 3.3 Troubleshooting recipes
Add practical troubleshooting documentation for common real-world issues.

**Priority scenarios**
- IIS / AspNetCoreModule
- reverse proxy / CDN behavior
- CSP blocked scripts/styles
- report-only rollout debugging

**Deliverables**
- new recipe docs under `docs/`

**Acceptance criteria**
- docs match actual APIs
- examples are copy/paste-friendly
- recipes linked from existing docs

---

# `v1.5 - Tooling and Adoption`

## Release goal

Reduce common implementation mistakes and make testing and adoption easier.

---

## Epic 4 — `SafeWebCore.Analyzers`

### 4.1 Create analyzer package scaffold
Create a new analyzer package for SafeWebCore-specific diagnostics.

**Deliverables**
- `SafeWebCore.Analyzers` project/package

**Acceptance criteria**
- analyzer package builds separately
- no runtime dependency impact on core package
- package structure supports future rules

---

### 4.2 Analyzer rule: missing `UseNetSecureHeaders()`
Add a rule that detects when services are registered but middleware is not added to the pipeline.

**Deliverables**
- analyzer rule
- tests
- rule documentation

**Acceptance criteria**
- warns when `AddNetSecureHeaders...()` is used without `UseNetSecureHeaders()`
- warning includes actionable fix
- advisory only, not breaking

---

### 4.3 Analyzer rules: risky CSP patterns
Add initial analyzer coverage for suspicious CSP configurations.

**Candidate patterns**
- `unsafe-inline`
- broad wildcards
- inconsistent nonce usage
- risky combinations

**Deliverables**
- initial CSP analyzer rules
- tests and rule docs

**Acceptance criteria**
- warnings are actionable
- rules are advisory
- no runtime changes required

**Status (implemented)**
- `SafeWebCore.Analyzers` project exists and builds as a separate package.
- `RegistrationWithoutMiddlewareAnalyzer` covers all registration helpers (including Swagger / ReverseProxy / BlazorWebSocket presets).
- `RiskyCspPatternAnalyzer` detects unsafe-inline without nonce and broad wildcards.
- Analyzer rules are advisory and produce build-time diagnostics.

### 4.4 Analyzer documentation and samples
Document rule IDs, behavior, and suppressions.

**Deliverables**
- analyzer docs page or README
- sample usage

**Acceptance criteria**
- rule catalog documented
- suppressions documented
- examples included

---

## Epic 5 — `SafeWebCore.Testing`

### 5.1 Create testing package scaffold
Create a separate package with consumer-facing test helpers.

**Deliverables**
- `SafeWebCore.Testing` project/package

**Acceptance criteria**
- package is separate from runtime library
- aligned with `.NET 10` and `xUnit v3`
- ready for future helpers

**Status (implemented)**
- `SafeWebCore.Testing` project exists as a separate package (no runtime dependency on SafeWebCore).
- Provides `HeaderAssertions`, `CspNonceAssertions`, and `TestHostBootstrapExtensions`.
- Aligned with xUnit v3 and .NET 10.
- Used in the main test suite; fully additive for consumers.

### 5.2 Header assertion helpers
Add helpers for response header verification in integration tests.

**Candidate helpers**
- assert header exists
- assert header absent
- assert header equals expected value
- assert header contains expected fragment

**Deliverables**
- header assertion helpers
- example tests

**Acceptance criteria**
- helpers reduce consumer boilerplate
- no runtime package changes required

---

### 5.3 CSP and nonce assertion helpers
Add helpers for common CSP validation scenarios.

**Candidate helpers**
- assert CSP header exists
- assert report-only vs enforce mode
- assert nonce exists
- assert nonce consistency within a request

**Deliverables**
- CSP and nonce test helpers
- example tests

**Acceptance criteria**
- useful for both minimal API and MVC scenarios
- fully backward compatible

---

### 5.4 Test host/bootstrap helpers
Add helpers to simplify SafeWebCore integration test setup.

**Target scenarios**
- minimal API
- MVC
- API preset usage

**Deliverables**
- bootstrap helper API
- examples

**Acceptance criteria**
- package remains optional
- consumer setup becomes simpler
- no changes required in core runtime package

---

## Epic 6 — Docs as product

### 6.1 Recipe docs for real-world scenarios
Add practical docs for common integration cases.

**Priority recipes**
- MVC + CDN
- Swagger
- Blazor nonce setup
- Report-only rollout
- reverse proxy deployment

**Deliverables**
- `docs/recipes/` content or equivalent structure

**Acceptance criteria**
- examples are real and current
- recipes align with shipped APIs
- docs are easy to copy into projects

---

### 6.2 Backward-compatible adoption guidance
Document new APIs as conveniences, not replacements.

**Deliverables**
- docs updates across README and guides

**Acceptance criteria**
- old and new setup paths are both documented where appropriate
- documentation does not imply forced migration

---

# `v1.6 - Observability and Platform Growth`

## Release goal

Add opt-in visibility, richer presets, and extend `SafeWebCore.FraudDetection` into a broader platform.

---

## Epic 7 — Observability

### 7.1 Telemetry abstractions
Add additive abstractions for runtime events and security signals.

**Candidate areas**
- CSP violations
- path policy hits
- diagnostics warnings
- fraud detection events

**Deliverables**
- telemetry abstraction layer
- tests and docs

**Acceptance criteria**
- existing `ICspReportSink` remains supported
- no mandatory telemetry dependency in core package
- abstractions are additive only

**Status (implemented + enhanced)**
- `SecurityEventDispatcher` + `ISecurityEventSink` already provide opt-in telemetry for security signals (CSP, path policies, headers).
- `IFraudEventSink` + `FraudEvent` (additive, opt-in) deliver the complete `FraudReport` after every analysis.
- `FraudReport` now includes the additive `Risk` property (`RiskScore` + `RiskLevel`), so every fraud event automatically carries structured risk scoring without any consumer change.
- `ICspReportSink` remains the dedicated extensibility point for raw CSP report payloads.
- All abstractions are additive; no mandatory dependency is introduced in the core package.
- Consumers can subscribe to rich events containing `Risk` for metrics, SIEM, alerting, or custom policies.

---

### 7.2 Opt-in metrics/logging integration
Build optional integrations for structured logs and metrics.

**Use cases**
- count CSP violations
- inspect most-hit policies
- understand policy exceptions

**Deliverables**
- optional logging/metrics integration
- examples

**Acceptance criteria**
- integration is opt-in
- no existing defaults change
- docs show practical usage

**Status (implemented)**
- Added opt-in metrics using `System.Diagnostics.Metrics` (standard .NET, OTEL-compatible, zero overhead when not observed).
- `SafeWebCoreMetrics` (meter: `SafeWebCore`) with counters:
  - `safewebcore.headers_applied_total`
  - `safewebcore.csp_violations_total`
  - `safewebcore.path_policy_matches_total`
- `SafeWebCoreFraudMetrics` (meter: `SafeWebCore.FraudDetection`) with counters:
  - `safewebcore.fraud_analyses_total`
  - `safewebcore.fraud_events_by_risk_total` (tagged by `risk_level`)
  - `safewebcore.fraud_events_by_verdict_total` (tagged by `verdict`)
- Metrics instances are registered automatically when using `AddNetSecureHeaders*` / `AddSafeWebCoreFraudDetection`.
- Existing `ISecurityEventSink` and `IFraudEventSink` continue to be the primary event extensibility points.
- Fully additive: no behavior change for consumers who do not configure metric exporters.
- Tests: direct `MeterListener` unit tests + integration test exercising real middleware path and asserting counter movement.

### 7.3 Observability documentation
Document how to operationalize SafeWebCore telemetry.

**Deliverables**
- observability guide
- dashboard/query examples

**Acceptance criteria**
- examples are concrete
- docs reflect actual implementation
- supports adoption without requiring a specific monitoring stack

---

## Epic 8 — Scenario presets

### 8.1 Swagger-friendly preset/helper
Add a helper or preset for Swagger-enabled applications.

**Why**
Swagger is a common friction point for strict CSP and header posture.

**Deliverables**
- new preset/helper
- tests and docs

**Acceptance criteria**
- additive only
- existing presets unchanged
- usage guidance includes trade-offs

**Status (implemented + enhanced)**
- `SecurePresets.Swagger()` + `AddNetSecureHeadersSwagger(...)` helper exist and are additive.
- Added dedicated preset tests (unsafe-inline/CDN behavior, base headers retained).
- Added registration test for the helper.
- Updated `docs/recipes/swagger.md` to lead with the dedicated helper + environment-aware alternatives + diagnostics tip.
- Analyzer now covers `AddNetSecureHeadersSwagger`.

### 8.2 Reverse proxy / YARP scenario preset
Add a scenario-specific helper for proxy-based deployments.

**Deliverables**
- new preset/helper
- docs and tests

**Acceptance criteria**
- existing presets remain unchanged
- guidance clearly explains intended usage

**Status (enhanced)**
- `SecurePresets.ReverseProxy()` + `AddNetSecureHeadersReverseProxyPreset(...)` exist and are additive.
- Added dedicated preset test (https/wss connect sources).
- Added registration test for the helper.
- Analyzer now covers `AddNetSecureHeadersReverseProxyPreset`.

### 8.3 Blazor hosted and websocket-oriented helpers
Add scenario support for hosted Blazor and websocket-heavy apps.

**Deliverables**
- new additive helpers/presets
- examples and tests

**Acceptance criteria**
- current API behavior stays intact
- scenarios are documented clearly

**Status (enhanced)**
- `SecurePresets.BlazorWebSocket()` + `AddNetSecureHeadersBlazorWebSocketPreset(...)` exist and are additive.
- Added dedicated preset test (explicit ws: + wss:).
- Added registration test for the helper.
- Analyzer now covers `AddNetSecureHeadersBlazorWebSocketPreset`.

## Epic 9 — `SafeWebCore.FraudDetection` growth

### 9.1 Add additive risk scoring model
Introduce risk scoring as extra metadata on fraud results.

**Deliverables**
- risk score model
- tests and docs

**Acceptance criteria**
- existing detection/report flows still work
- score is additive, not replacement
- default behavior unchanged

**Status (implemented)**
- New public `RiskLevel` enum (Low / Medium / High / Critical) and `RiskScore` record added (additive metadata only).
- `FraudReport.Risk` property added (defaults to `RiskScore.None` so all existing callers and serialized output are unaffected).
- Populated in both `GeoCulturalConsistencyDetector` and legacy `WesternImpersonationDetector` (bypass paths + normal analysis) using `RiskScore.FromScoreAndVerdict(score, verdict)`.
- `PublicAPI.Unshipped.txt` updated.
- New tests verify bypass → Low and high-inconsistency → Critical while `SuspicionScore`, `Verdict`, and `RecommendedAction` continue to behave exactly as before.
- No changes to thresholds, verdicts, actions, or existing report fields.

### 9.2 Add fraud action pipeline abstractions
Allow consumers to configure reactions to fraud outcomes.

**Candidate actions**
- log
- notify
- webhook
- custom consumer pipeline

**Deliverables**
- action abstractions
- examples and tests

**Acceptance criteria**
- current event-driven notifications remain supported
- injected mail client pattern remains supported
- new pipeline is additive only

**Status (implemented + enhanced)**
- `IFraudEventSink` + `FraudEvent` introduced (additive, opt-in).
- Both `GeoCulturalConsistencyDetector` and legacy `WesternImpersonationDetector` dispatch after producing a `FraudReport`.
- Default `LoggingFraudEventSink` registered automatically.
- `AddFraudEventSink<T>()` helper for consumers.
- Pen-test notification pattern (`IPenTestAuthorizationNotificationConsumer`) remains unchanged.
- Added `WebhookFraudEventSink` (best-effort JSON POST) + `AddFraudWebhookSink(webhookUrl, httpClientName)` registration helper.
- Tests: registration + actual POST verification using mock handler (payload round-trips with correct `SuspicionScore` / `Verdict`).
- First concrete slice for Epic 9.2 delivered. Further pipeline helpers (webhook, policy-driven actions) can follow.

---

### 9.3 Tenant-aware fraud policy extensibility
Improve per-tenant configurability and override support.

**Deliverables**
- additive tenant-aware extension points
- docs and tests

**Acceptance criteria**
- existing config paths remain supported
- examples show realistic tenant override scenarios

---

# Priority order

## Highest ROI quick wins
These should be considered first:

1. backward compatibility policy
2. public API baseline
3. add tests to solution
4. `AddNetSecureHeadersFromConfiguration(...)`
5. improved validation messages
6. diagnostics model
7. diagnostics endpoint
8. analyzer package scaffold
9. missing `UseNetSecureHeaders()` analyzer
10. testing package scaffold

---

# Definition of Done

A roadmap item is only complete when:

- public API is documented with XML comments where applicable
- backward compatibility has been reviewed
- tests are added or updated
- performance impact has been considered for hot paths
- docs are updated
- examples are added when relevant
- changelog is updated for shipped work
- default behavior for existing consumers is unchanged unless the feature is opt-in

---

# Milestone summary

## `v1.4 - DX Foundation`
Focus on:
- compatibility safety
- easier configuration
- diagnostics
- contributor workflow

## `v1.5 - Tooling and Adoption`
Focus on:
- analyzers
- test helpers
- recipe docs
- misuse prevention

## `v1.6 - Observability and Platform Growth`
Focus on:
- telemetry
- scenario presets
- fraud detection extensibility
- platform positioning

---

# Final note

This roadmap intentionally favors opt-in additions over behavioral changes so that SafeWebCore can keep growing in developer value without forcing migrations or breaking existing consumers.
