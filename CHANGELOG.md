# Changelog

All notable changes to SafeWebCore will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## [1.3.0] — 2026-05-02

### Changed

- **`StrictAPlus` preset — Permissions-Policy browser-compatibility cleanup**

  The `StrictAPlus` (and all presets derived from it: `Api`, `Mvc`, `Blazor`, `SpaReverseProxy`) no longer emits Permissions-Policy tokens that are absent from the current spec. Chromium-based browsers log each unrecognised token as a console warning, which surfaced as noise for consumers of the preset.

  **Removed (8 stale tokens — no longer in the Permissions Policy spec):**

  | Token | Reason |
  |---|---|
  | `ambient-light-sensor` | Removed from spec; not recognised by Chromium |
  | `battery` | Was in old Feature Policy; never added to Permissions Policy |
  | `cross-origin-isolated` | Document Policy concept, not a Permissions Policy feature |
  | `document-domain` | Proposed but removed before standardisation |
  | `execution-while-not-rendered` | Removed from spec |
  | `execution-while-out-of-viewport` | Removed from spec |
  | `navigation-override` | Removed from spec |
  | `sync-xhr` | Deprecated and removed from spec |

  **Added (7 modern tokens — standardised 2022–2024):**

  | Token | Standardised since |
  |---|---|
  | `clipboard-read` | Chrome 76 / Permissions Policy v2 |
  | `clipboard-write` | Chrome 76 / Permissions Policy v2 |
  | `identity-credentials-get` | Chrome 116 (FedCM) |
  | `local-fonts` | Chrome 103 |
  | `otp-credentials` | Chrome 93 (WebOTP) |
  | `publickey-credentials-create` | Chrome 108 (WebAuthn L2) |
  | `window-management` | Chrome 100 (replaces `window-placement`) |

  The preset now emits **28 recognised feature tokens** — all denied — providing full coverage without browser console noise.

### Tests

- Added `StrictAPlusPermissionsPolicyIncludesModernTokens` — asserts all 7 new tokens are present.
- Added `StrictAPlusPermissionsPolicyExcludesStaleTokens` — asserts all 8 removed tokens are absent, preventing regressions.

### Compatibility

- ✅ **100% backwards compatible** with v1.0.0 – v1.2.0
- The `PermissionsPolicyValue` string changes, but it is still a valid Permissions-Policy header value. Any application that overrides `PermissionsPolicyValue` manually (as the stopgap pattern) is unaffected.



## [1.1.0] — 2025-06-28

### Added

- **`HttpContext.GetCspNonce()` extension method** — Discoverable way to retrieve the per-request CSP nonce without magic strings. Available via `using SafeWebCore.Extensions;`.
  ```csharp
  var nonce = HttpContext.GetCspNonce();
  ```
- **`NonceService.TryWriteNonce(Span<char>, out int)`** — Zero-allocation overload that writes the nonce directly into a caller-provided buffer. Ideal for high-throughput scenarios or writing directly into response buffers.
  ```csharp
  Span<char> buffer = stackalloc char[NonceService.NonceLength];
  if (nonceService.TryWriteNonce(buffer, out int written))
  {
      // Use buffer[..written] — no heap allocation
  }
  ```
- **`NonceService.NonceLength` constant** — Public constant (44) for the length of a generated nonce string. Eliminates magic numbers when pre-allocating buffers.

### Changed

- **CSP template is now pre-built once** in the middleware constructor instead of being rebuilt on every request. Only the lightweight `string.Replace("{nonce}", nonce)` runs per-request. This significantly reduces per-request allocations.
- **`CspOptions.Build()` uses `StringBuilder`** — Replaced `List<string>` + interpolated string allocations + `string.Join` with a pre-sized `StringBuilder(512)`. Eliminates ~20 intermediate string allocations per call.
- **`CspReportMiddleware` now passes `CancellationToken`** — `ReadToEndAsync` uses `context.RequestAborted` for proper cancellation when clients disconnect.
- **`CspNonceAttribute` uses C# pattern matching** — Collapsed nested conditionals into a single `is string { Length: > 0 } nonce` pattern expression.
- **Preset application extracted to `ApplyPreset` helper** — Internal `NetSecureHeadersOptions.ApplyPreset()` method consolidates the 20+ line property copy into a single reusable call. Adding new options in the future requires updating only one place.

### Compatibility

- ✅ **100% backwards compatible** with v1.0.0
- All existing public APIs (`AddNetSecureHeadersStrictAPlus`, `UseNetSecureHeaders`, `CspBuilder`, `[CspNonce]`, CSP reporting) remain unchanged
- No breaking changes to method signatures, behavior, or configuration
- All 40 existing tests pass without modification

---

## [1.0.0] — 2025-06-15

### Added

- Strict A+ preset — `AddNetSecureHeadersStrictAPlus()` for one-line A+ configuration on securityheaders.com
- Fluent `CspBuilder` with full CSP Level 3 (W3C Recommendation) directive coverage
- CSP Level 4 support — Trusted Types (`require-trusted-types-for`, `trusted-types`), `fenced-frame-src`
- Per-request cryptographic nonce generation with `stackalloc` + `RandomNumberGenerator` (zero heap allocations)
- `[CspNonce]` action filter attribute for Razor view nonce injection
- Built-in CSP violation reporting middleware (`/csp-report` endpoint)
- Extensible `IHeaderPolicy` interface for custom header policies
- Full security header suite: HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy, COEP, COOP, CORP, X-DNS-Prefetch-Control, X-Permitted-Cross-Domain-Policies
- Server header removal
- Comprehensive documentation and test suite

[Unreleased]: https://github.com/MPCoreDeveloper/SafeWebCore/compare/v1.3.0...HEAD
[1.3.0]: https://github.com/MPCoreDeveloper/SafeWebCore/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/MPCoreDeveloper/SafeWebCore/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/MPCoreDeveloper/SafeWebCore/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/MPCoreDeveloper/SafeWebCore/releases/tag/v1.0.0
