# Benchmarks

SafeWebCore ships a [BenchmarkDotNet](https://benchmarkdotnet.org/) project (`benchmarks/SafeWebCore.Benchmarks`) that covers all performance-critical components. Run benchmarks to understand the per-request overhead before deploying, or when profiling a regression.

## Benchmark suites

| File | What it measures |
|---|---|
| `NonceBenchmarks` | Nonce generation throughput: heap-allocating (`GenerateNonce`) vs. zero-alloc stack span (`TryWriteNonce`) |
| `CspBuildBenchmarks` | CSP header value assembly: strict default policy vs. a broader, asset-heavy policy |
| `PolicyBuilderBenchmarks` | Typed builder output: `PermissionsPolicyBuilder`, `ReferrerPolicyBuilder`, `CrossOriginPolicyBuilder` |
| `PresetBenchmarks` | Instantiation cost of every application-profile preset (`StrictAPlus`, `Api`, `Mvc`, `Blazor`, `SpaReverseProxy`) |
| `MiddlewarePipelineBenchmarks` | End-to-end `NetSecureHeadersMiddleware.InvokeAsync` overhead across four scenarios |
| `CspReportParseBenchmarks` | CSP report JSON parsing pipeline: minimal, full, and invalid-payload paths |

## Running benchmarks

BenchmarkDotNet **requires a Release build** to produce meaningful results. Debug builds are rejected with an error message.

```bash
cd benchmarks/SafeWebCore.Benchmarks
dotnet run -c Release
```

This launches an interactive switcher. Type the suite number and press Enter, or pass `--filter` to run specific suites non-interactively:

```bash
# Run a single suite
dotnet run -c Release -- --filter "*Nonce*"

# Run all suites
dotnet run -c Release -- --filter "*"

# Run multiple suites
dotnet run -c Release -- --filter "*Middleware*" --filter "*Preset*"
```

Results are written to `BenchmarkDotNet.Artifacts/results/` in Markdown, HTML, and CSV formats.

## Middleware pipeline scenarios

`MiddlewarePipelineBenchmarks` uses a no-op `RequestDelegate` (`Task.CompletedTask`) and a fresh `DefaultHttpContext` per iteration so that only the middleware work itself is timed.

| Benchmark | Description |
|---|---|
| `DefaultOptions` *(baseline)* | Full pass with default strict-A+ options; no path policies |
| `WithPathPolicies` | Three path prefixes configured; request misses all prefixes (tests matching overhead) |
| `WithPathPolicyHit` | Request hits the `/api` prefix — measures policy-override branching |
| `ReportOnlyCsp` | CSP emitted as `Content-Security-Policy-Report-Only` instead of enforce mode |

## CSP report parsing scenarios

`CspReportParseBenchmarks` measures the full request-body → JSON parse → `CspViolationReport` mapping pipeline. The `[MemoryDiagnoser]` output shows per-invocation allocations, which makes it easy to detect regressions in the deserialization path.

| Benchmark | Description |
|---|---|
| `ParseMinimalReport` *(baseline)* | Minimal payload with only `violated-directive` and `effective-directive` |
| `ParseFullReport` | Full payload with all optional fields (referrer, source-file, line/column, sample) |
| `ParseInvalidReport` | Truncated/invalid JSON — exercises the rejection and `JsonException` catch path |

## Interpreting results

- **Mean** — average time per operation; the primary metric for latency regressions.
- **Allocated** — heap bytes per operation (from `[MemoryDiagnoser]`); target is 0 B on hot paths.
- **Ratio** — relative to the baseline benchmark in the same class; values > 1.0 mean slower.

The nonce path (`TryWriteNonce`) is intentionally zero-allocation — if `Allocated` rises above 0 B it indicates a regression. The middleware pipeline allocates for the nonce `string` and response-header entries; this is unavoidable and expected.

## Adding new benchmarks

1. Create a class in `benchmarks/SafeWebCore.Benchmarks/` decorated with `[MemoryDiagnoser]`.
2. Mark each scenario with `[Benchmark]` and designate one as `[Benchmark(Baseline = true)]`.
3. Use `[GlobalSetup]` for one-time setup and `[IterationSetup]` for per-iteration reset when the workload consumes state (e.g., streams).
4. Keep benchmark methods as **instance** methods — BenchmarkDotNet instantiates the class and dispatches dynamically.

```csharp
[MemoryDiagnoser]
public class MyComponentBenchmarks
{
    private MyComponent _component = null!;

    [GlobalSetup]
    public void GlobalSetup() => _component = new MyComponent();

    [Benchmark(Baseline = true)]
    public string FastPath() => _component.FastOperation();

    [Benchmark]
    public string SlowPath() => _component.FullOperation();
}
```
