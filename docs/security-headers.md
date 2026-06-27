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
| `PermissionsPolicyValue` | `camera=(), microphone=(), geolocation=()` | All recognized features denied (scanner-safe) |

**What it does:** Restricts which browser features your page (and embedded iframes) can use.

**Strict A+ disables recognized features only (avoids scanner "invalid directive" warnings):**

```
accelerometer=(), autoplay=(), camera=(), clipboard-read=(), clipboard-write=(),
display-capture=(), encrypted-media=(), fullscreen=(), geolocation=(),
gyroscope=(), hid=(), idle-detection=(), local-fonts=(), magnetometer=(),
microphone=(), midi=(), payment=(), picture-in-picture=(),
publickey-credentials-get=(), screen-wake-lock=(), serial=(), usb=(),
web-share=(), xr-spatial-tracking=()
```

> Note: Tokens such as `identity-credentials-get`, `otp-credentials`, `publickey-credentials-create`, and `window-management` are intentionally omitted — security scanners (e.g. securityheaders.com) currently flag them as invalid.

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

### How removal works (using `OnStarting`)

SafeWebCore performs removal of the `Server` header (constant: `HeaderNames.Server`) by registering a delegate with `HttpResponse.OnStarting`.

`OnStarting` is the **latest possible hook** in the ASP.NET Core response lifecycle. The registered callback is invoked right before the response headers are flushed to the client.  
This design allows SafeWebCore to remove headers that Kestrel, the hosting layer, or other middleware may have added *after* your application code executed.

The exact same `OnStarting` pattern is used for `X-Powered-By` (see below) when `RemoveXPoweredBy` is enabled. Internally the code uses `HeaderNames.XPoweredBy`.

### Important hosting limitations

Headers removed via `OnStarting` **can still appear** in real deployments because many components inject them **outside** the ASP.NET Core application process:

- **IIS** (in-process or out-of-process): The AspNetCoreModule often re-adds `Server` and `X-Powered-By` after the managed pipeline has completed.
- **Reverse proxies / CDNs / load balancers**: nginx, Azure Front Door, Application Gateway, Cloudflare, AWS ALB, etc. frequently overwrite or inject the `Server` header.
- **Container sidecars**, service meshes, or observability agents that sit in front of or after the application.

For reliable removal you must usually configure it at the **web server / proxy / host** level in addition to (or instead of) the SafeWebCore setting.

**IIS `web.config` example (strongly recommended for production):**

```xml
<system.webServer>
  <httpProtocol>
    <customHeaders>
      <remove name="X-Powered-By" />
      <!-- <remove name="Server" /> -->
    </customHeaders>
  </httpProtocol>
</system.webServer>
```

Other common techniques: IIS URL Rewrite rules, request filtering, or the `aspnetcore:HostingStartupExcludeHosts` setting.

---

## X-Powered-By Header Removal

**Attack prevented:** Server/framework fingerprinting (ASP.NET, PHP, etc.)

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `RemoveXPoweredBy` | `false` | `true` |

**What it does:** Removes the `X-Powered-By` response header (commonly `X-Powered-By: ASP.NET`) that leaks implementation details.

Removal is implemented with the identical `OnStarting` technique described in the [Server Header Removal](#server-header-removal) section (using the constant `HeaderNames.XPoweredBy`). This gives the highest chance of success inside the ASP.NET Core pipeline.

> **Critical for IIS and reverse proxies**  
> The IIS AspNetCoreModule (and many reverse proxies) can still inject `X-Powered-By` or `Server` **after** the application has finished writing headers.  
> When running on IIS, always combine `RemoveXPoweredBy = true` with the `<remove name="X-Powered-By" />` entry in `web.config` shown above to achieve complete removal in production.

---

## NEL (Network Error Logging)

**Attack prevented / Use case:** Proactive collection of network and application errors (DNS, TCP, HTTP, CORS failures, etc.)

| Setting | Default | Strict A+ |
|---------|---------|-----------|
| `EnableNel` | `false` | `false` (opt-in) |
| `NelValue` | (empty) | (not set) |

**What it does:** Instructs supporting browsers to send structured error reports to a collector endpoint you control (often via `Reporting-Endpoints` + Report URI or similar).

Example configuration:

```csharp
opts.EnableNel = true;
opts.NelValue = """{"report_to":"default","max_age":2592000,"include_subdomains":true}""";
```

Combine with:

```csharp
opts.ReportingEndpoints.Add(new ReportingEndpointOptions
{
    Group = "default",
    Url = "https://your-report-uri.example.com/nel"
});
```

> NEL is a newer header. It is emitted only when `EnableNel` is true **and** `NelValue` is non-empty.

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
| Permissions-Policy | 3 features | **~24 scanner-safe features** | All recognized denied (no invalid tokens) |
| Server removed | `true` | `true` | Same (uses `OnStarting`; may need host-level config for IIS/proxies) |
| X-Powered-By removed | `false` | **`true`** | New in Strict A+ (same `OnStarting` mechanism) |
| NEL | `false` | `false` (opt-in) | Opt-in via `EnableNel` + `NelValue` |
| CSP script-src | nonce + strict-dynamic + `https:` | nonce + strict-dynamic | **No `https:` fallback** |
| CSP img-src | `'self' https: data:` | **`'self'`** | No external images |
| CSP connect-src | _(inherits 'none')_ | **`'self'`** | Explicit self |
| CSP font-src | _(inherits 'none')_ | **`'self'`** | Explicit self |
| CSP worker-src | _(inherits 'none')_ | **`'self'`** | Explicit self |
| CSP manifest-src | _(inherits 'none')_ | **`'self'`** | Explicit self |
| Trusted Types | _(disabled)_ | **Enabled** | DOM XSS protection |
