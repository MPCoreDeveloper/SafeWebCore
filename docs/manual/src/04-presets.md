# 4. Presets

A preset is a complete `NetSecureHeadersOptions` tuned for a shape of application. It is not a mode: it is data, produced
by a public static method in `SecurePresets`, which you can read, copy, adjust or ignore. That design is why a preset can
be reviewed in a pull request like any other object.

## The registration methods

Each preset has an extension method that registers the services with that preset applied:

| Method | For |
| --- | --- |
| `AddNetSecureHeadersStrictAPlus()` | Anything that can live with the strictest policy — a site with no inline script except what carries the nonce |
| `AddNetSecureHeadersApiPreset()` | JSON services: the browser-oriented headers are the point, the CSP is minimal |
| `AddNetSecureHeadersOwaspApiPreset()` | API services that want the OWASP-recommended set |
| `AddNetSecureHeadersMvcPreset()` | Server-rendered MVC and Razor Pages |
| `AddNetSecureHeadersBlazorPreset()` | Blazor Server or WebAssembly with the framework's needs |
| `AddNetSecureHeadersBlazorWebSocketPreset()` | Blazor Server that also opens the SignalR socket |
| `AddNetSecureHeadersSpaReverseProxyPreset()` | A SPA served behind the same origin as its API |
| `AddNetSecureHeadersReverseProxyPreset()` | An application that sits behind a proxy and must not claim otherwise |
| `AddNetSecureHeadersSwagger()` / `AddNetSecureHeadersNSwagPreset()` | Development-time API documentation |
| `AddNetSecureHeadersFromConfiguration(...)` | A preset-free path: bind the whole model from `IConfiguration` (Chapter 14) |

## What each one emphasises

| Preset | Emphasises | Notable choices |
| --- | --- | --- |
| `StrictAPlus` | The scanner's A+ with nothing relaxed | Everything enabled, `default-src 'none'`, nonce + `'strict-dynamic'`, cross-origin isolation on |
| `Api` | Correct for JSON, quiet for clients | HSTS, `nosniff`, frame and referrer rules; CSP limited to what an API needs |
| `ApiMinimal` | The smallest set that is still safe | The headers that cost nothing and break nothing |
| `OwaspApi` | The OWASP API Security set | The `Api` headers plus the OWASP additions |
| `Mvc` | Server-rendered HTML | A policy that a Razor view can satisfy with Tag Helpers |
| `Blazor` | The framework's script and style needs | Relaxations limited to what Blazor itself requires |
| `BlazorWebSocket` | Blazor Server over SignalR | The same, with `connect-src` for the socket |
| `SpaReverseProxy` | One origin for site and API | A policy that allows the SPA bundle without `'unsafe-inline'` |
| `ReverseProxy` | Honesty about the deployment | Choices that do not double-apply proxy-level headers |
| `Swagger` / `NSwag` | Documentation pages | What the tooling's inline script needs, for development |

## Applying a preset by hand

`ApplyPreset` copies every value from the preset onto the instance, and `Clone` returns an independent copy. That makes the
"preset, then the exceptions" pattern explicit:

```csharp
builder.Services.AddNetSecureHeaders(options =>
{
    options.ApplyPreset(SecurePresets.Mvc());

    // the exceptions, next to the preset they modify
    options.Csp = new CspBuilder()
        .ConnectSrc("'self' https://api.example.com")
        .ImgSrc("'self' https://cdn.example.com data:")
        .Build();

    options.EnableNel = true;
    options.NelValue = """{"report_to":"default","max_age":2592000}""";
    options.ReportingEndpoints =
    [
        new ReportingEndpointOptions { Group = "default", Url = "https://reports.example.com/csp" }
    ];
});
```

Two rules keep this readable. First, apply the preset **first** and adjust after it, so the exceptions are visibly
exceptions. Second, prefer replacing the whole `Csp` value with a builder chain over assigning twenty directives — the
builder is a record `with` expression per call, so the result is immutable and comparable in a test.

## Preset as a path policy

A path policy needs the same data as the application default, plus a prefix. `ApiPath` and `OwaspApiPath` return a
`PathPolicyOptions` for exactly that, and take an optional customization callback:

```csharp
builder.Services.AddNetSecureHeaders(options =>
{
    options.ApplyPreset(SecurePresets.Mvc());

    options.PathPolicies =
    [
        SecurePresets.ApiPath("/api"),
        SecurePresets.OwaspApiPath("/api/admin", api => api.EnableNel = true)
    ];
});
```

Chapter 8 explains the matching rules; Chapter 9 explains when an endpoint override is the better tool than a path
policy.

## Choosing

Start from the shape, not from the headers:

- **A browser loads your HTML and your scripts are yours** → `Mvc`, `Blazor` or `SpaReverseProxy`.
- **The only browser that talks to you is the one rendering documentation** → `Api` or `OwaspApi`.
- **You are not sure and can afford the strict version** → `StrictAPlus`, then relax one directive at a time, driven by
  the report-only evidence of Chapter 10.

> **Presets do not change between releases.** A preset is documented output: if 1.9.0 relaxed a directive that 1.8.1
> emitted, every application that relied on the old value would silently lose protection. Additive options are how a
> preset grows (Chapter 18).
