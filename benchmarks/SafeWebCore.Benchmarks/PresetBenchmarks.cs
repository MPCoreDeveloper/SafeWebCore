using BenchmarkDotNet.Attributes;
using SafeWebCore.Presets;

namespace SafeWebCore.Benchmarks;

/// <summary>
/// Benchmarks the instantiation cost of every built-in application-profile preset.
/// These benchmarks verify that preset factory methods remain allocation-light and
/// that the cost of copying/cloning options does not grow with new properties.
/// </summary>
[MemoryDiagnoser]
public class PresetBenchmarks
{
    /// <summary>
    /// Measures instantiation of the strict A+ preset (baseline).
    /// </summary>
    /// <returns>The preset options instance.</returns>
    [Benchmark(Baseline = true)]
    public object StrictAPlus() => SecurePresets.StrictAPlus();

    /// <summary>
    /// Measures instantiation of the API-profile preset.
    /// </summary>
    /// <returns>The preset options instance.</returns>
    [Benchmark]
    public object Api() => SecurePresets.Api();

    /// <summary>
    /// Measures instantiation of the MVC-profile preset.
    /// </summary>
    /// <returns>The preset options instance.</returns>
    [Benchmark]
    public object Mvc() => SecurePresets.Mvc();

    /// <summary>
    /// Measures instantiation of the Blazor-profile preset.
    /// </summary>
    /// <returns>The preset options instance.</returns>
    [Benchmark]
    public object Blazor() => SecurePresets.Blazor();

    /// <summary>
    /// Measures instantiation of the SPA reverse-proxy-profile preset.
    /// </summary>
    /// <returns>The preset options instance.</returns>
    [Benchmark]
    public object SpaReverseProxy() => SecurePresets.SpaReverseProxy();
}
