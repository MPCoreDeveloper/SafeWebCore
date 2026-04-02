using BenchmarkDotNet.Attributes;
using SafeWebCore.Options;

namespace SafeWebCore.Benchmarks;

/// <summary>
/// Benchmarks CSP header value generation for common policy profiles.
/// </summary>
[MemoryDiagnoser]
public class CspBuildBenchmarks
{
    private readonly CspOptions _strictPolicy = new();

    private readonly CspOptions _assetHeavyPolicy = new()
    {
        ScriptSrc = "'self' 'nonce-{nonce}' 'strict-dynamic' https:",
        StyleSrc = "'self' 'nonce-{nonce}'",
        ImgSrc = "'self' https: data: blob:",
        FontSrc = "'self' https: data:",
        ConnectSrc = "'self' https: wss:",
        MediaSrc = "'self' https: blob:",
        WorkerSrc = "'self' blob:",
        ReportTo = "default"
    };

    /// <summary>
    /// Measures strict default CSP rendering.
    /// </summary>
    /// <returns>The generated header value.</returns>
    [Benchmark(Baseline = true)]
    public string BuildStrictPolicy() => _strictPolicy.Build();

    /// <summary>
    /// Measures rendering for broader source-list policies.
    /// </summary>
    /// <returns>The generated header value.</returns>
    [Benchmark]
    public string BuildAssetHeavyPolicy() => _assetHeavyPolicy.Build();
}
