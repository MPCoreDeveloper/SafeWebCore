# Recipe: Behind Reverse Proxy / YARP / CDN / IIS

When your application sits behind a reverse proxy, load balancer, CDN or IIS, some headers may need special handling.

## Core Recommendations

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    // Keep removal of server headers (SafeWebCore does this via OnStarting)
    opts.RemoveServerHeader = true;
    opts.RemoveXPoweredBy = true;

    // If you terminate TLS at the proxy, you may want to relax HSTS or let the proxy set it
    // opts.EnableHsts = false;
});
```

## Common Issues

| Problem | Cause | Fix |
|---------|-------|-----|
| `Server` or `X-Powered-By` still visible | Proxy or IIS re-adds the header | Configure removal at the edge (IIS `web.config`, nginx `proxy_hide_header`, Cloudflare, etc.) |
| HSTS missing or duplicate | TLS termination at proxy | Let the proxy set HSTS, or ensure the app sees the original scheme |
| CSP blocked resources | Proxy rewrites URLs or injects scripts | Allow the proxy/CDN domains explicitly in CSP |

## Example web.config (IIS)

```xml
<system.webServer>
  <httpProtocol>
    <customHeaders>
      <remove name="X-Powered-By" />
      <remove name="Server" />
    </customHeaders>
  </httpProtocol>
</system.webServer>
```

## Diagnostics

Use `/safewebcore/diagnostics` (when enabled) to see what headers SafeWebCore *intends* to emit, then compare with the final response at the edge.
