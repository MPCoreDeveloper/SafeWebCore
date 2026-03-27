# Getting Started with SafeWebCore

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- Visual Studio 2026+ / VS Code with C# Dev Kit / JetBrains Rider

## Installation

```bash
dotnet add package SafeWebCore
```

Or add to your `.csproj`:

```xml
<PackageReference Include="SafeWebCore" Version="1.0.0" />
```

## Minimal Setup (A+ in 3 lines)

```csharp
using SafeWebCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNetSecureHeadersStrictAPlus();   // 1. Register services

var app = builder.Build();
app.UseNetSecureHeaders();                            // 2. Add middleware
app.MapGet("/", () => "Secure!");                     // 3. Your endpoints

app.Run();
```

## Verifying Your Headers

### Option A: Browser DevTools

1. Open your app in the browser
2. Open DevTools → **Network** tab
3. Click on the page request
4. Check the **Response Headers** section

You should see all security headers listed.

### Option B: curl

```bash
curl -I https://localhost:5001
```

Expected output (truncated):

```
HTTP/2 200
strict-transport-security: max-age=63072000; includeSubDomains; preload
x-frame-options: DENY
x-content-type-options: nosniff
referrer-policy: no-referrer
permissions-policy: accelerometer=(), camera=(), microphone=(), ...
cross-origin-embedder-policy: require-corp
cross-origin-opener-policy: same-origin
cross-origin-resource-policy: same-origin
x-dns-prefetch-control: off
x-permitted-cross-domain-policies: none
content-security-policy: default-src 'none'; script-src 'nonce-abc123...' 'strict-dynamic'; ...
```

### Option C: securityheaders.com

1. Deploy your app to a public URL
2. Visit [securityheaders.com](https://securityheaders.com)
3. Enter your URL and scan
4. You should see an **A+** rating

## Next Steps

| Topic | Link |
|-------|------|
| Understand each header | [Security Headers Guide](security-headers.md) |
| Configure CSP in detail | [CSP Configuration](csp-configuration.md) |
| Customize the A+ preset | [Presets](presets.md) |
| Custom policies & reporting | [Advanced Configuration](advanced-configuration.md) |
