# Security Headers Reference

SafeWebCore adds the following security headers to every HTTP response. Each header addresses a specific attack vector.

---

## Strict-Transport-Security (HSTS)

**Attack prevented:** SSL stripping, protocol downgrade attacks

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `EnableHsts` | `true` | `true` |
| `HstsValue` | `max-age=31536000; includeSubDomains; preload` | `max-age=63072000; includeSubDomains; preload` |

**What it does:** Forces browsers to only use HTTPS for your domain. The `preload` directive allows submission to the [HSTS preload list](https://hstspreload.org/).

- `max-age` — duration (seconds) the browser remembers to use HTTPS only
- `includeSubDomains` — applies to all subdomains
- `preload` — eligible for browser preload list (requires 2-year max-age)

```csharp
opts.EnableHsts = true;
opts.HstsValue = "max-age=63072000; includeSubDomains; preload";
```

> ⚠️ **Warning:** Only enable `preload` if ALL subdomains support HTTPS. Removal from the preload list can take months.

---

## X-Frame-Options

**Attack prevented:** Clickjacking

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `EnableXFrameOptions` | `true` | `true` |
| `XFrameOptionsValue` | `DENY` | `DENY` |

**What it does:** Controls whether your page can be embedded in `<frame>`, `<iframe>`, or `<object>` elements.

| Value | Meaning |
|-------|---------|
| `DENY` | Page cannot be framed at all |
| `SAMEORIGIN` | Only same-origin pages can frame it |

```csharp
opts.XFrameOptionsValue = "DENY";
```

> 💡 CSP's `frame-ancestors` directive is the modern replacement. SafeWebCore sets both for maximum compatibility.

---

## X-Content-Type-Options

**Attack prevented:** MIME-type sniffing attacks

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `EnableXContentTypeOptions` | `true` | `true` |
| `XContentTypeOptionsValue` | `nosniff` | `nosniff` |

**What it does:** Prevents browsers from guessing the MIME type of a response. The browser will only use the declared `Content-Type`.

```csharp
opts.XContentTypeOptionsValue = "nosniff";
```

---

## Referrer-Policy

**Attack prevented:** Information leakage via the `Referer` header

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `EnableReferrerPolicy` | `true` | `true` |
| `ReferrerPolicyValue` | `strict-origin-when-cross-origin` | `no-referrer` |

**What it does:** Controls how much referrer information is sent with requests.

| Value | Behavior |
|-------|----------|
| `no-referrer` | Never send referrer (strictest) |
| `no-referrer-when-downgrade` | Don't send on HTTPS→HTTP |
| `strict-origin-when-cross-origin` | Full URL same-origin, origin-only cross-origin |
| `same-origin` | Only send referrer for same-origin requests |
| `origin` | Only send the origin (no path) |

```csharp
opts.ReferrerPolicyValue = "no-referrer";
```

---

## Permissions-Policy

**Attack prevented:** Unauthorized access to browser features (camera, mic, GPS, etc.)

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `EnablePermissionsPolicy` | `true` | `true` |
| `PermissionsPolicyValue` | `camera=(), microphone=(), geolocation=()` | All 29 features denied |

**What it does:** Restricts which browser features your page (and embedded iframes) can use.

**Strict A+ disables all features:**

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

To allow a specific feature:

```csharp
opts.PermissionsPolicyValue = "camera=(self), microphone=(), geolocation=()";
```

---

## Cross-Origin-Embedder-Policy (COEP)

**Attack prevented:** Spectre-style side-channel attacks

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `EnableCoep` | `true` | `true` |
| `CoepValue` | `require-corp` | `require-corp` |

**What it does:** Requires all cross-origin resources to explicitly opt-in via CORS or CORP headers. Enables `crossOriginIsolated` state for `SharedArrayBuffer` protection.

| Value | Meaning |
|-------|---------|
| `require-corp` | All cross-origin resources must have CORP/CORS headers |
| `credentialless` | Cross-origin requests don't include credentials |
| `unsafe-none` | No restrictions (not recommended) |

---

## Cross-Origin-Opener-Policy (COOP)

**Attack prevented:** Cross-window attacks (Spectre, XS-Leaks)

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `EnableCoop` | `true` | `true` |
| `CoopValue` | `same-origin` | `same-origin` |

**What it does:** Isolates your browsing context from cross-origin windows. Other origins cannot obtain a reference to your window.

---

## Cross-Origin-Resource-Policy (CORP)

**Attack prevented:** Cross-origin resource theft

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `EnableCorp` | `true` | `true` |
| `CorpValue` | `same-origin` | `same-origin` |

**What it does:** Blocks cross-origin reads of your resources. Only same-origin requests can load your resources.

---

## X-DNS-Prefetch-Control

**Attack prevented:** DNS-based information leakage

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `EnableXDnsPrefetchControl` | `true` | `true` |
| `XDnsPrefetchControlValue` | `off` | `off` |

**What it does:** Disables DNS prefetching, which can leak which links exist on your page to DNS resolvers.

---

## X-Permitted-Cross-Domain-Policies

**Attack prevented:** Adobe Flash/Acrobat cross-domain data theft

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `EnableXPermittedCrossDomainPolicies` | `true` | `true` |
| `XPermittedCrossDomainPoliciesValue` | `none` | `none` |

**What it does:** Prevents Adobe Flash and Acrobat from loading cross-domain policy files.

---

## Server Header Removal

**Attack prevented:** Server technology fingerprinting

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `RemoveServerHeader` | `true` | `true` |

**What it does:** Removes the `Server` response header (e.g., `Server: Kestrel`) to prevent attackers from identifying your web server technology.

---

## Content-Security-Policy (CSP)

**Attack prevented:** Cross-site scripting (XSS), data injection, clickjacking

CSP is the most powerful security header. See the dedicated [CSP Configuration Guide](csp-configuration.md) for full details.

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `EnableCsp` | `true` | `true` |
| `default-src` | `'none'` | `'none'` |
| `script-src` | `'nonce-{nonce}' 'strict-dynamic' https:` | `'nonce-{nonce}' 'strict-dynamic'` |
| `style-src` | `'nonce-{nonce}'` | `'nonce-{nonce}'` |

---

## Header Comparison: Default vs Strict A+

| Header | Default | Strict A+ | Difference |
|--------|---------|-----------|------------|
| HSTS max-age | 1 year | **2 years** | Longer enforcement |
| Referrer-Policy | `strict-origin-when-cross-origin` | **`no-referrer`** | Zero leakage |
| Permissions-Policy | 3 features | **29 features** | All denied |
| CSP script-src | nonce + strict-dynamic + `https:` | nonce + strict-dynamic | **No `https:` fallback** |
| CSP img-src | `'self' https: data:` | **`'self'`** | No external images |
| CSP connect-src | _(inherits 'none')_ | **`'self'`** | Explicit self |
| CSP font-src | _(inherits 'none')_ | **`'self'`** | Explicit self |
| CSP worker-src | _(inherits 'none')_ | **`'self'`** | Explicit self |
| CSP manifest-src | _(inherits 'none')_ | **`'self'`** | Explicit self |
| Trusted Types | _(disabled)_ | **Enabled** | DOM XSS protection |
