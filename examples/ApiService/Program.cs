using SafeWebCore.Abstractions;
using SafeWebCore.Examples.ApiService.Infrastructure;
using SafeWebCore.Extensions;
using SafeWebCore.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// -----------------------------------------------------------------------
// SafeWebCore: API preset — no CSP (APIs return JSON, not HTML), strict
// transport, CORS, and other headers suited for JSON API services.
// -----------------------------------------------------------------------
builder.Services.AddNetSecureHeadersApiPreset(opts =>
{
    // Optional headers: prevent search engines from indexing internal APIs.
    opts.EnableXRobotsTag = true;
    opts.XRobotsTagValue = "noindex, nofollow";

    // Path-based policy: the /internal prefix gets even stricter headers
    // and skips CSP entirely since it is only consumed by trusted services.
    opts.PathPolicies.Add(new PathPolicyOptions
    {
        PathPrefix = "/internal",
        Options = new NetSecureHeadersOptions
        {
            EnableHsts = true,
            HstsValue = "max-age=63072000; includeSubDomains; preload",
            EnableXContentTypeOptions = true,
            EnableXRobotsTag = true,
            XRobotsTagValue = "noindex, nofollow",
        }
    });
});

// -----------------------------------------------------------------------
// Register a custom ICspReportSink that appends violations to a JSON-lines
// file next to the binary — useful for offline analysis or forwarding to
// a SIEM. The built-in CspLoggingReportSink (structured log) is still
// active alongside it.
// -----------------------------------------------------------------------
builder.Services.AddSingleton<ICspReportSink, JsonFileCspReportSink>();

var app = builder.Build();

app.UseNetSecureHeaders();
app.UseCspReport();

app.MapControllers();
app.Run();
