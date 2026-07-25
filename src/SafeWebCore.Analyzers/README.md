# SafeWebCore.Analyzers

`SafeWebCore.Analyzers` is the ` v1.5 and newer ` tooling package for SafeWebCore.

## Purpose

This package provides Roslyn analyzers that catch common SafeWebCore integration mistakes at build time, before they become runtime or production issues.

## Rules

### SWC001 — SafeWebCore middleware is registered but not used

**Severity:** Warning  
**Category:** SafeWebCore

**When reported:**  
You call one of the registration methods:

- `AddNetSecureHeaders(...)`
- `AddNetSecureHeadersStrictAPlus(...)`
- `AddNetSecureHeadersFromConfiguration(...)`
- `AddNetSecureHeaders*Preset(...)`
- `AddNetSecureHeadersForEnvironment(...)`
- `AddNetSecureHeadersStrictAPlusForEnvironment(...)`

...but `UseNetSecureHeaders()` is never called on the application pipeline.

**Why it matters:**  
Registration only adds services. Without `UseNetSecureHeaders()`, the middleware never runs and no security headers are emitted.

**Example of the problem:**

```csharp
// In Program.cs or Startup
builder.Services.AddNetSecureHeadersStrictAPlus();

// ... later in the pipeline
app.UseRouting();
// Missing: app.UseNetSecureHeaders();
app.MapControllers();
```

**Fix:**

```csharp
app.UseNetSecureHeaders();   // Add this
```

The analyzer uses a compilation-wide heuristic. It reports on every registration call when no `UseNetSecureHeaders` call is found anywhere in the compilation.

### SWC002 — CSP is configured in report-only mode

**Severity:** Warning  
**Category:** SafeWebCore

**When reported:**  
`UseCspReportOnly = true` is set (either directly or in object initializers / configuration).

**Why it matters:**  
Report-only mode is very useful during development and rollout, but it is frequently left permanently enabled. This means CSP violations are only logged — the policy never actually blocks anything.

**Example of the problem:**

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.UseCspReportOnly = true;   // ← Warning SWC002
});
```

**Recommended practice:**
- Use report-only during development/staging (via `AddNetSecureHeaders*ForEnvironment` helpers)
- Explicitly set `UseCspReportOnly = false` when you are ready to enforce
- Or remove the flag entirely when you want strict enforcement

## Current status

- **SWC001** — Detects registration without `UseNetSecureHeaders()`
- **SWC002** — Detects permanent `UseCspReportOnly = true`
- **SWC003** — Detects `'unsafe-inline'` without a nonce
- **SWC004** — Detects overly broad CSP sources (`*`, bare `https:`, `unsafe-eval`)

Package is additive and opt-in. More rules may be added in future v1.5.x releases.

## Installation (when published)

```bash
dotnet add package SafeWebCore.Analyzers
```

The analyzer will be automatically discovered by the .NET SDK.

## Compatibility

This package does not change runtime behavior of SafeWebCore. It only provides build-time diagnostics. Existing consumers are unaffected unless they reference this package.
