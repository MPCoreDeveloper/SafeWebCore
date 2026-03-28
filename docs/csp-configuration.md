# CSP Configuration Guide

Content Security Policy (CSP) is the most powerful security header for preventing XSS attacks. SafeWebCore provides a fluent builder and nonce-based enforcement out of the box.

SafeWebCore implements the **full CSP Level 3** (W3C Recommendation) directive set and forward-looking **CSP Level 4** features including Trusted Types and `fenced-frame-src`.

---

## How CSP Works

CSP tells the browser which sources of content are allowed. Any resource not explicitly allowed is **blocked**. SafeWebCore uses **nonce-based CSP** — the most secure approach recommended by Google and the W3C.

### The Nonce Flow

```
1. Server generates a unique random nonce per request
2. Nonce is injected into the CSP header:
   script-src 'nonce-abc123' 'strict-dynamic'
3. Your HTML includes the nonce on allowed scripts:
   <script nonce="abc123">...</script>
4. Browser executes only scripts with the matching nonce
```

---

## CSP Directives Reference

### Fetch Directives

These control where resources can be loaded from.

| Directive | Purpose | Strict A+ Value |
|-----------|---------|-----------------|
| `default-src` | Fallback for all fetch directives | `'none'` |
| `script-src` | JavaScript execution | `'nonce-{nonce}' 'strict-dynamic'` |
| `script-src-elem` | `<script>` elements (CSP L3) | _(inherits script-src)_ |
| `script-src-attr` | Inline event handlers (CSP L3) | _(inherits script-src)_ |
| `style-src` | Stylesheets | `'nonce-{nonce}'` |
| `style-src-elem` | `<style>` elements (CSP L3) | _(inherits style-src)_ |
| `style-src-attr` | Inline `style` attributes (CSP L3) | _(inherits style-src)_ |
| `img-src` | Images | `'self'` |
| `font-src` | Fonts | `'self'` |
| `connect-src` | XHR, fetch, WebSocket, EventSource | `'self'` |
| `media-src` | `<audio>`, `<video>` | _(inherits 'none')_ |
| `object-src` | `<object>`, `<embed>`, `<applet>` | `'none'` |
| `child-src` | `<frame>`, `<iframe>`, workers | `'none'` |
| `frame-src` | `<frame>`, `<iframe>` (CSP L3, split from child-src) | _(inherits child-src)_ |
| `worker-src` | Worker, SharedWorker, ServiceWorker | `'self'` |
| `manifest-src` | Web app manifest | `'self'` |
| `fenced-frame-src` | `<fencedframe>` (2025+) | _(disabled)_ |

### Document Directives

| Directive | Purpose | Strict A+ Value |
|-----------|---------|-----------------|
| `base-uri` | Restricts `<base>` URIs | `'none'` |
| `sandbox` | Sandbox restrictions | _(disabled)_ |

### Navigation Directives

| Directive | Purpose | Strict A+ Value |
|-----------|---------|-----------------|
| `form-action` | Form submission targets | `'self'` |
| `frame-ancestors` | Who can embed this page | `'none'` |

### Trusted Types (CSP Level 3)

| Directive | Purpose | Strict A+ Value |
|-----------|---------|-----------------|
| `require-trusted-types-for` | Enforce Trusted Types on DOM sinks | `'script'` |
| `trusted-types` | Allowed Trusted Type policy names | `'none'` |

### Transport

| Directive | Purpose | Strict A+ Value |
|-----------|---------|-----------------|
| `upgrade-insecure-requests` | Auto-upgrade HTTP → HTTPS | ✅ Enabled |
| `block-all-mixed-content` | Block mixed content _(deprecated)_ | ❌ Disabled |

---

## Using the Fluent CspBuilder

```csharp
using SafeWebCore.Builder;

opts.Csp = new CspBuilder()
    .DefaultSrc("'none'")
    .ScriptSrc("'nonce-{nonce}' 'strict-dynamic'")
    .StyleSrc("'nonce-{nonce}'")
    .ImgSrc("'self' https://images.example.com")
    .FontSrc("'self' https://fonts.gstatic.com")
    .ConnectSrc("'self' https://api.example.com wss://ws.example.com")
    .WorkerSrc("'self'")
    .ObjectSrc("'none'")
    .BaseUri("'none'")
    .FormAction("'self'")
    .FrameAncestors("'none'")
    .RequireTrustedTypesFor("'script'")
    .UpgradeInsecureRequests()
    .Build();
```

Every method returns `this` for chaining. Call `.Build()` at the end to get the immutable `CspOptions` record.

---

## Using CspOptions Directly

Since `CspOptions` is a C# `record`, you can use `with` expressions. Each directive is a **space-separated** string of sources:

```csharp
using SafeWebCore.Options;

// Start from scratch
opts.Csp = new CspOptions() with
{
    DefaultSrc = "'none'",
    ScriptSrc = "'nonce-{nonce}' 'strict-dynamic'",
    ImgSrc = "'self' https://cdn.example.com data:",
    ConnectSrc = "'self' https://api.example.com"
};
```

Or modify the Strict A+ preset — change only what you need, the rest stays strict:

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.Csp = opts.Csp with
    {
        ImgSrc = "'self' https://cdn.example.com",
        FontSrc = "'self' https://fonts.gstatic.com"
    };
});
```

### Multiple origins per directive

Add as many origins as you need, separated by spaces:

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.Csp = opts.Csp with
    {
        // Images from two CDNs + data URIs
        ImgSrc = "'self' https://cdn1.example.com https://cdn2.example.com data:",

        // API + WebSocket + analytics
        ConnectSrc = "'self' https://api.example.com wss://ws.example.com https://analytics.example.com",

        // Google Fonts + your own CDN
        FontSrc = "'self' https://fonts.gstatic.com https://cdn.example.com",

        // Multiple script hosts (loaded via strict-dynamic trust chain)
        ScriptSrc = "'nonce-{nonce}' 'strict-dynamic'"
    };
});
```

> 💡 **Key rule:** one string per directive, spaces between origins. The `with { ... }` block lets you change multiple directives in one expression.

---

## Nonce Usage in HTML

### Razor Views with `[CspNonce]` attribute

```csharp
[CspNonce]
public class HomeController : Controller
{
    public IActionResult Index() => View();
}
```

```html
@{
    var nonce = ViewData["CspNonce"]?.ToString();
}

<!-- Scripts -->
<script nonce="@nonce">
    document.addEventListener('DOMContentLoaded', () => {
        console.log('CSP-compliant script');
    });
</script>

<!-- Styles -->
<style nonce="@nonce">
    .hero { background-color: #007bff; }
</style>

<!-- External scripts also need the nonce -->
<script nonce="@nonce" src="/js/app.js"></script>
```

### Direct access from HttpContext

```csharp
// In middleware, minimal API handlers, etc.
app.MapGet("/api/nonce", (HttpContext ctx) =>
{
    var nonce = ctx.Items[NetSecureHeaders.CspNonceKey] as string;
    return Results.Ok(new { nonce });
});
```

---

## The `{nonce}` Placeholder

SafeWebCore uses `{nonce}` as a placeholder in CSP directive values. At runtime, the middleware replaces it with the actual per-request nonce:

```
Config:   script-src 'nonce-{nonce}' 'strict-dynamic'
Runtime:  script-src 'nonce-k7sJ2mP9xQ...' 'strict-dynamic'
```

This replacement happens once per request in `NetSecureHeadersMiddleware`.

---

## CSP Source Values Reference

| Value | Meaning |
|-------|---------|
| `'none'` | Block everything |
| `'self'` | Same origin only |
| `'unsafe-inline'` | Allow inline scripts/styles (**avoid!**) |
| `'unsafe-eval'` | Allow `eval()` (**avoid!**) |
| `'nonce-{nonce}'` | Allow resources with matching nonce |
| `'strict-dynamic'` | Trust scripts loaded by already-trusted scripts |
| `https:` | Allow any HTTPS source |
| `data:` | Allow `data:` URIs |
| `blob:` | Allow `blob:` URIs |
| `https://example.com` | Allow specific origin |

---

## Common Scenarios

### SPA with API backend

```csharp
opts.Csp = new CspBuilder()
    .DefaultSrc("'none'")
    .ScriptSrc("'nonce-{nonce}' 'strict-dynamic'")
    .StyleSrc("'nonce-{nonce}'")
    .ImgSrc("'self'")
    .FontSrc("'self'")
    .ConnectSrc("'self' https://api.myapp.com")
    .BaseUri("'none'")
    .FormAction("'self'")
    .FrameAncestors("'none'")
    .UpgradeInsecureRequests()
    .Build();
```

### Content site with CDN and Google Fonts

```csharp
opts.Csp = new CspBuilder()
    .DefaultSrc("'none'")
    .ScriptSrc("'nonce-{nonce}' 'strict-dynamic'")
    .StyleSrc("'nonce-{nonce}'")
    .ImgSrc("'self' https://cdn.example.com data:")
    .FontSrc("'self' https://fonts.gstatic.com")
    .ConnectSrc("'self'")
    .BaseUri("'none'")
    .FormAction("'self'")
    .FrameAncestors("'none'")
    .UpgradeInsecureRequests()
    .Build();
```

### Embedding YouTube videos

```csharp
opts.Csp = new CspBuilder()
    .DefaultSrc("'none'")
    .ScriptSrc("'nonce-{nonce}' 'strict-dynamic'")
    .StyleSrc("'nonce-{nonce}'")
    .ImgSrc("'self' https://img.youtube.com")
    .FrameSrc("https://www.youtube.com")
    .FrameAncestors("'none'")
    .UpgradeInsecureRequests()
    .Build();
```

---

## CSP Level 3 / Level 4 Compliance

SafeWebCore implements the **complete CSP Level 3** W3C Recommendation and includes forward-looking **CSP Level 4** directives.

### CSP Level 3 (W3C Recommendation) — ✅ Full Coverage

| Category | Directives | Status |
|----------|------------|--------|
| **Fetch** | `default-src`, `script-src`, `style-src`, `img-src`, `font-src`, `connect-src`, `media-src`, `object-src`, `child-src`, `frame-src`, `worker-src`, `manifest-src` | ✅ All 12 |
| **Granular fetch (L3)** | `script-src-elem`, `script-src-attr`, `style-src-elem`, `style-src-attr` | ✅ All 4 |
| **Document** | `base-uri`, `sandbox` | ✅ Both |
| **Navigation** | `form-action`, `frame-ancestors` | ✅ Both |
| **Reporting** | `report-to` (Reporting API v1) | ✅ |
| **Transport** | `upgrade-insecure-requests` | ✅ |
| **Nonce-based** | `'nonce-{nonce}'` per-request cryptographic nonces | ✅ |
| **Hash-based** | `'sha256-...'`, `'sha384-...'`, `'sha512-...'` allowlisting | ✅ |
| **Trust propagation** | `'strict-dynamic'` — trusted scripts can load further dependencies | ✅ |
| **Deprecated (L3)** | `report-uri` → `[Obsolete]`, `block-all-mixed-content` → `[Obsolete]` | ✅ Handled |

#### Key CSP Level 3 improvements implemented

- **`frame-src` split from `child-src`** — In CSP Level 2, `child-src` governed both frames and workers. Level 3 separates them: `frame-src` for `<frame>`/`<iframe>`, `worker-src` for Worker/SharedWorker/ServiceWorker.
- **`worker-src`** — Dedicated directive for controlling Worker, SharedWorker, and ServiceWorker sources.
- **`manifest-src`** — Controls web app manifest loading.
- **Granular script/style directives** — `script-src-elem`, `script-src-attr`, `style-src-elem`, `style-src-attr` provide fine-grained control beyond the base `script-src`/`style-src`.
- **`report-to`** — Replaces the deprecated `report-uri` directive with the modern Reporting API v1.
- **Nonce + hash + `strict-dynamic`** — The recommended approach per Google and the W3C. SafeWebCore generates a unique cryptographic nonce per request using `stackalloc` + `RandomNumberGenerator` (zero heap allocations).

### CSP Level 4 (Emerging) — ✅ Ready

| Directive | Purpose | Status |
|-----------|---------|--------|
| `require-trusted-types-for` | Enforces Trusted Types for DOM XSS sinks (`innerHTML`, `eval()`, etc.) | ✅ |
| `trusted-types` | Controls which Trusted Type policy names are allowed | ✅ |
| `fenced-frame-src` | Controls `<fencedframe>` sources (Privacy Sandbox) | ✅ |

#### Trusted Types

Trusted Types prevent DOM-based XSS at the API level by requiring all dangerous DOM sinks to use type-safe objects instead of raw strings:

```csharp
opts.Csp = new CspBuilder()
    .DefaultSrc("'none'")
    .ScriptSrc("'nonce-{nonce}' 'strict-dynamic'")
    .RequireTrustedTypesFor("'script'")
    .TrustedTypes("'none'")
    .Build();
```

This blocks calls like `element.innerHTML = userInput` unless the value is wrapped in a Trusted Type policy.

#### Hash-Based Allowlisting

CSP Level 3 supports allowing specific inline scripts/styles by their SHA digest. Pass hash tokens directly in any directive value:

```csharp
opts.Csp = new CspBuilder()
    .ScriptSrc("'sha256-abc123...' 'strict-dynamic'")
    .StyleSrc("'sha256-def456...'")
    .Build();
```

Supported algorithms: `sha256`, `sha384`, `sha512`.

### Deprecated Directives

SafeWebCore correctly handles deprecated directives:

| Directive | Status | Replacement |
|-----------|--------|-------------|
| `report-uri` | `[Obsolete]` — still emitted when set for backward compatibility | `report-to` (Reporting API v1) |
| `block-all-mixed-content` | `[Obsolete]` — modern browsers block mixed content by default | `upgrade-insecure-requests` |

Both deprecated directives are excluded from `CspBuilder` but remain available on `CspOptions` with `[Obsolete]` attributes and compiler warnings.
