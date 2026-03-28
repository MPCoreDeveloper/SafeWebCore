# Presets

SafeWebCore includes pre-configured security presets for common use cases. Presets give you a battle-tested baseline — customize only what your app needs.

---

## Strict A+ Preset

The `StrictAPlus` preset configures **every security header** to the strictest possible value, targeting an A+ rating on [securityheaders.com](https://securityheaders.com) and a passing grade on [Google CSP Evaluator](https://csp-evaluator.withgoogle.com/).

### Usage

```csharp
// Option 1: Zero-configuration
builder.Services.AddNetSecureHeadersStrictAPlus();

// Option 2: With customization
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.Csp = opts.Csp with { ImgSrc = "'self' https://cdn.example.com" };
});
```

### What It Configures

#### Transport & Identity

| Header | Value | Why |
|--------|-------|-----|
| `Strict-Transport-Security` | `max-age=63072000; includeSubDomains; preload` | 2-year HTTPS enforcement, preload eligible |
| `Server` | _(removed)_ | Hides server technology |

#### Framing & Embedding

| Header | Value | Why |
|--------|-------|-----|
| `X-Frame-Options` | `DENY` | Blocks all framing |
| `Cross-Origin-Embedder-Policy` | `require-corp` | Requires explicit CORP/CORS |
| `Cross-Origin-Opener-Policy` | `same-origin` | Isolates browsing context |
| `Cross-Origin-Resource-Policy` | `same-origin` | Blocks cross-origin reads |

#### Content & Privacy

| Header | Value | Why |
|--------|-------|-----|
| `X-Content-Type-Options` | `nosniff` | No MIME sniffing |
| `Referrer-Policy` | `no-referrer` | Zero referrer leakage |
| `X-DNS-Prefetch-Control` | `off` | No DNS leak |
| `X-Permitted-Cross-Domain-Policies` | `none` | No Flash/Acrobat policies |

#### Permissions-Policy (29 features denied)

```
accelerometer=(), ambient-light-sensor=(), autoplay=(), battery=(),
camera=(), cross-origin-isolated=(), display-capture=(),
document-domain=(), encrypted-media=(), execution-while-not-rendered=(),
execution-while-out-of-viewport=(), fullscreen=(), geolocation=(),
gyroscope=(), hid=(), idle-detection=(), magnetometer=(), microphone=(),
midi=(), navigation-override=(), payment=(), picture-in-picture=(),
publickey-credentials-get=(), screen-wake-lock=(), serial=(), sync-xhr=(),
usb=(), web-share=(), xr-spatial-tracking=()
```

#### Content Security Policy

```
default-src 'none';
script-src 'nonce-{nonce}' 'strict-dynamic';
style-src 'nonce-{nonce}';
img-src 'self';
font-src 'self';
connect-src 'self';
object-src 'none';
child-src 'none';
worker-src 'self';
manifest-src 'self';
base-uri 'none';
form-action 'self';
frame-ancestors 'none';
require-trusted-types-for 'script';
trusted-types 'none';
upgrade-insecure-requests
```

---

## Customizing the Preset

The `AddNetSecureHeadersStrictAPlus` method accepts an optional `Action<NetSecureHeadersOptions>` that runs **after** the preset is applied. This lets you relax specific settings.

### How CSP origins work

CSP directives use **space-separated** sources in a single string. To allow multiple origins, just add them with spaces:

```csharp
// One origin
opts.Csp = opts.Csp with { ImgSrc = "'self' https://cdn.example.com" };

// Two CDNs + data URIs
opts.Csp = opts.Csp with { ImgSrc = "'self' https://cdn1.example.com https://cdn2.example.com data:" };

// Multiple directives at once — use a single 'with { ... }' block
opts.Csp = opts.Csp with
{
    ImgSrc = "'self' https://img.cdn.com https://avatars.cdn.com",
    ConnectSrc = "'self' https://api.example.com wss://ws.example.com",
    FontSrc = "'self' https://fonts.gstatic.com https://cdn.example.com"
};
```

> 💡 `CspOptions` is a C# `record` — the `with` expression creates a copy with only the specified properties changed. All other directives keep their strict defaults.

---

### Allow external images

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    // Single CDN
    opts.Csp = opts.Csp with { ImgSrc = "'self' https://cdn.example.com data:" };

    // Or multiple CDNs — just add more origins
    opts.Csp = opts.Csp with { ImgSrc = "'self' https://cdn1.example.com https://cdn2.example.com data:" };
});
```

### Allow Google Fonts

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.Csp = opts.Csp with
    {
        FontSrc = "'self' https://fonts.gstatic.com",
        StyleSrc = "'nonce-{nonce}'"  // keep nonce for style, Google Fonts CSS loads via nonce
    };
});
```

### Allow API connections

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.Csp = opts.Csp with
    {
        ConnectSrc = "'self' https://api.example.com wss://ws.example.com"
    };
});
```

### Use SAMEORIGIN framing (for embedded dashboards)

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.XFrameOptionsValue = "SAMEORIGIN";
    opts.Csp = opts.Csp with { FrameAncestors = "'self'" };
});
```

### Relax referrer policy

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";
});
```

### Enable specific browser features

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.PermissionsPolicyValue = "camera=(self), fullscreen=(self), geolocation=()";
});
```

### Disable cross-origin isolation (for third-party embeds)

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.EnableCoep = false;  // Allows loading cross-origin resources without CORP
});
```

---

## Using the Preset Directly

You can also use the `SecurePresets` class directly for more control:

```csharp
using SafeWebCore.Presets;

// Get the preset as an options object
var strictOptions = SecurePresets.StrictAPlus();

// Inspect values
Console.WriteLine(strictOptions.HstsValue);
Console.WriteLine(strictOptions.Csp.Build());
```

This is useful for:
- Unit testing your customizations
- Building custom presets based on the strict A+ baseline
- Inspecting the exact values at startup

---

## When to NOT Use Strict A+

The Strict A+ preset is intentionally aggressive. You may need a different approach when:

| Scenario | Issue | Solution |
|----------|-------|----------|
| Third-party widgets (chat, analytics) | Blocked by `default-src 'none'` | Add specific origins to `connect-src`, `script-src` |
| CDN-hosted assets | Blocked by `'self'`-only policies | Add CDN origins to relevant directives |
| OAuth/SAML redirects | Blocked by `form-action 'self'` | Add identity provider origin to `form-action` |
| Embedded iframes (YouTube, maps) | Blocked by `child-src 'none'` | Add embed origins to `child-src` |
| COEP with third-party images | Third parties lack CORP headers | Set `EnableCoep = false` or use `credentialless` |
| Legacy browsers | May not support CSP Level 3 | Add `https:` fallback to `script-src` |
