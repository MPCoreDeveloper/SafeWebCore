# SafeWebCore.JwtBearer Demo

This example reproduces the exact scenario from **Stephan van Rooij's** issue
[dotnet/aspnetcore#67991](https://github.com/dotnet/aspnetcore/issues/67991)
and shows how **`SafeWebCore.JwtBearer`** fixes it.

> [!IMPORTANT]
> If a **security** feature is **misconfigured**, the application should fail fast at
> startup instead of starting normally while silently returning `401` for everything.
> — **Stephan van Rooij ([@svrooij](https://github.com/svrooij))**, issue #67991, July 2026

The .NET team scheduled a fix for **.NET 12 Planning** only. This module gives you the
behavior today, on .NET 10.

## The faulty behavior (the bug)

The `Authority` is misspelled (`organisations` instead of `organizations`). Microsoft's
discovery endpoint for that URL returns **HTTP 400**, not 404, so the app:  
- starts and runs normally,  
- never loads signing keys,  
- returns `401 invalid_token` for **every** request,  
- logs the failure only at **Information** level, which the default `Microsoft.AspNetCore: Warning`
  filter hides — so **nothing appears in the logs**.

Request result (from issue #67991):

```text
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer error="invalid_token", error_description="The signature key was not found"
```

## The fix (`SafeWebCore.JwtBearer`)

The module registers a **startup guard** (`JwtAuthorityValidationGuard`) that eagerly loads the
OpenID Connect metadata for the configured authority. An HTTP **4xx** response (the typo case) is
classified as a **permanent misconfiguration** and:

| Mode | Behavior |
|------|----------|
| `FailFast = true` *(secure default)* | The application **refuses to start** with a clear error that names the scheme and the metadata address. |
| `FailFast = false` | The application starts but logs at **Error** level; every request keeps failing closed (401) until the authority is fixed. |

Transient failures (timeout, 5xx, DNS) only log a **Warning**, so a short identity-provider outage
does not take the app down.

## Run it yourself

```bash
cd examples/JwtBearerDemo

# FIXED mode (default): the app refuses to start because the authority 400s
dotnet run

# BROKEN mode: the stock behavior from issue #67991 - app starts, everything 401s silently
dotnet run -- --broken
```

### See the broken behavior first

1. `dotnet run -- --broken` — the app starts and logs `Application started`.
2. Send the request from [`JwtBearerDemo.http`](JwtBearerDemo.http) (VS Code REST Client).  
   → **`401`**, and no `Error`/`Warning` in the console.

### Then apply the fix

1. `dotnet run` (default, guard enabled).
2. Startup hits the metadata endpoint, receives **HTTP 400**, and stops:

```text
Unhandled exception. System.InvalidOperationException: The JWT authority for scheme 'Bearer'
is misconfigured (HTTP 4xx from 'https://login.microsoftonline.com/organisations/v2.0').
Fix the Authority/MetadataAddress before starting.
```

You can also try `AddJwtBearerAuthorityValidation(o => o.FailFast = false)` in
[`Program.cs`](Program.cs) to keep the app running while logging an `Error` with the same
information.

## What this demo teaches

- A misconfigured authority fails **closed** (401), which is secure — the lock works.  
- What is missing in stock ASP.NET Core is the **loudness**: nothing surfaces at the default log level.  
- `SafeWebCore.JwtBearer` restores that loudness **at startup**, using the exact 4xx classification.

See the main [module README](../../src/SafeWebCore.JwtBearer/README.md) for the full API
(`AddJwtBearerHardening`, `AddSafeWebCoreJwtBearer`, token hardening, runtime metadata logging).