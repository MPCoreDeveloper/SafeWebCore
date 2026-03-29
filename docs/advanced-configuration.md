# Advanced Configuration

This guide covers custom header policies, CSP violation reporting, per-route configuration, and troubleshooting.

---

## Custom Header Policies

Implement `IHeaderPolicy` to add any header not built into SafeWebCore:

```csharp
using SafeWebCore.Abstractions;
using Microsoft.AspNetCore.Http;

public sealed class CacheControlPolicy : IHeaderPolicy
{
    public void Apply(HttpResponse response)
    {
        response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        response.Headers["Pragma"] = "no-cache";
    }
}
```

Register it:

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.CustomPolicies.Add(new CacheControlPolicy());
});
```

Custom policies run **after** all built-in headers, so you can also override built-in values if needed.

---

## CSP Violation Reporting

### Setting Up the Report Endpoint

SafeWebCore includes `CspReportMiddleware` that handles `POST /csp-report`:

```csharp
var app = builder.Build();

app.UseCspReport();            // Must be before UseNetSecureHeaders
app.UseNetSecureHeaders();

app.Run();
```

### Configuring CSP to Send Reports

#### Reporting API v1 (recommended)

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.Csp = opts.Csp with { ReportTo = "default" };
});
```

You'll also need to add the `Reporting-Endpoints` header. Use a custom policy:

```csharp
public sealed class ReportingEndpointsPolicy : IHeaderPolicy
{
    public void Apply(HttpResponse response)
    {
        response.Headers["Reporting-Endpoints"] = """default="/csp-report" """.Trim();
    }
}
```

#### Violations are logged at Warning level

```
warn: SafeWebCore.Infrastructure.CspReportMiddleware
      CSP Violation Report: {"csp-report":{"document-uri":"https://example.com/","violated-directive":"script-src",...}}
```

### Sending Reports to External Services

Instead of the built-in endpoint, send reports to a service like [report-uri.com](https://report-uri.com):

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.Csp = opts.Csp with { ReportTo = "default" };
});

// Add Reporting-Endpoints header pointing to external service
opts.CustomPolicies.Add(new ReportingEndpointsPolicy("https://your-report-collector.example.com/csp"));
```

---

## Disabling Specific Headers

Every header can be individually disabled:

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.EnableHsts = false;                        // Behind a reverse proxy that adds HSTS
    opts.EnableXFrameOptions = false;               // Using frame-ancestors in CSP instead
    opts.EnableCoep = false;                        // Breaking third-party embeds
    opts.RemoveServerHeader = false;                // Need Server header for monitoring
    opts.EnableCsp = false;                         // Managed by a separate WAF
});
```

---

## Middleware Pipeline Order

The middleware order matters. Place `UseNetSecureHeaders()` early in the pipeline:

```csharp
var app = builder.Build();

// 1. Exception handling (should be first)
app.UseExceptionHandler("/error");

// 2. CSP report endpoint
app.UseCspReport();

// 3. Security headers (before any content is generated)
app.UseNetSecureHeaders();

// 4. Standard middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 5. Endpoints
app.MapControllers();

app.Run();
```

> ⚠️ `UseNetSecureHeaders()` must be before `UseStaticFiles()` to ensure static files also get security headers.

---

## Per-Route CSP Nonce Access

### In Minimal APIs

```csharp
using SafeWebCore.Extensions;

app.MapGet("/", (HttpContext ctx) =>
{
    var nonce = ctx.GetCspNonce();
    return Results.Content($"""
        <html>
        <body>
            <script nonce="{nonce}">console.log('secure');</script>
        </body>
        </html>
        """, "text/html");
});
```

### In MVC Controllers

```csharp
using SafeWebCore.Attributes;

[CspNonce]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        // Nonce is automatically in ViewData["CspNonce"]
        return View();
    }
}
```

### In Razor Pages

```csharp
using SafeWebCore.Extensions;

public class IndexModel : PageModel
{
    public string? CspNonce { get; private set; }

    public void OnGet()
    {
        CspNonce = HttpContext.GetCspNonce();
    }
}
```

```html
<script nonce="@Model.CspNonce">
    // Your inline script
</script>
```

---

## Testing Security Headers

### Automated Test Example

SafeWebCore uses `Microsoft.AspNetCore.TestHost` for integration tests:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using SafeWebCore.Extensions;

public class SecurityHeadersTests : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;

    public SecurityHeadersTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(svc =>
                {
                    svc.AddRouting();
                    svc.AddNetSecureHeadersStrictAPlus();
                });
                web.Configure(app =>
                {
                    app.UseNetSecureHeaders();
                    app.UseRouting();
                    app.UseEndpoints(e => e.MapGet("/", () => "OK"));
                });
            })
            .Start();

        _client = _host.GetTestClient();
    }

    [Fact]
    public async Task ResponseContainsAllSecurityHeaders()
    {
        var response = await _client.GetAsync("/");
        
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

---

## Troubleshooting

### Common Issues

| Problem | Cause | Fix |
|---------|-------|-----|
| Inline scripts blocked | Missing nonce attribute | Add `nonce="@ViewData["CspNonce"]"` to `<script>` tags, or use `HttpContext.GetCspNonce()` |
| Styles not loading | Missing nonce on `<style>` | Add nonce to `<style>` and `<link>` elements |
| Google Fonts blocked | `font-src` too restrictive | Add `https://fonts.gstatic.com` to `font-src` |
| API calls failing | `connect-src` doesn't include API origin | Add your API URL to `connect-src` |
| Third-party images broken | `img-src` is `'self'` only | Add image CDN origins to `img-src` |
| COEP breaking embeds | Cross-origin resources lack CORP headers | Set `EnableCoep = false` or use `credentialless` |
| HSTS issues in dev | HSTS pinned to localhost | Disable HSTS in development: `opts.EnableHsts = false` |

### Reading CSP Violation Reports

Browser DevTools Console shows CSP violations:

```
Refused to execute inline script because it violates the following 
Content Security Policy directive: "script-src 'nonce-abc123' 'strict-dynamic'".
```

This means:
1. You have an inline script without a nonce
2. Fix: Add `nonce="@ViewData["CspNonce"]"` to the script tag

### Development Configuration

In development, you may want a more relaxed configuration:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddNetSecureHeaders(_ => { }); // Secure defaults, less strict
}
else
{
    builder.Services.AddNetSecureHeadersStrictAPlus();
}
```
