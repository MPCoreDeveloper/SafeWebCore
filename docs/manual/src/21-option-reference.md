# 21. Option Reference

Every option, its default, and where it is explained. A value marked **off by default** is opt-in: turning it on changes
what a browser does, so it is never a silent default.

## Header options — `NetSecureHeadersOptions`

| Option | Default | Header |
| --- | --- | --- |
| `EnableHsts` / `HstsValue` | `true` / `max-age=31536000; includeSubDomains; preload` | `Strict-Transport-Security` |
| `EnableXFrameOptions` / `XFrameOptionsValue` | `true` / `DENY` | `X-Frame-Options` |
| `EnableXContentTypeOptions` / `XContentTypeOptionsValue` | `true` / `nosniff` | `X-Content-Type-Options` |
| `EnableReferrerPolicy` / `ReferrerPolicyValue` | `true` / `strict-origin-when-cross-origin` | `Referrer-Policy` |
| `EnablePermissionsPolicy` / `PermissionsPolicyValue` | `true` / `camera=(), microphone=(), geolocation=()` | `Permissions-Policy` |
| `EnableCoep` / `CoepValue` | `true` / `require-corp` | `Cross-Origin-Embedder-Policy` |
| `EnableCoop` / `CoopValue` | `true` / `same-origin` | `Cross-Origin-Opener-Policy` |
| `EnableCorp` / `CorpValue` | `true` / `same-origin` | `Cross-Origin-Resource-Policy` |
| `EnableXDnsPrefetchControl` / `XDnsPrefetchControlValue` | `true` / `off` | `X-DNS-Prefetch-Control` |
| `EnableXPermittedCrossDomainPolicies` / `…Value` | `true` / `none` | `X-Permitted-Cross-Domain-Policies` |
| `EnableOriginAgentCluster` / `OriginAgentClusterValue` | **off** / `?1` | `Origin-Agent-Cluster` |
| `EnableXRobotsTag` / `XRobotsTagValue` | **off** / `noindex, nofollow` | `X-Robots-Tag` |
| `EnableClearSiteData` / `ClearSiteDataValue` | **off** / `"cache", "cookies", "storage"` | `Clear-Site-Data` |
| `RemoveServerHeader` | `true` | deletes `Server` in `OnStarting` |
| `RemoveXPoweredBy` | **off** | deletes `X-Powered-By` in `OnStarting` |
| `EnableNel` / `NelValue` | **off** / empty | `Network-Error-Logging` |
| `EnableCsp` / `UseCspReportOnly` | `true` / `false` | `Content-Security-Policy[-Report-Only]` |

## Collections

| Option | Type | Explained |
| --- | --- | --- |
| `Csp` | `CspOptions` | Chapter 6 |
| `PathPolicies` | `List<PathPolicyOptions>` | Chapter 8 |
| `AdditionalHeaders` | `List<AdditionalHeaderOptions>` | Chapter 13 |
| `ReportingEndpoints` | `List<ReportingEndpointOptions>` | Chapter 10 |
| `CustomPolicies` | `List<IHeaderPolicy>` | Chapter 13 |
| `ApplyPreset(options)`, `Clone()` | — | Chapter 4 |

## CSP directives — `CspOptions`

| Property | Default | Emitted as |
| --- | --- | --- |
| `DefaultSrc` | `'none'` | `default-src` |
| `ScriptSrc` | `'nonce-{nonce}' 'strict-dynamic' https:` | `script-src` |
| `ScriptSrcElem`, `ScriptSrcAttr` | empty | `script-src-elem`, `script-src-attr` |
| `StyleSrc` | `'nonce-{nonce}'` | `style-src` |
| `StyleSrcElem`, `StyleSrcAttr` | empty | `style-src-elem`, `style-src-attr` |
| `ImgSrc` | `'self' https: data:` | `img-src` |
| `FontSrc` | empty | `font-src` |
| `ConnectSrc` | empty | `connect-src` |
| `MediaSrc` | empty | `media-src` |
| `ObjectSrc` | `'none'` | `object-src` |
| `ChildSrc` | empty | `child-src` |
| `FrameSrc` | empty | `frame-src` |
| `WorkerSrc` | empty | `worker-src` |
| `ManifestSrc` | empty | `manifest-src` |
| `FencedFrameSrc` | empty | `fenced-frame-src` |
| `BaseUri` | `'none'` | `base-uri` |
| `Sandbox` | empty | `sandbox` |
| `FormAction` | `'self'` | `form-action` |
| `FrameAncestors` | `'none'` | `frame-ancestors` |
| `RequireTrustedTypesFor` | empty | `require-trusted-types-for` |
| `TrustedTypes` | empty | `trusted-types` |
| `ReportTo` | empty | `report-to` |
| `ReportUri` | empty (obsolete) | `report-uri` |
| `EnableUpgradeInsecureRequests` | `true` | `upgrade-insecure-requests` |
| `EnableBlockAllMixedContent` | **off** (obsolete) | `block-all-mixed-content` |

An empty string is "do not emit this directive". `Build()` produces the header value; the middleware caches it per policy
and substitutes `{nonce}` per request.

## Path policies — `PathPolicyOptions`

| Property | Default | Notes |
| --- | --- | --- |
| `PathPrefix` | empty | Normalised with a leading slash; an empty value fails validation |
| `Options` | `new()` | A complete `NetSecureHeadersOptions`, not a delta |

## Additional headers and reporting endpoints

| Type | Property | Notes |
| --- | --- | --- |
| `AdditionalHeaderOptions` | `Name`, `Value` | A duplicate name or a library-owned name fails validation |
| `ReportingEndpointOptions` | `Group`, `Url` | The URL must be absolute HTTP(S); the group must be unique |

## Fraud detection — `FraudDetectionOptions`

| Option | Default |
| --- | --- |
| `EnableWesternImpersonation` | `true` (legacy detector) |
| `EnableGeoCulturalConsistency` | **off** |
| `EnablePenTestDetection` | `true` |
| `WesternImpersonation` | `WesternDetectorOptions` defaults |
| `GeoCulturalConsistency` | `GeoCulturalConsistencyOptions` defaults (`MediumSuspicionThreshold` 30, `HighSuspicionThreshold` 65, `HighInconsistencyThreshold` 85, travel mode and device fingerprinting on) |
| `PenTestDetection` | `PenTestDetectionOptions` defaults (`X-PenTest-Authorized`, burst 30 requests per 15 s, scanner score 60, notification cooldown 10 min) |

The configuration section name is `SafeWebCore:FraudDetection` (`FraudDetectionOptions.DefaultSectionName`).

## JWT bearer

| `JwtAuthorityValidationOptions` | Default |
| --- | --- |
| `Scheme` | the default scheme |
| `FailFast` | `false` |
| `EnforceStaticConfigurationChecks` | `true` |
| `RequireSigningKeys` | `true` |
| `PeriodicValidationInterval` | `null` |

| `JwtBearerHardeningOptions` | Default |
| --- | --- |
| `RequireSignedTokens`, `RequireExpirationTime`, `ValidateLifetime`, `RequireTokenType` | `true` |
| `RequireJwtId`, `RequireNotBefore`, `RequireIssuedAt` | `false` |
| `MaximumClockSkew` | 5 minutes |
| `MaximumTokenLifetime` | `null` |
| `ValidateIssuer`, `ValidateAudience` | `false` |
| `EnableRuntimeMetadataLogging` | `true` |
| `AllowedTokenTypes` | `JWT`, `at+jwt` |
| `AllowedAlgorithms`, `ValidIssuers`, `ValidAudiences` | empty |

## Diagnostics and registration

| API | Default |
| --- | --- |
| `MapSafeWebCoreDiagnostics(pattern)` | `/safewebcore/diagnostics` |
| `UseCspReport()` | `POST /csp-report` |
| `AddNetSecureHeadersFromConfiguration(config)` | section `NetSecureHeaders` |
| `NetSecureHeaders.CspNonceKey` | `CspNonce` |
| `NonceService.NonceLength` | `44` characters (32 random bytes, base64) |

> **A default is a promise.** Changing one of the values in this chapter is a breaking change, not a tuning decision; the
> compatibility rules of Chapter 18 apply to this page exactly as they apply to a method signature.

