# Advanced Configuration

This guide covers custom header policies, CSP report-only rollout, path-based configuration, startup validation, CSP violation reporting, endpoint overrides, and troubleshooting.

---

## CSP Report-Only Rollout

Use report-only mode to evaluate new CSP policies before enforcing them in production:

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.UseCspReportOnly = true;
});
```

When enabled, SafeWebCore emits `Content-Security-Policy-Report-Only` instead of `Content-Security-Policy`. This allows you to:
1. Test a stricter policy without breaking functionality
2. Collect violations in production safely
3. Refine directives before enforcement

---

## Path-Based Security Policies

Apply different security headers to specific route prefixes. Useful for:
- Stricter policies for `/admin` routes
- Relaxed policies for `/public` routes
- Different headers for `/api` vs web pages

```csharp
using SafeWebCore.Options;

builder.Services.AddNetSecureHeaders(opts =>
{
    // Base configuration for all routes
    opts.EnableHsts = true;
    opts.RemoveServerHeader = true;

    // Stricter policy for admin
    opts.PathPolicies.Add(new PathPolicyOptions
    {
        PathPrefix = "/admin",
        Options = new NetSecureHeadersOptions
        {
            ReferrerPolicyValue = "no-referrer",
            XFrameOptionsValue = "DENY",
            UseCspReportOnly = false
        }
    });

    // Report-only for experimental routes
    opts.PathPolicies.Add(new PathPolicyOptions
    {
        PathPrefix = "/experimental",
        Options = new NetSecureHeadersOptions
        {
            UseCspReportOnly = true
        }
    });

    // Even stricter for sensitive data
    opts.PathPolicies.Add(new PathPolicyOptions
    {
        PathPrefix = "/admin/users",
        Options = new NetSecureHeadersOptions
        {
            ReferrerPolicyValue = "no-referrer",
            UseCspReportOnly = false,
            EnableCoep = true
        }
    });
});
```

### Matching Behavior

- **Prefix-based matching** — `/admin/users` matches both `/admin` and `/admin/users` paths
- **Longest prefix wins** — If both `/admin` and `/admin/users` are defined, `/admin/users` takes precedence
- **Fall-through to default** — Routes without a matching prefix use the global options

---

## Startup Configuration Validation

SafeWebCore validates options at startup and fails fast. This prevents silent misconfiguration.

### Examples of Invalid Configurations

| Configuration | Error |
|---|---|
| `UseCspReportOnly = true` with `EnableCsp = false` | CSP report-only without CSP enabled |
| Duplicate path prefixes (normalized) | `"/api"` and `"api"` both registered |
| Empty path policy prefix | `PathPrefix = ""` |
| CSP report endpoint without CSP | `UseCspReport()` but `EnableCsp = false` |

### Validation at Startup

If you have an invalid configuration, the application **fails immediately** with a clear error message:

```
OptionsValidationException: SafeWebCore options validation failed:
- UseCspReportOnly cannot be true when EnableCsp is false
```

This is better than silent failures or runtime errors in production.

---

## Razor Nonce TagHelpers

SafeWebCore includes TagHelpers that automatically inject the per-request CSP nonce into `<script>` and `<style>` elements.

### Registration

Add this to `Views/_ViewImports.cshtml`:

```razor
@addTagHelper *, SafeWebCore
```

### Usage

After registration, nonce is injected **automatically** on any `<script>` or `<style>` tag:

```html
<!-- Nonce will be added automatically -->
<script>
    console.log('This script will be allowed by CSP!');
</script>

<style>
    body { font-family: sans-serif; }
</style>

<!-- Works with src attributes too -->
<script src="/js/app.js"></script>
```

### Explicit Nonce (Manual)

If you need the nonce value explicitly, use the `[CspNonce]` controller attribute or `HttpContext.GetCspNonce()`:

```html
<!-- Manual nonce attribute — TagHelper won't add it again -->
<script nonce="@ViewData["CspNonce"]">
    // Your script
</script>

<!-- If nonce already present, TagHelper leaves it unchanged -->
<script nonce="manually-set">
    // Won't get overridden
</script>
```

---

## Endpoint-Level Overrides

Skip security headers or switch to report-only mode for specific endpoints.

### Minimal APIs

```csharp
using SafeWebCore.Extensions;
using SafeWebCore.Metadata;

// Skip headers entirely (e.g., health probes)
app.MapGet("/health", () => Results.Ok("healthy"))
   .SkipNetSecureHeaders();

// Use report-only CSP instead of enforce (e.g., legacy endpoints)
app.MapGet("/legacy", () => Results.Ok("legacy"))
   .WithCspMode(CspEndpointMode.ReportOnly);
```

### MVC / Controllers

```csharp
using SafeWebCore.Attributes;

// Skip headers entirely
[SkipNetSecureHeaders]
public sealed class HealthController : Controller
{
    public IActionResult Ping() => Ok();
}

// Use report-only CSP
[CspMode(CspEndpointMode.ReportOnly)]
public sealed class BetaFeaturesController : Controller
{
    public IActionResult Index() => View();
}

// Mixed: one method skips, others use defaults
public sealed class AdminController : Controller
{
    [SkipNetSecureHeaders]
    public IActionResult Health() => Ok();

    public IActionResult Dashboard() => View();  // Uses default headers
}
```

> ⚠️ For endpoint metadata to work, `UseNetSecureHeaders()` must be placed **after** `UseRouting()` in your pipeline.

---

## CSP Violation Reporting *(v1.1.0+)*

### Setting Up the Report Endpoint

SafeWebCore includes built-in middleware to handle CSP violation reports:

```csharp
var app = builder.Build();

app.UseCspReport();              // Must be before UseNetSecureHeaders
app.UseNetSecureHeaders();

app.MapControllers();
app.Run();
```

This registers a `POST /csp-report` endpoint that:
1. Parses JSON CSP violation payloads
2. Validates the report structure
3. Invokes all registered `ICspReportSink` implementations
4. Returns `204 No Content` for valid reports or `400 Bad Request` for invalid ones

### Configuring CSP to Send Reports

Use the Reporting API v1 (recommended):

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.Csp = opts.Csp with { ReportTo = "default" };
});
```

Also configure the `Reporting-Endpoints` header:

```csharp
using SafeWebCore.Abstractions;
using Microsoft.AspNetCore.Http;

public sealed class ReportingEndpointsPolicy : IHeaderPolicy
{
    public void Apply(HttpResponse response)
    {
        response.Headers["Reporting-Endpoints"] = """default="/csp-report" """.Trim();
    }
}

builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.Csp = opts.Csp with { ReportTo = "default" };
    opts.CustomPolicies.Add(new ReportingEndpointsPolicy());
});
```

### Custom Sinks

Implement `ICspReportSink` to forward violations to your monitoring system (SIEM, logging, analytics, etc.):

```csharp
using SafeWebCore.Abstractions;
using SafeWebCore.Models;

public sealed class SecurityAnalyticsSink : ICspReportSink
{
    private readonly ILogger<SecurityAnalyticsSink> _logger;

    public SecurityAnalyticsSink(ILogger<SecurityAnalyticsSink> logger)
    {
        _logger = logger;
    }

    public Task WriteAsync(CspViolationReport report, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "CSP Violation: {Directive} blocked {BlockedUri} on {DocumentUri}",
            report.ViolatedDirective,
            report.BlockedUri,
            report.DocumentUri);

        // Forward to external SIEM/analytics
        // return ForwardToSiemAsync(report, cancellationToken);

        return Task.CompletedTask;
    }
}

builder.Services.AddSingleton<ICspReportSink, SecurityAnalyticsSink>();
```

Multiple sinks can be registered — each is invoked for every valid report:

```csharp
builder.Services.AddSingleton<ICspReportSink, SecurityAnalyticsSink>();
builder.Services.AddSingleton<ICspReportSink, SiemSink>();
builder.Services.AddSingleton<ICspReportSink, DatabaseSink>();
```

> 💡 A default logging sink remains active; custom sinks are **in addition** to logging.

### Report Structure

SafeWebCore parses reports into a strongly-typed `CspViolationReport`:

```csharp
public sealed record CspViolationReport
{
    public string DocumentUri { get; init; }
    public string? ViolatedDirective { get; init; }
    public string? EffectiveDirective { get; init; }
    public string? BlockedUri { get; init; }
    public int StatusCode { get; init; }
    public string? SourceFile { get; init; }
    public int? LineNumber { get; init; }
    public int? ColumnNumber { get; init; }
    public string? OriginalPolicy { get; init; }
    public string? Disposition { get; init; }
}
```

---

## Custom Header Policies

Implement `IHeaderPolicy` to add or override headers:

```csharp
using SafeWebCore.Abstractions;
using Microsoft.AspNetCore.Http;

public sealed class CacheControlPolicy : IHeaderPolicy
{
    public void Apply(HttpResponse response)
    {
        response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        response.Headers["Pragma"] = "no-cache";
        response.Headers["Expires"] = "0";
    }
}

public sealed class CustomSecurityPolicy : IHeaderPolicy
{
    public void Apply(HttpResponse response)
    {
        response.Headers["X-Custom-Security"] = "enabled";
    }
}
```

Register them:

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.CustomPolicies.Add(new CacheControlPolicy());
    opts.CustomPolicies.Add(new CustomSecurityPolicy());
});
```

Custom policies run **after** all built-in SafeWebCore headers, allowing you to:
- Add headers not supported by SafeWebCore
- Override built-in header values
- Add dynamic header logic per request

---

## Disabling Specific Headers

Every built-in header can be individually disabled:

```csharp
builder.Services.AddNetSecureHeaders(opts =>
{
    opts.EnableHsts = false;                      // Behind reverse proxy that adds HSTS
    opts.EnableXFrameOptions = false;             // Using frame-ancestors in CSP
    opts.EnableCoep = false;                      // Third-party embeds need it off
    opts.RemoveServerHeader = false;              // Monitoring needs Server header
    opts.EnableCsp = false;                       // Managed by WAF instead
    opts.EnablePermissionsPolicy = false;         // Not relevant for your app
});
```

---

## Middleware Pipeline Order

Place `UseNetSecureHeaders()` strategically:

```csharp
var app = builder.Build();

// 1. Exception handling (should be first)
app.UseExceptionHandler("/error");

// 2. CSP violation reporting
app.UseCspReport();

// 3. Security headers (must be before content generation)
app.UseNetSecureHeaders();

// 4. Standard middleware
app.UseHttpsRedirection();
app.UseStaticFiles();              // Important: static files must be after UseNetSecureHeaders
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 5. Endpoints
app.MapControllers();

app.Run();
```

**Critical:** `UseNetSecureHeaders()` **must be before** `UseStaticFiles()` to ensure static files (CSS, JS, images) also receive security headers.

---

## Per-Endpoint CSP Nonce Access

### Minimal APIs

```csharp
using SafeWebCore.Extensions;

app.MapGet("/", (HttpContext ctx) =>
{
    var nonce = ctx.GetCspNonce();
    return Results.Content($"""
        <html>
        <body>
            <h1>Secure Page</h1>
            <script nonce="{nonce}">console.log('CSP-compliant');</script>
        </body>
        </html>
        """, "text/html");
});
```

### MVC Controllers

```csharp
using SafeWebCore.Attributes;
using SafeWebCore.Extensions;

[CspNonce]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        // Nonce is automatically in ViewData["CspNonce"]
        return View();
    }

    public IActionResult DataTable()
    {
        // Access directly if needed
        var nonce = HttpContext.GetCspNonce();
        return Json(new { nonce });
    }
}
```

### Razor Pages

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
    console.log('Inline script with CSP nonce');
</script>
```

---

## Testing Security Headers

Use `Microsoft.AspNetCore.TestHost` for integration tests:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using SafeWebCore.Extensions;
using Xunit;

public class SecurityHeadersTests
{
    private readonly HttpClient _client;
    private readonly IHost _host;

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
    public async Task GetRequestContainsAllSecurityHeaders()
    {
        var response = await _client.GetAsync("/");
        
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
    }

    [Fact]
    public async Task CspHeaderContainsNonce()
    {
        var response = await _client.GetAsync("/");
        
        var cspHeader = response.Content.Headers.FirstOrDefault(h => h.Key == "Content-Security-Policy").Value?.FirstOrDefault();
        Assert.NotNull(cspHeader);
        Assert.Matches(@"'nonce-[\w\+\/]+=*'", cspHeader);
    }
}
```

---

## Troubleshooting

| Problem | Cause | Fix |
|---------|-------|-----|
| **Inline scripts blocked** | Missing nonce on `<script>` | Add `nonce="@ViewData["CspNonce"]"` or use TagHelpers |
| **Styles not loading** | Missing nonce on `<style>` | Add `nonce="@ViewData["CspNonce"]`
