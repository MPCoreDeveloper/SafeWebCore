# Recipe: Safe NSwag UI

[NSwag](https://github.com/RicoSuter/NSwag) is a popular .NET Swagger/OpenAPI toolkit (NSwagStudio, ASP.NET Core middleware). Its UI is stricter than classic Swagger UI because it loads assets from `https://unpkg.com/nswag/` and works with nonces + `'strict-dynamic'` — so SafeWebCore can enforce a strong CSP **without** `'unsafe-inline'`.

## Recommended Approach (Dedicated Preset)

```csharp
builder.Services.AddNetSecureHeadersNSwagPreset(opts =>
{
    // Optional tweaks
    // opts.ReferrerPolicyValue = "no-referrer";
});
```

The `AddNetSecureHeadersNSwagPreset` preset:
- Starts from a strong base (Strict A+)
- Allows `https://unpkg.com` for NSwag assets (scripts + styles)
- Uses nonce-based CSP with `'strict-dynamic'` — **no `'unsafe-inline'`**
- Uses `strict-origin-when-cross-origin` referrer policy for better UX
- Keeps strong transport and document security headers

## Alternative (Manual Customization)

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.ApplyPreset(SecurePresets.StrictAPlus());

    opts.Csp = opts.Csp with
    {
        ScriptSrc = "'self' 'nonce-{nonce}' 'strict-dynamic' https://unpkg.com",
        StyleSrc = "'self' 'nonce-{nonce}' https://unpkg.com",
        ImgSrc = "'self' data: https:",
        FontSrc = "'self' https://unpkg.com",
        ConnectSrc = "'self' https:",
        WorkerSrc = "'self' blob:"
    };

    opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";
});
```

## Environment-Aware Rollout

```csharp
builder.Services.AddNetSecureHeadersForEnvironment(builder.Environment, opts =>
{
    opts.Csp = opts.Csp with
    {
        ScriptSrc = "'self' 'nonce-{nonce}' 'strict-dynamic' https://unpkg.com",
        StyleSrc = "'self' 'nonce-{nonce}' https://unpkg.com",
        WorkerSrc = "'self' blob:"
    };
});
```

## Important Notes

- **NSwag vs Swagger:** NSwag is stricter — no `'unsafe-inline'` needed. The Swagger preset (`AddNetSecureHeadersSwagger`) is for classic Swagger UI and still requires `'unsafe-inline'` for styles in many versions.
- If you use NSwag's `Owin`/ASP.NET middleware the same host serves both API and UI — these headers already cover the `/swagger` path.
- Consider restricting NSwag UI to development/staging environments in production builds.
- Use the diagnostics endpoint (`/safewebcore/diagnostics`) to preview the effective CSP during development.