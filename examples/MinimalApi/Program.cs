using SafeWebCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------
// 1. Add SafeWebCore with the strictest A+ preset.
//    This configures all recommended security headers in one call.
//    Override only what your application actually needs.
// -----------------------------------------------------------------------
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    // Safe rollout: start in report-only mode and inspect violations first.
    opts.UseCspReportOnly = true;

    // Allow images from a CDN in addition to 'self'
    opts.Csp = opts.Csp with
    {
        ImgSrc = "'self' https://picsum.photos data:",
        ConnectSrc = "'self' https://api.example.com",
        ReportTo = "csp-endpoint",
    };

    // First-class Reporting API endpoint mapping for `report-to`.
    opts.ReportingEndpoints.Add(new()
    {
        Group = "csp-endpoint",
        Url = "https://localhost:5001/csp-report"
    });
});

var app = builder.Build();

// -----------------------------------------------------------------------
// 2. Register the security-headers middleware.
//    Place this early so every response gets the headers.
// -----------------------------------------------------------------------
app.UseNetSecureHeaders();

// -----------------------------------------------------------------------
// 3. Register the built-in CSP violation reporting endpoint (/csp-report).
//    Reports are parsed and forwarded to all registered ICspReportSink
//    implementations (default: structured log via CspLoggingReportSink).
// -----------------------------------------------------------------------
app.UseCspReport();

// -----------------------------------------------------------------------
// 4. Application endpoints
// -----------------------------------------------------------------------

// Retrieve the per-request nonce via the GetCspNonce() extension method.
app.MapGet("/", (HttpContext ctx) =>
{
    var nonce = ctx.GetCspNonce();

    return Results.Content(
        $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <title>SafeWebCore – Minimal API Example</title>
            <style nonce="{{nonce}}">
                body { font-family: system-ui, sans-serif; max-width: 720px; margin: 2rem auto; }
                code { background: #f0f0f0; padding: 2px 6px; border-radius: 4px; }
            </style>
        </head>
        <body>
            <h1>🛡️ SafeWebCore – Minimal API</h1>
            <p>This page was served with a full set of security headers.</p>
            <p>CSP nonce: <code>{{nonce}}</code></p>
            <p>Open DevTools → Network and inspect the response headers.</p>
            <script nonce="{{nonce}}">
                console.log('CSP nonce:', '{{nonce}}');
            </script>
        </body>
        </html>
        """,
        "text/html");
});

// A simple JSON API endpoint — security headers are still added automatically.
app.MapGet("/api/status", () => new { status = "ok", time = DateTimeOffset.UtcNow })
   .WithName("GetStatus");

// An endpoint that intentionally skips all security headers (e.g. health probes).
app.MapGet("/health", () => Results.Ok(new { healthy = true }))
   .SkipNetSecureHeaders();

app.Run();
