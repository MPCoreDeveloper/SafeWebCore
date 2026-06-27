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
<PackageReference Include="SafeWebCore" Version="1.3.5" />
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

## Using CSP Nonces *(v1.1.0+)*

SafeWebCore generates a unique per-request nonce for `<script>` and `<style>` elements.

### In Minimal API / Controllers

```csharp
app.MapGet("/page", (HttpContext ctx) =>
{
    var nonce = ctx.GetCspNonce();  // v1.1.0+
    return Results.Content(
        $"""
        <html>
        <script nonce="{nonce}">console.log('CSP nonce: {nonce}');</script>
        <body>Hello, secure world!</body>
        </html>
        """,
        "text/html");
});
```

### Zero-Allocation Nonce Generation *(v1.1.0+)*

For high-throughput scenarios, write the nonce directly to a `Span<char>` to avoid allocations:

```csharp
using SafeWebCore;

var nonceService = HttpContext.RequestServices.GetRequiredService<NonceService>();
Span<char> nonceBuffer = stackalloc char[NonceService.NonceLength];  // 44 chars

if (nonceService.TryWriteNonce(nonceBuffer))
{
    var nonce = new string(nonceBuffer);
    // Use nonce...
}
```

### In Razor Views with TagHelpers *(v1.1.0+)*

Register TagHelpers in `_ViewImports.cshtml`:

```razor
@addTagHelper *, SafeWebCore
```

Then nonce is injected automatically:

```html
<script>
    console.log('Nonce injected automatically!');
</script>

<style>
    body { font-family: sans-serif; }
</style>
```

## Fully Custom Setup

Prefer full control? Use `AddNetSecureHeaders` and configure every header yourself:

```csharp
using SafeWebCore.Builder;
using SafeWebCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNetSecureHeaders(opts =>
{
    opts.EnableHsts = true;
    opts.HstsValue = "max-age=31536000; includeSubDomains";

    opts.EnableXFrameOptions = true;
    opts.XFrameOptionsValue = "SAMEORIGIN";

    opts.EnableXContentTypeOptions = true;
    opts.EnableReferrerPolicy = true;
    opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";

    opts.EnablePermissionsPolicy = true;
    opts.PermissionsPolicyValue = "camera=(), microphone=(), geolocation=()";

    opts.RemoveServerHeader = true;
    opts.RemoveXPoweredBy = true; // removes X-Powered-By (enabled by default in Strict A+ presets)

    // CSP — use the fluent builder
    opts.Csp = new CspBuilder()
        .DefaultSrc("'none'")
        .ScriptSrc("'nonce-{nonce}' 'strict-dynamic' https:")
        .StyleSrc("'nonce-{nonce}'")
        .ImgSrc("'self' https: data:")
        .ConnectSrc("'self'")
        .FrameAncestors("'none'")
        .BaseUri("'none'")
        .FormAction("'self'")
        .UpgradeInsecureRequests()
        .Build();
});

var app = builder.Build();
app.UseNetSecureHeaders();
app.Run();
```

Both methods are defined in `SafeWebCore.Extensions.ServiceCollectionExtensions`.

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

This tool grades **all** security headers (HSTS, CSP, X-Frame-Options, Permissions-Policy, etc.) from A+ through F.

### Option D: Google CSP Evaluator

1. Copy the `Content-Security-Policy` header value from DevTools or `curl` output
2. Visit [csp-evaluator.withgoogle.com](https://csp-evaluator.withgoogle.com/)
3. Paste the header value and click **Check CSP**
4. All checks should be green with SafeWebCore's defaults

Google's CSP Evaluator checks for common misconfigurations like missing `object-src`, `'unsafe-inline'` without nonce, and missing `'strict-dynamic'`.

> 💡 **Tip:** Always validate with both tools after any CSP changes. See the [CSP Configuration Guide](csp-configuration.md#validate-your-csp) for detailed usage instructions.

## Next Steps

| Topic | Link |
|-------|------|
| **See working examples** | [Examples](examples.md) (MinimalApi, MvcApp, ApiService) |
| Understand each header | [Security Headers Guide](security-headers.md) |
| Configure CSP in detail | [CSP Configuration](csp-configuration.md) |
| Customize the A+ preset | [Presets](presets.md) |
| Custom policies, report-only rollout, path-based policies, and TagHelpers | [Advanced Configuration](advanced-configuration.md) |
