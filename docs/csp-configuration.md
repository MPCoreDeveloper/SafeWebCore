# CSP Configuration Guide

Content Security Policy (CSP) is the most powerful security header for preventing XSS attacks. SafeWebCore provides a fluent builder and nonce-based enforcement out of the box.

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

Since `CspOptions` is a C# `record`, you can also use `with` expressions:

```csharp
using SafeWebCore.Options;

// Start from Strict A+ and relax
opts.Csp = new CspOptions() with
{
    DefaultSrc = "'none'",
    ScriptSrc = "'nonce-{nonce}' 'strict-dynamic'",
    ImgSrc = "'self' https://cdn.example.com data:",
    ConnectSrc = "'self' https://api.example.com"
};
```

Or modify the Strict A+ preset:

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
    .ChildSrc("https://www.youtube.com")
    .FrameAncestors("'none'")
    .UpgradeInsecureRequests()
    .Build();
```
