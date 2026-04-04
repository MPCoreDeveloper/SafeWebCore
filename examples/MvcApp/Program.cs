using SafeWebCore.Builder;
using SafeWebCore.Extensions;
using SafeWebCore.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// -----------------------------------------------------------------------
// SafeWebCore: MVC preset with typed builders and path-based policies.
// -----------------------------------------------------------------------
builder.Services.AddNetSecureHeadersMvcPreset(opts =>
{
    // Route CSP reports through Reporting API endpoint groups.
    opts.Csp = opts.Csp with { ReportTo = "csp-endpoint" };

    opts.ReportingEndpoints.Add(new()
    {
        Group = "csp-endpoint",
        Url = "https://localhost:5001/csp-report"
    });

    // Override the Referrer-Policy using the typed builder
    opts.ReferrerPolicyValue = new ReferrerPolicyBuilder()
        .StrictOriginWhenCrossOrigin()
        .Build();

    // Fine-tune the Permissions-Policy with the typed builder
    opts.PermissionsPolicyValue = new PermissionsPolicyBuilder()
        .Disable(PermissionsFeature.Camera)
        .Disable(PermissionsFeature.Microphone)
        .Disable(PermissionsFeature.Payment)
        .AllowSelf(PermissionsFeature.Geolocation)
        .Build();

    // Configure Cross-Origin headers with the typed builder
    var crossOrigin = new CrossOriginPolicyBuilder()
        .CoepRequireCorp()
        .CoopSameOrigin()
        .CorpSameOrigin()
        .Build();

    opts.CoepValue = crossOrigin.Coep;
    opts.CoopValue = crossOrigin.Coop;
    opts.CorpValue = crossOrigin.Corp;

    // Path-based policy: the /public area uses a relaxed CSP in report-only mode
    // while the rest of the application enforces the strict policy.
    opts.PathPolicies.Add(new PathPolicyOptions
    {
        PathPrefix = "/public",
        Options = new NetSecureHeadersOptions
        {
            EnableCsp = true,
            UseCspReportOnly = true,
            ReferrerPolicyValue = "strict-origin-when-cross-origin",
            Csp = opts.Csp with { ReportTo = "csp-endpoint" },
            ReportingEndpoints =
            [
                new()
                {
                    Group = "csp-endpoint",
                    Url = "https://localhost:5001/csp-report"
                }
            ]
        }
    });
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// Register security-headers middleware before controllers.
app.UseNetSecureHeaders();
app.UseCspReport();

app.MapDefaultControllerRoute();
app.Run();
