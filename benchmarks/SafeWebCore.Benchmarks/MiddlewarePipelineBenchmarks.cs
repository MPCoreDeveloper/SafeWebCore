using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SafeWebCore.Middleware;
using SafeWebCore.Options;

// Alias to avoid ambiguity between SafeWebCore.Options namespace and Microsoft.Extensions.Options.Options class
using ExtOptions = Microsoft.Extensions.Options.Options;

namespace SafeWebCore.Benchmarks;

/// <summary>
/// Benchmarks the end-to-end request overhead introduced by <see cref="NetSecureHeadersMiddleware"/>.
/// Each scenario measures header assembly, nonce generation, and policy resolution in isolation
/// so that regressions in any sub-component show up as a measurable delta.
/// </summary>
/// <remarks>
/// <para>
/// Uses <c>[IterationSetup]</c> to provide a fresh <see cref="DefaultHttpContext"/> per iteration;
/// this setup cost is excluded from the measured time by BenchmarkDotNet.
/// </para>
/// <para>
/// The <c>next</c> delegate is a no-op (<c>Task.CompletedTask</c>) so that only the middleware
/// work itself is timed.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class MiddlewarePipelineBenchmarks
{
    private static readonly RequestDelegate NoOpNext = _ => Task.CompletedTask;

    private NetSecureHeadersMiddleware _defaultMiddleware = null!;
    private NetSecureHeadersMiddleware _pathPoliciesMiddleware = null!;
    private NetSecureHeadersMiddleware _reportOnlyMiddleware = null!;

    private DefaultHttpContext _context = null!;

    /// <summary>
    /// Creates middleware instances once. Each variant uses a different options profile to
    /// reflect a realistic deployment scenario.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        var nonceService = new NonceService();

        // Default: strict A+ options with all headers enabled
        _defaultMiddleware = new NetSecureHeadersMiddleware(
            nonceService,
            ExtOptions.Create(new NetSecureHeadersOptions()),
            new SafeWebCore.Infrastructure.SecurityEventDispatcher([]),
            new SafeWebCore.Infrastructure.SafeWebCoreMetrics());

        // Path policies: adds three prefix policies so the path-matching loop runs
        var withPathPolicies = new NetSecureHeadersOptions();
        withPathPolicies.PathPolicies.Add(new PathPolicyOptions
        {
            PathPrefix = "/api",
            Options = new NetSecureHeadersOptions { EnableCoep = false, EnableCoop = false }
        });
        withPathPolicies.PathPolicies.Add(new PathPolicyOptions
        {
            PathPrefix = "/public",
            Options = new NetSecureHeadersOptions { EnableCsp = false }
        });
        withPathPolicies.PathPolicies.Add(new PathPolicyOptions
        {
            PathPrefix = "/admin",
            Options = new NetSecureHeadersOptions { EnableHsts = false }
        });
        _pathPoliciesMiddleware = new NetSecureHeadersMiddleware(
            nonceService,
            ExtOptions.Create(withPathPolicies),
            new SafeWebCore.Infrastructure.SecurityEventDispatcher([]),
            new SafeWebCore.Infrastructure.SafeWebCoreMetrics());

        // Report-only: CSP sent via Content-Security-Policy-Report-Only
        var reportOnly = new NetSecureHeadersOptions { UseCspReportOnly = true };
        _reportOnlyMiddleware = new NetSecureHeadersMiddleware(
            nonceService,
            ExtOptions.Create(reportOnly),
            new SafeWebCore.Infrastructure.SecurityEventDispatcher([]),
            new SafeWebCore.Infrastructure.SafeWebCoreMetrics());
    }

    /// <summary>
    /// Creates a fresh <see cref="DefaultHttpContext"/> before each measured iteration so that
    /// accumulated response-started callbacks from prior iterations do not skew timing.
    /// </summary>
    [IterationSetup]
    public void IterationSetup() => _context = new DefaultHttpContext();

    /// <summary>
    /// Baseline: full middleware pass with default strict A+ options, no path policies.
    /// </summary>
    [Benchmark(Baseline = true)]
    public Task DefaultOptions() => _defaultMiddleware.InvokeAsync(_context, NoOpNext);

    /// <summary>
    /// Path policy matching: middleware must walk a 3-entry prefix table per request.
    /// The request path (<c>/home/index</c>) intentionally matches no policy so the fallback
    /// default options are used — isolating purely the matching overhead.
    /// </summary>
    [Benchmark]
    public Task WithPathPolicies()
    {
        _context.Request.Path = "/home/index";
        return _pathPoliciesMiddleware.InvokeAsync(_context, NoOpNext);
    }

    /// <summary>
    /// Path policy match hit: request path matches the <c>/api</c> prefix, exercising
    /// the policy-override code path including COEP/COOP suppression.
    /// </summary>
    [Benchmark]
    public Task WithPathPolicyHit()
    {
        _context.Request.Path = "/api/users";
        return _pathPoliciesMiddleware.InvokeAsync(_context, NoOpNext);
    }

    /// <summary>
    /// Report-only CSP: the header is written as <c>Content-Security-Policy-Report-Only</c>
    /// instead of <c>Content-Security-Policy</c>; measures any branching/string overhead.
    /// </summary>
    [Benchmark]
    public Task ReportOnlyCsp() => _reportOnlyMiddleware.InvokeAsync(_context, NoOpNext);
}
