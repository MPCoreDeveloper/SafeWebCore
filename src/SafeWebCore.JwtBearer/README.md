# SafeWebCore.JwtBearer

Optional companion module for **SafeWebCore** that makes a misconfigured or unreachable
**JWT authority fail loud before your users ever see a 401** — and optionally hardens
your token validation rules.

This module exists because of a real issue reported by **Stephan van Rooij**
([@svrooij](https://github.com/svrooij)):
[**dotnet/aspnetcore#67991** — *"Setup JwtBearer authentication with faulty authority should crash"*](https://github.com/dotnet/aspnetcore/issues/67991).

> If a security feature is misconfigured, the application should fail fast at startup
> instead of starting normally while silently returning 401 for everything.

The .NET team only scheduled the fix for **.NET 12 Planning**
([milestone](https://github.com/dotnet/aspnetcore/issues/67991)). This module gives you the
behavior today, on **.NET 10**, plus additional opt-in JWT hardening.

---

## The problem (Stephan's repro)

A misspelled authority (`organisations` instead of `organizations`) makes the app **start fine**
but return **`401 invalid_token` for every request** — while logging the root cause only at
**Information** level, below the default `Microsoft.AspNetCore: Warning` filter. So: an app that
runs green, logs nothing, and 401s everything. Requests fail **closed** (no bypass), but the
**loudness is missing.**

With SafeWebCore.JwtBearer, that same app either **refuses to start** (fail fast) or starts with a
**loud Error log**, depending on your preference.

```text
Unhandled exception. System.InvalidOperationException: The JWT authority for scheme 'Bearer'
is misconfigured (HTTP 4xx from 'https://login.microsoftonline.com/organisations/v2.0').
Fix the Authority/MetadataAddress before starting.
```

## Install

```bash
dotnet add package SafeWebCore.JwtBearer
```

## Quick start

### Option 1 — Startup validation only (Stephan's fix)

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* your authority, issuer, audience, ... */ });

// Fail fast at startup when the authority is misconfigured (HTTP 4xx):
builder.Services.AddJwtBearerAuthorityValidation(o => o.FailFast = true);

// Or fail loud but keep the app running (requests still fail closed):
// builder.Services.AddJwtBearerAuthorityValidation(); // FailFast defaults to false
```

### Option 2 — Startup validation + token hardening (recommended)

```csharp
builder.Services.AddJwtBearerHardening(o =>
{
    o.ValidateAudience = true;
    o.ValidAudiences.Add("api://my-api");
    o.AllowedAlgorithms.Add("RS256");
    o.MaximumTokenLifetime = TimeSpan.FromHours(1);
});
```

Hardening is applied after your `AddJwtBearer` configuration and can only **increase** strictness;
stronger settings you already defined are never weakened. It also registers the startup guard with
`FailFast = true` as a secure default.

### Option 3 — Everything in one line

```csharp
builder.Services.AddSafeWebCoreJwtBearer(
    options =>
    {
        options.Authority = "https://login.microsoftonline.com/organizations/v2.0";
        options.TokenValidationParameters.ValidAudiences = new[]
        {
            "api://98f7735b-23c5-4b12-bc16-0e2f3a5d7e21",
        };
    },
    hardening => hardening.MaximumTokenLifetime = TimeSpan.FromHours(1));
```

## Startup validation

| Option | Default | Description |
|--------|---------|-------------|
| `Scheme` | `Bearer` | The JWT bearer authentication scheme to validate. |
| `FailFast` | `false` | When true, a permanent (HTTP 4xx) authority failure **throws at startup**. |
| `EnforceStaticConfigurationChecks` | `true` | Runs deterministic, network-free checks: absolute HTTPS authority (when `RequireHttpsMetadata` is on), audience must be configured when validation is enabled, issuer required when no metadata resolves it, `none` algorithm rejected. |

**4xx is permanent** (typo, unknown tenant, revoked metadata) → `Error` log and, with `FailFast`,
a startup crash. **Everything else** (5xx, timeout, DNS) is transient → `Warning` log, the app
starts, and requests fail closed (401) until the identity provider is reachable.

## Token hardening (`JwtBearerHardeningOptions`)

| Option | Default | Enforces |
|--------|---------|----------|
| `RequireSignedTokens` | `true` | Tokens must be signed (`alg: none` rejected). |
| `RequireExpirationTime` | `true` | An `exp` claim must be present. |
| `ValidateLifetime` | `true` | `nbf`/`exp` ranges are validated. |
| `RequireTokenType` | `true` | The `typ` header must be present and match `AllowedTokenTypes` (`JWT`, `at+jwt`). |
| `RequireJwtId` | `false` | A unique `jti` (JWT ID) claim is required. |
| `RequireNotBefore` | `false` | An `nbf` claim is required. |
| `RequireIssuedAt` | `false` | An `iat` claim is required. |
| `MaximumClockSkew` | `5 min` | Clock skew is capped at this value (the tighter of existing and configured wins). |
| `MaximumTokenLifetime` | `null` | Tokens whose valid lifetime (`exp - nbf`) exceeds this are rejected. Common: 1 hour. |
| `AllowedAlgorithms` | *(empty)* | When non-empty, only the listed JWS algorithms are accepted (`RS256`, `ES256`, `PS256`, ...). |
| `ValidateIssuer` / `ValidIssuers` | `false` / *(empty)* | Enforces issuer validation against the configured issuer(s). |
| `ValidateAudience` / `ValidAudiences` | `false` / *(empty)* | Enforces audience validation against the configured audience(s). |
| `EnableRuntimeMetadataLogging` | `true` | Wraps the configuration manager so metadata failures surf at `Error` (4xx) / `Warning` also during runtime, not just at startup. |

## Runtime metadata logging

.NET/IdentityModel only logs OpenID Connect metadata retrieval failures at **Information** level.
This module wraps the configuration manager (via `IPostConfigureOptions`) so that a **4xx** metadata
failure during runtime is logged at **Error** and a transient failure at **Warning**, rate-limited to
one line per refresh interval. Requires `AddJwtBearerHardening`/`AddSafeWebCoreJwtBearer` to run
**after** `AddJwtBearer`.

## How it works

1. **`JwtAuthorityValidationGuard`** (an `IHostedService`, registered at startup):
   - runs the deterministic configuration checks, then eagerly fetches the OpenID Connect metadata
     (issuer + signing keys) for your scheme's `IConfigurationManager`;
   - **HTTP 4xx** → permanent → `Error` (+ fail fast when configured);
   - **anything else** → transient → `Warning`, app starts;
   - success → `Information`. Nothing else changes.
2. **`JwtBearerHardeningApplication`** applies the token-validation defaults and chains claim checks
   (`typ`, `jti`, `nbf`, `iat`, maximum lifetime) onto your existing `OnTokenValidated` handler.
3. **`LoggingConfigurationManager`** decorates the manager so metadata failures that actually surface
   (no usable cached configuration) are logged at `Error`/`Warning` during runtime, not just at startup.

## Reproduce it yourself

The repository includes [**`examples/JwtBearerDemo`**](../../examples/JwtBearerDemo/) — a copy of
Stephan's exact reproduction (misspelled `organisations` authority, his exact token-validation
settings) with a `--broken` switch between the broken and the fixed behavior.

## Limitations

- The guard validates the authority **once at startup**; requests keep failing closed (401) whenever
  tokens cannot be validated. The runtime metadata logging (Option 2/3) reports retrieval failures
  that **surface** — once IdentityModel has loaded a healthy configuration, its last-known-good
  behavior serves the cached metadata without throwing, so an identity-provider outage *after* a
  healthy start produces no Error log (requests are unaffected while the cached config is valid).
- The metadata is fetched one extra time at startup (which also warms the configuration cache).
- `AddJwtBearerHardening` must be registered **after** `AddJwtBearer`.

## License

MIT, same as SafeWebCore.