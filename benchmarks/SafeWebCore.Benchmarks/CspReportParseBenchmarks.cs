using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SafeWebCore.Infrastructure;

namespace SafeWebCore.Benchmarks;

/// <summary>
/// Benchmarks the CSP violation report parsing pipeline, from raw HTTP request body
/// through JSON deserialization and sink dispatch.
/// </summary>
/// <remarks>
/// A fresh <see cref="DefaultHttpContext"/> and <see cref="MemoryStream"/> are created per invocation
/// so that the async stream reader always begins at position 0.
/// The <c>[MemoryDiagnoser]</c> captures per-invocation allocation, making this benchmark
/// useful for tracking regressions in the JSON parsing path.
/// </remarks>
[MemoryDiagnoser]
public class CspReportParseBenchmarks
{
    // Minimal report: only the mandatory violated-directive and effective-directive fields
    private static readonly byte[] MinimalReportBytes = Encoding.UTF8.GetBytes("""
        {
          "csp-report": {
            "document-uri": "https://example.com/",
            "violated-directive": "script-src",
            "effective-directive": "script-src",
            "original-policy": "default-src 'none'; script-src 'nonce-abc123'"
          }
        }
        """);

    // Full report: all optional fields populated to simulate a real browser payload
    private static readonly byte[] FullReportBytes = Encoding.UTF8.GetBytes("""
        {
          "csp-report": {
            "document-uri": "https://example.com/page",
            "referrer": "https://referrer.example.com",
            "violated-directive": "script-src 'nonce-abc123'",
            "effective-directive": "script-src",
            "original-policy": "default-src 'none'; script-src 'nonce-abc123' 'strict-dynamic'",
            "blocked-uri": "https://cdn.example.com/evil.js",
            "disposition": "enforce",
            "status-code": 200,
            "source-file": "https://example.com/page",
            "line-number": 42,
            "column-number": 15,
            "script-sample": "eval('bad code')"
          }
        }
        """);

    // Invalid payload: malformed JSON to measure the error/rejection path
    private static readonly byte[] InvalidReportBytes = Encoding.UTF8.GetBytes("""
        { "csp-report": { "violated-directive": 
        """);

    private static readonly RequestDelegate NoOpNext = _ => Task.CompletedTask;

    private CspReportMiddleware _middleware = null!;

    /// <summary>Creates the middleware once. No sinks — measures pure parse overhead.</summary>
    [GlobalSetup]
    public void GlobalSetup() =>
        _middleware = new CspReportMiddleware(
            NullLogger<CspReportMiddleware>.Instance,
            [],
            null,  // dispatcher is optional
            null); // metrics is optional (will create internal instance)

    /// <summary>
    /// Baseline: parse and map a minimal CSP report with only the required fields.
    /// </summary>
    [Benchmark(Baseline = true)]
    public Task ParseMinimalReport() => _middleware.InvokeAsync(CreatePostContext(MinimalReportBytes), NoOpNext);

    /// <summary>
    /// Full report: parse and map a CSP report with all optional fields populated.
    /// </summary>
    [Benchmark]
    public Task ParseFullReport() => _middleware.InvokeAsync(CreatePostContext(FullReportBytes), NoOpNext);

    /// <summary>
    /// Rejection path: measures the overhead of detecting and rejecting an invalid JSON body.
    /// </summary>
    [Benchmark]
    public Task ParseInvalidReport() => _middleware.InvokeAsync(CreatePostContext(InvalidReportBytes), NoOpNext);

    // PERF: new MemoryStream(byte[]) wraps without copying; DefaultHttpContext is lightweight
    private static DefaultHttpContext CreatePostContext(byte[] body)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/csp-report";
        ctx.Request.Method = "POST";
        ctx.Request.Body = new MemoryStream(body);
        return ctx;
    }
}
