using BenchmarkDotNet.Attributes;
using SafeWebCore.Builder;

namespace SafeWebCore.Benchmarks;

/// <summary>
/// Benchmarks typed policy builder output generation.
/// </summary>
[MemoryDiagnoser]
public class PolicyBuilderBenchmarks
{
    /// <summary>
    /// Measures permissions policy generation from typed features.
    /// </summary>
    /// <returns>The generated permissions policy value.</returns>
    [Benchmark]
    public string BuildPermissionsPolicy() => new PermissionsPolicyBuilder()
        .Disable(PermissionsFeature.Camera)
        .Disable(PermissionsFeature.Microphone)
        .AllowSelf(PermissionsFeature.Geolocation)
        .Allow(PermissionsFeature.Payment, "https://pay.example.com")
        .Build();

    /// <summary>
    /// Measures referrer policy typed value generation.
    /// </summary>
    /// <returns>The generated referrer policy value.</returns>
    [Benchmark]
    public string BuildReferrerPolicy() => new ReferrerPolicyBuilder()
        .StrictOriginWhenCrossOrigin()
        .Build();

    /// <summary>
    /// Measures cross-origin policy tuple generation.
    /// </summary>
    /// <returns>The typed cross-origin policy values.</returns>
    [Benchmark]
    public CrossOriginPolicyValues BuildCrossOriginPolicy() => new CrossOriginPolicyBuilder()
        .CoepRequireCorp()
        .CoopSameOrigin()
        .CorpSameOrigin()
        .Build();
}
