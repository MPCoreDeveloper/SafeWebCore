# Recipe: CSP Report-Only Rollout

A safe way to introduce or tighten a CSP policy.

## Recommended Pattern

Use the environment-aware helpers:

```csharp
builder.Services.AddNetSecureHeadersStrictAPlusForEnvironment(builder.Environment, opts =>
{
    // Start strict, but allow necessary sources
    opts.Csp = opts.Csp with
    {
        ConnectSrc = "'self' https://api.example.com wss:",
        ImgSrc = "'self' https: data:",
        ScriptSrc = "'nonce-{nonce}' 'strict-dynamic' 'self' https://cdn.example.com"
    };
});
```

In non-production environments, this will automatically set `UseCspReportOnly = true`.

## When to Switch to Enforce

1. Collect violations for a period (days/weeks).
2. Analyze reports (via `ICspReportSink` or the built-in logging sink).
3. Adjust the policy.
4. In production (or when ready), explicitly set:
   ```csharp
   opts.UseCspReportOnly = false;
   ```

## Monitoring

- Use the diagnostics endpoint (`/safewebcore/diagnostics`) to preview the current mode.
- Register custom `ICspReportSink` implementations to forward violations to your SIEM / logging system.
