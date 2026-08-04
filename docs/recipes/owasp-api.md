# Recipe: OWASP API Security Alignment

The [OWASP API Security Top 10](https://owasp.org/API-Security/) is the industry-standard list of the most critical API security risks. SafeWebCore's `OwaspApi` preset applies the recommended response-header hardening for API endpoints.

## Recommended Approach (Dedicated Preset)

```csharp
builder.Services.AddNetSecureHeadersOwaspApiPreset(opts =>
{
    // Optional tweaks
    // opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";
});
```

The `AddNetSecureHeadersOwaspApiPreset` preset:
- **HSTS enabled** — 2-year HTTPS enforcement with preload (API1/Broken Object Level Authorization, API2/Broken Authentication both assume TLS)
- **`X-Content-Type-Options: nosniff`** — No MIME sniffing (API8/Security Misconfiguration)
- **`Referrer-Policy: no-referrer`** — prevents token leakage via the referrer header
- **`X-Permitted-Cross-Domain-Policies: none`** — No Flash/Acrobat cross-domain policies (API8)
- **`X-DNS-Prefetch-Control: off`** — No DNS leak
- **Server + X-Powered-By removal** — reduces attack surface / framework fingerprinting (API2)
- **Browser-document headers disabled** — CSP, X-Frame-Options, Permissions-Policy, COEP/COOP/CORP are off because APIs return JSON, not HTML

## Path Policy for API + UI on the same host

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    // Strong browser-facing defaults for the UI
    opts.HstsValue = "max-age=63072000; includeSubDomains; preload";

    // OWASP-aligned hardening for /api paths
    opts.PathPolicies.Add(SecurePresets.OwaspApiPath("/api"));
});
```

Or with inheritance and customization:

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.PathPolicy("/api", api =>
    {
        api.ApplyPreset(SecurePresets.OwaspApi());
        api.ReferrerPolicyValue = "strict-origin-when-cross-origin";
    });
});
```

## Environment-Aware Rollout

OWASP alignment is equally important in dev/staging because header misconfiguration is itself an "API8: Security Misconfiguration" finding:

```csharp
builder.Services.AddNetSecureHeadersOwaspApiPreset(opts =>
{
    // In non-production, SafeWebCore does not force report-only CSP here because
    // this preset disables CSP by default (JSON responses do not render HTML).
});
```

## When to use

- REST/GraphQL APIs with explicit OWASP API Security Top 10 alignment requirements
- Microservices that must comply with OWASP API security review checklists
- Backend services where browser-document headers add noise or break cross-origin consumers
- API + UI on the same host where `/api` should be OWASP-aligned while the UI uses a stricter document preset

## Difference vs the regular `Api` preset

| Aspect | `Api` preset | `OwaspApi` preset |
|--------|-------------|-------------------|
| CSP | Disabled | Disabled |
| HSTS | ✅ 2-year | ✅ 2-year |
| X-Content-Type-Options | ✅ nosniff | ✅ nosniff |
| Referrer-Policy | `no-referrer` | `no-referrer` |
| X-Permitted-Cross-Domain-Policies | ❌ | ✅ `none` |
| X-DNS-Prefetch-Control | ❌ | ✅ `off` |
| Server/X-Powered-By removal | ✅ | ✅ |
| OWASP API Security Top 10 mapping | Not explicit | Explicit |