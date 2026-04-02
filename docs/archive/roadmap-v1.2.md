# SafeWebCore v1.2 Roadmap

This roadmap tracks the scope of the GitHub milestone:

- Milestone: [`v1.2 - Security Rollout & DX`](https://github.com/MPCoreDeveloper/SafeWebCore/milestone/1)

---

## Goal

Deliver the next high-impact security and developer-experience release for `SafeWebCore` while keeping backward compatibility and low runtime overhead.

---

## Planned Feature Scope

### 1) CSP Report-Only mode
- Add support for `Content-Security-Policy-Report-Only`.
- Allow safe rollout and policy tuning before strict enforcement.

### 2) Route/path-based policy selection
- Allow different header/CSP profiles per endpoint path.
- Support mixed scenarios (e.g., `/api`, `/admin`, `/health`, `/swagger`).

### 3) Startup configuration validation
- Validate unsafe or conflicting policy combinations at startup.
- Emit actionable warnings/errors for misconfiguration.

### 4) Razor nonce auto-injection
- Add Tag Helpers to inject CSP nonce into `<script>` and `<style>`.
- Reduce manual nonce plumbing in Razor views.

### 5) Typed policy builders (beyond CSP)
- Add fluent builders for `Permissions-Policy`, `Referrer-Policy`, and COOP/COEP/CORP settings.
- Reduce typo risk in string-based policy setup.

### 6) Advanced CSP reporting pipeline
- Parse and validate incoming CSP report payloads.
- Add sink abstraction for custom handling (logging/telemetry/SIEM).

### 7) Endpoint metadata overrides
- Add endpoint-level metadata/attributes for skip or relaxed policies.
- Keep global defaults while enabling targeted exceptions.

### 8) Optional additional headers
- Add opt-in support for headers such as `Origin-Agent-Cluster`, `X-Robots-Tag`, and `Clear-Site-Data` helpers.

### 9) Additional presets
- Add presets for common app profiles: API-only, MVC, Blazor, and SPA reverse proxy setups.

### 10) Benchmark and release hardening
- Add benchmark suite for middleware/header overhead.
- Expand regression coverage and complete release documentation.

---

## Release Strategy

- `v1.2` focus: Report-Only, route policies, startup validation, nonce Tag Helpers.
- `v1.3` follow-up: reporting pipeline depth, metadata overrides, typed builders expansion, extra presets, benchmark polish.

This split can be adjusted based on implementation risk and test outcomes.
