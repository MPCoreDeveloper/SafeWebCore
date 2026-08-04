# Presets

SafeWebCore includes pre-configured security presets for common use cases. Presets give you a battle-tested baseline — customize only what your app needs.

---

## Available Presets

| Preset | Best for | Registration helper |
|--------|----------|---------------------|
| `StrictAPlus` | Maximum lockdown / A+ target | `AddNetSecureHeadersStrictAPlus()` |
| `Api` | API-only services | `AddNetSecureHeadersApiPreset()` |
| `OwaspApi` | OWASP API Security Top 10 aligned hardening for API endpoints | `AddNetSecureHeadersOwaspApiPreset()` |
| `NSwag` | NSwag UI (Rico Sutter's NSwag / NSwagStudio) | `AddNetSecureHeadersNSwagPreset()` |
| `Swagger` | Swagger / OpenAPI UI | `AddNetSecureHeadersSwagger()` |
| `Mvc` | MVC + Razor server-rendered apps | `AddNetSecureHeadersMvcPreset()` |
| `Blazor` | Blazor Server/WebAssembly hybrid hosting | `AddNetSecureHeadersBlazorPreset()` |
| `SpaReverseProxy` | SPA frontend behind ASP.NET Core reverse proxy | `AddNetSecureHeadersSpaReverseProxyPreset()` |

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
| `X-Powered-By` | _(removed)_ | Hides framework (ASP.NET etc.) — enabled in Strict A+ |

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

#### Permissions-Policy (scanner-safe features denied)

Only tokens recognized by current security scanners are emitted. Invalid directives that trigger warnings on securityheaders.com are excluded.

```
accelerometer=(), autoplay=(), camera=(), clipboard-read=(), clipboard-write=(),
display-capture=(), encrypted-media=(), fullscreen=(), geolocation=(),
gyroscope=(), hid=(), idle-detection=(), local-fonts=(), magnetometer=(),
microphone=(), midi=(), payment=(), picture-in-picture=(),
publickey-credentials-get=(), screen-wake-lock=(), serial=(), usb=(),
web-share=(), xr-spatial-tracking=()
```

> Note: `identity-credentials-get`, `otp-credentials`, `publickey-credentials-create`, and `window-management` are omitted to avoid "invalid directive" warnings.

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

### Remove X-Powered-By explicitly (enabled by default in Strict A+)

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.RemoveXPoweredBy = true;
});
```

### Enable Network Error Logging (NEL) — opt-in

```csharp
using SafeWebCore.Options;

builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.EnableNel = true;
    opts.NelValue = """{"report_to":"default","max_age":2592000,"include_subdomains":true}""";
    opts.ReportingEndpoints.Add(new ReportingEndpointOptions
    {
        Group = "default",
        Url = "https://your-report-uri.example.com/nel"
    });
});
```

---

## API Preset

The `Api` preset is for **API-only services** that return JSON instead of HTML.

### Usage

```csharp
builder.Services.AddNetSecureHeadersApiPreset();

// With customization
builder.Services.AddNetSecureHeadersApiPreset(opts =>
{
    opts.Csp = opts.Csp with { ConnectSrc = "'self' https://api.example.com" };
});
```

### What It Configures

- **CSP disabled** (`EnableCsp = false`) — APIs return JSON, not HTML
- **HSTS enabled** — 2-year HTTPS enforcement
- **CORS/embedding headers** — `Cross-Origin-Embedder-Policy: require-corp`, etc.
- **Referrer-Policy** — `strict-origin-when-cross-origin`
- **Permissions-Policy** — All browser features denied
- **`X-Robots-Tag` disabled by default** — Enable if you don't want search indexing

### When to use

- REST/GraphQL APIs
- Microservices
- Backend services consumed by SPAs or mobile apps

---

## OWASP API Preset

The `OwaspApi` preset follows the **OWASP API Security Top 10** recommended response-header hardening for API endpoints.

### Usage

```csharp
builder.Services.AddNetSecureHeadersOwaspApiPreset();

// With customization
builder.Services.AddNetSecureHeadersOwaspApiPreset(opts =>
{
    opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";
});
```

### What It Configures

- **HSTS enabled** — 2-year HTTPS enforcement (OWASP API1/API2)
- **`X-Content-Type-Options: nosniff`** — No MIME sniffing (OWASP API8)
- **`Referrer-Policy: no-referrer`** — Prevents token leakage via the referrer header
- **`X-Permitted-Cross-Domain-Policies: none`** — No Flash/Acrobat policies (OWASP API8)
- **`X-DNS-Prefetch-Control: off`** — No DNS leak
- **Server header removed** — Reduces attack surface (OWASP API2)
- **`X-Powered-By` removed** — Hides framework details (OWASP API2)
- **Browser-document headers disabled** — CSP, X-Frame-Options, Permissions-Policy, and cross-origin isolation are off because APIs return JSON, not HTML

### Path policy helper

```csharp
// API + UI on same host — use OWASP API preset for /api paths
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.PathPolicies.Add(SecurePresets.OwaspApiPath("/api"));
});
```

### When to use

- REST/GraphQL APIs with explicit OWASP API Security Top 10 alignment
- Microservices that must comply with OWASP API security review checklists
- Backend services where browser-document headers add noise or break cross-origin consumers

---

## NSwag Preset

The `NSwag` preset is for applications that expose the **NSwag UI** (Rico Sutter's NSwag / NSwagStudio).

### Usage

```csharp
builder.Services.AddNetSecureHeadersNSwagPreset();

// With customization
builder.Services.AddNetSecureHeadersNSwagPreset(opts =>
{
    opts.ReferrerPolicyValue = "no-referrer";
});
```

### What It Configures

- **Nonce-based CSP** — Per-request nonce for scripts and styles
- **`scripts-src`** — `'self' 'nonce-{nonce}' 'strict-dynamic' https://unpkg.com`
- **`style-src`** — `'self' 'nonce-{nonce}' https://unpkg.com`
- **No `'unsafe-inline'`** — Unlike the Swagger preset, NSwag UI works with nonces and `'strict-dynamic'` so `'unsafe-inline'` is **not** required
- **HSTS enabled** — 2-year HTTPS enforcement
- **Strict referrer policy** — `strict-origin-when-cross-origin`

### Why it is stricter than Swagger

NSwag UI loads its assets from the official `https://unpkg.com/nswag/` package and works with nonce-based CSP, so this preset avoids `'unsafe-inline'` — giving a stronger CSP posture than the Swagger preset, which still requires it for older Swagger UI versions.

Run the diagnostics endpoint (`/safewebcore/diagnostics`) to preview the effective CSP while developing.

---

## MVC Preset

The `Mvc` preset is for **server-rendered MVC + Razor Views** applications.

### Usage

```csharp
builder.Services.AddNetSecureHeadersMvcPreset();

// With customization
builder.Services.AddNetSecureHeadersMvcPreset(opts =>
{
    opts.Csp = opts.Csp with { ImgSrc = "'self' https://cdn.example.com" };
});
```

### What It Configures

- **Nonce-based CSP** — Per-request nonce for `<script>` and `<style>`
- **Practical asset allowances** — `img-src 'self' https: data:`, `font-src 'self' https:`
- **HSTS enabled** — 2-year HTTPS enforcement with preload
- **Referrer-Policy** — `strict-origin-when-cross-origin` (balanced for navigation)
- **Server header removed**
- **TagHelpers enabled** — Use `@addTagHelper *, SafeWebCore` in `_ViewImports.cshtml`

### CSP Details

```
default-src 'none';
script-src 'nonce-{nonce}' 'strict-dynamic' https%;
style-src 'nonce-{nonce}';
img-src 'self' https: data%;
font-src 'self' https%;
connect-src 'self';
```

### When to use

- ASP.NET Core MVC apps
- Razor Pages apps
- Server-rendered apps with static assets

### Example: CDN + API

```csharp
builder.Services.AddNetSecureHeadersMvcPreset(opts =>
{
    opts.Csp = opts.Csp with
    {
        ImgSrc = "'self' https://cdn.example.com",
        FontSrc = "'self' https://fonts.gstatic.com",
        ConnectSrc = "'self' https://api.example.com"
    };

    opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";
});
```

---

## Blazor Preset

The `Blazor` preset is for **Blazor Server + WebAssembly hybrid hosting**.

### Usage

```csharp
builder.Services.AddNetSecureHeadersBlazorPreset();

// With customization
builder.Services.AddNetSecureHeadersBlazorPreset(opts =>
{
    opts.Csp = opts.Csp with { WorkerSrc = "'self' blob:" };
});
```

### What It Configures

- **Nonce-based CSP** — Per-request nonce for inline scripts
- **WebAssembly + Worker support** — `worker-src 'self' blob:` for Blazor WASM
- **WebSocket connections** — `connect-src 'self' wss:` for Blazor Server signaling
- **HSTS enabled** — 2-year HTTPS enforcement
- **Permissions-Policy** — Stricter than MVC, no geolocation/camera

### CSP Details

```
default-src 'none';
script-src 'nonce-{nonce}' 'strict-dynamic' https%;
style-src 'nonce-{nonce}';
img-src 'self' https: data%;
font-src 'self' https%;
connect-src 'self' wss%;
worker-src 'self' blob%;
```

### When to use

- Blazor Server apps
- Blazor WebAssembly hosted by ASP.NET Core
- Apps combining server and client-side rendering

---

## SPA Reverse-Proxy Preset

The `SpaReverseProxy` preset is for **Single-Page Apps (Vue, React, Angular) served behind ASP.NET Core**.

### Usage

```csharp
builder.Services.AddNetSecureHeadersSpaReverseProxyPreset();

// With customization
builder.Services.AddNetSecureHeadersSpaReverseProxyPreset(opts =>
{
    opts.Csp = opts.Csp with { ConnectSrc = "'self' https://api.example.com wss://ws.example.com" };
});
```

### What It Configures

- **Nonce-based CSP** — Per-request nonce for frameworks that inline scripts
- **Broad asset allowances** — `img-src 'self' https: data: blob:`, `font-src 'self' https:`
- **WebSocket + API support** — `connect-src 'self' https: wss:`
- **Blob support** — For dynamic imports and canvas operations
- **HSTS enabled** — 2-year HTTPS enforcement

### CSP Details

```
default-src 'none';
script-src 'nonce-{nonce}' 'strict-dynamic' https: wss%;
style-src 'nonce-{nonce}' https%;
img-src 'self' https: data: blob%;
font-src 'self' https%;
connect-src 'self' https: wss%;
worker-src 'self' blob%;
```

### When to use

- Vue.js, React, Angular apps
- Frontend served by ASP.NET Core reverse proxy
- Apps with dynamic imports or web workers

### Example: API backend

```csharp
builder.Services.AddNetSecureHeadersSpaReverseProxyPreset(opts =>
{
    opts.Csp = opts.Csp with { ConnectSrc = "'self' https://api.example.com wss://ws.example.com" };
});
```

---

## Comparing Presets

| Feature | StrictAPlus | Api | Mvc | Blazor | SpaReverseProxy |
|---------|:----------:|:---:|:---:|:------:|:---------------:|
| **CSP enabled** | ✅ Nonce | ❌ | ✅ Nonce | ✅ Nonce | ✅ Nonce |
| **HSTS** | ✅ 2-year | ✅ 1-year | ✅ 2-year | ✅ 2-year | ✅ 2-year |
| **WebSocket** | ❌ | ❌ | ❌ | ✅ `wss:` | ✅ `wss:` |
| **Worker/Blob** | ❌ | ❌ | ❌ | ✅ | ✅ |
| **External HTTPS** | ❌ | ✅ | ✅ | ✅ | ✅ |
| **Images/Data** | `'self'` | N/A | `https: data:` | `https: data:` | `https: data: blob:` |
| **Permissions-Policy** | ~24 scanner-safe features denied | All denied | Balanced | Restricted | Balanced |

---

## Using the Preset Directly

You can inspect preset values without registering them:

```csharp
using SafeWebCore.Presets;

// Get the preset as an options object
var strictOptions = SecurePresets.StrictAPlus();
var mvcOptions = SecurePresets.Mvc();

// Inspect values
Console.WriteLine(strictOptions.HstsValue);
Console.WriteLine(mvcOptions.Csp.Build());
```

This is useful for:
- Unit testing your customizations
- Building custom presets based on a preset baseline
- Comparing preset values at startup

---

## Choosing Your Preset

| Your Application | Recommended Preset | Why |
|------------------|-------------------|-----|
| REST/GraphQL API | `Api` | No HTML = no CSP needed; focus on transport & CORS |
| REST/GraphQL API (OWASP-aligned) | `OwaspApi` | Explicit OWASP API Security Top 10 response-header hardening |
| NSwag UI/NSwagStudio | `NSwag` | Nonce-based CSP, unpkg assets, no `'unsafe-inline'` |
| Swagger / OpenAPI UI | `Swagger` | CDN assets + `unsafe-inline` styles for older Swagger UI |
| MVC + Razor Pages | `Mvc` | Server-rendered HTML with practical asset allowances |
| Blazor Server | `Blazor` | WebSocket + Blazor runtime support |
| Blazor WASM hosted | `Blazor` | Worker/blob support for WASM + nonce for Server interop |
| React/Vue/Angular SPA | `SpaReverseProxy` | Dynamic imports, web workers, broad asset support |
| Maximum security locked down | `StrictAPlus` | Strictest possible; relax selectively for your needs |
