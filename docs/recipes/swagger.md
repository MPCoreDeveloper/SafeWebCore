# Recipe: Safe Swagger / OpenAPI UI

Swagger UI is a common source of CSP friction because it loads many scripts and styles dynamically.

## Recommended Approach (Dedicated Preset)

Use the Swagger-specific preset helper for the simplest integration:

```csharp
builder.Services.AddNetSecureHeadersSwagger(opts =>
{
    // Optionally tighten further or relax specific directives
    // opts.UseCspReportOnly = builder.Environment.IsDevelopment();
});
```

The `AddNetSecureHeadersSwagger` preset:
- Starts from a strong base (Strict A+)
- Allows `https://cdn.jsdelivr.net` for Swagger assets
- Permits `'unsafe-inline'` for styles (required by many Swagger UI versions)
- Uses `strict-origin-when-cross-origin` referrer policy for better UX
- Keeps strong transport and document security headers

## Alternative (Manual Customization)

If you need more control, start from another preset and customize:

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.ApplyPreset(SecurePresets.Api());

    opts.Csp = opts.Csp with
    {
        ScriptSrc = "'nonce-{nonce}' 'strict-dynamic' 'unsafe-inline' 'self' https://cdn.jsdelivr.net",
        StyleSrc = "'nonce-{nonce}' 'unsafe-inline' 'self' https://cdn.jsdelivr.net",
        ImgSrc = "'self' data: https:",
        ConnectSrc = "'self' https:",
        WorkerSrc = "'self' blob:"
    };

    // Optional: only in development/staging
    // opts.UseCspReportOnly = true;
});
```

## Environment-Aware Rollout

```csharp
builder.Services.AddNetSecureHeadersForEnvironment(builder.Environment, opts =>
{
    opts.Csp = opts.Csp with
    {
        ScriptSrc = "'nonce-{nonce}' 'strict-dynamic' 'self' https://cdn.jsdelivr.net",
        StyleSrc = "'nonce-{nonce}' 'unsafe-inline' 'self' https://cdn.jsdelivr.net",
        WorkerSrc = "'self' blob:"
    };
});
```

## Important Notes

- Swagger often requires `unsafe-inline` for styles in older versions.
- Newer versions of Swagger UI work better with nonces.
- Consider restricting Swagger to development/staging environments in production builds.
- Use the diagnostics endpoint (`/safewebcore/diagnostics`) to preview the effective CSP during development.
