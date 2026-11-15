# 3. Installation and the First A+ Response

## Install

The core package is `SafeWebCore`. The companion packages are separate and optional.

```bash
dotnet add package SafeWebCore
```

```xml
<PackageReference Include="SafeWebCore" Version="1.9.0" />
```

The package ships two assemblies: `lib/net10.0` and `lib/net11.0`. A .NET 11 application resolves the build compiled
against .NET 11 (C# 15, the .NET 11 JIT); a .NET 10 application keeps the `net10.0` build. The public API is identical,
and the dependency floors of the `net10.0` asset are the ones that shipped in 1.8.1.

## The first response

Two calls: one registers the services and the options, one adds the middleware. Order matters only in that the
registration must happen before `builder.Build()`.

```csharp
using SafeWebCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNetSecureHeadersStrictAPlus();

var app = builder.Build();

app.UseNetSecureHeaders();

app.MapGet("/", () => "Hello, headers.");

app.Run();
```

Ask for the headers and read them back:

```bash
curl -sS -D - http://localhost:5000/ -o /dev/null
```

```text
HTTP/1.1 200 OK
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
X-Frame-Options: DENY
X-Content-Type-Options: nosniff
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: camera=(), microphone=(), geolocation=()
Cross-Origin-Embedder-Policy: require-corp
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Resource-Policy: same-origin
X-DNS-Prefetch-Control: off
X-Permitted-Cross-Domain-Policies: none
Content-Security-Policy: default-src 'none'; script-src 'nonce-Kf3…' 'strict-dynamic' https:; style-src 'nonce-Kf3…'; …
```

That response is what a scanner grades A+. Three details are worth noticing, because they come back in later chapters:

- The nonce is different on every request; the policy is assembled from a template computed once, with the nonce
  substituted per request.
- `Server` is gone: `RemoveServerHeader` defaults to `true` and deletes it in `OnStarting`, so a component that adds it
  later still loses it.
- No CSP violation is possible yet in the sense that matters: an inline `<script>` without the nonce will be blocked,
  which is the point, and Chapter 7 shows how to give your own scripts the attribute.

## Development, staging, production

HSTS on `localhost` is harmless (browsers ignore it for non-HTTPS origins) but useless, and a strict CSP on a
development machine that serves from a live-reload server is a nuisance. The `ForEnvironment` overloads exist for that:
they take the environment name and fall back to report-only when it is not production.

```csharp
builder.Services.AddNetSecureHeadersStrictAPlusForEnvironment(builder.Environment);
```

Chapter 10 describes exactly what those overloads change and how to prove it in one request.

## A first test, before the first bug report

The testing helpers turn "the headers are there" into one assertion, so the first test costs a minute.

```csharp
using SafeWebCore.Testing;

[Fact]
public async Task HomePage_CarriesSecurityHeaders()
{
    using var host = TestHostBootstrapExtensions.CreateSafeWebCoreTestHost();
    using var client = host.GetTestClient();

    var response = await client.GetAsync("/");

    response.AssertHasSecurityHeaders();
    response.AssertHasCspEnforceMode();
    response.AssertHasNonceInCsp();
}
```

**The package** is installed; **the response** is graded; **the suite** can prove it. Chapter 4 replaces the one-line
strict preset with a preset chosen for the application's shape, and Chapter 10 replaces enforcement with a report-only
rollout when the application is not ready for it yet.
