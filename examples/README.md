# SafeWebCore – Examples

Four runnable ASP.NET Core applications that demonstrate different integration patterns for [SafeWebCore](../README.md) — including a companion-module demo for `SafeWebCore.JwtBearer`.

All examples reference the local source projects directly so you can run them straight from a clone — no NuGet restore from the feed required.

## Examples at a glance

| Example | Pattern | Key features demonstrated |
|---------|---------|--------------------------|
| [MinimalApi](MinimalApi/) | ASP.NET Core Minimal API | `AddNetSecureHeadersStrictAPlus`, `GetCspNonce()`, `SkipNetSecureHeaders()`, CSP report endpoint |
| [MvcApp](MvcApp/) | ASP.NET Core MVC + Razor Views | MVC preset, typed policy builders, path policies, `[CspNonce]` attribute, nonce TagHelpers |
| [ApiService](ApiService/) | Web API with controllers | API preset, custom `ICspReportSink`, `[SkipNetSecureHeaders]`, `[CspMode]` endpoint overrides |
| [JwtBearerDemo](JwtBearerDemo/) | Minimal API + JWT | `SafeWebCore.JwtBearer`: broken JWT authority (dotnet/aspnetcore#67991) vs. fail-fast startup |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)

## Running an example

```bash
# MinimalApi
cd examples/MinimalApi
dotnet run

# MvcApp
cd examples/MvcApp
dotnet run

# ApiService
cd examples/ApiService
dotnet run

# JwtBearerDemo (read its README first: broken vs. fixed JWT authority)
cd examples/JwtBearerDemo
dotnet run
```

Each example starts on `http://localhost:5000` by default (JwtBearerDemo uses `http://localhost:5120`). Open your browser or HTTP client and inspect the response headers in DevTools → Network to see the security headers in action.

> [!NOTE]
> **JwtBearerDemo** is the reproduction of [dotnet/aspnetcore#67991](https://github.com/dotnet/aspnetcore/issues/67991)
> (reported by Stephan van Rooij). It is not about security headers: it shows how a misspelled
> JWT authority silently 401s every request, and how `SafeWebCore.JwtBearer` makes that a
> fail-fast (or loud-log) startup event instead. See its [README](JwtBearerDemo/README.md).

## Feature matrix

| Feature | MinimalApi | MvcApp | ApiService |
|---------|:----------:|:------:|:----------:|
| Strict A+ preset | ✅ | | |
| API preset | | | ✅ |
| MVC preset | | ✅ | |
| CSP nonce (`GetCspNonce()`) | ✅ | | |
| `[CspNonce]` controller attribute | | ✅ | |
| Nonce TagHelpers | | ✅ | |
| Typed `ReferrerPolicyBuilder` | | ✅ | |
| Typed `PermissionsPolicyBuilder` | | ✅ | |
| Typed `CrossOriginPolicyBuilder` | | ✅ | |
| Path-based policies | ✅ | ✅ | ✅ |
| CSP report-only mode | ✅ | ✅ | |
| `SkipNetSecureHeaders` | ✅ | | ✅ |
| `[CspMode]` endpoint override | | | ✅ |
| Custom `ICspReportSink` | | | ✅ |
| Optional `X-Robots-Tag` | | | ✅ |

## Next steps

- Read the full [documentation](../docs/) for API reference and configuration options.
- See [docs/presets.md](../docs/presets.md) for all available presets and customisation patterns.
- See [docs/csp-configuration.md](../docs/csp-configuration.md) for the fluent CSP builder and nonce usage.
- See [docs/advanced-configuration.md](../docs/advanced-configuration.md) for path policies, endpoint overrides, and custom sinks.