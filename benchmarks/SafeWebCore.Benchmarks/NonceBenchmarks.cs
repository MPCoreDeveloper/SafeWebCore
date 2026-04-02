using BenchmarkDotNet.Attributes;

namespace SafeWebCore.Benchmarks;

/// <summary>
/// Benchmarks nonce generation throughput and allocation behavior.
/// </summary>
[MemoryDiagnoser]
public class NonceBenchmarks
{
    private readonly NonceService _nonceService = new();

    /// <summary>
    /// Measures standard nonce generation.
    /// </summary>
    /// <returns>The generated nonce.</returns>
    [Benchmark]
    public string GenerateNonce() => _nonceService.GenerateNonce();

    /// <summary>
    /// Measures span-based nonce generation with caller-provided buffer.
    /// </summary>
    /// <returns>The generated nonce string from written span.</returns>
    [Benchmark]
    public string TryWriteNonce()
    {
        Span<char> destination = stackalloc char[NonceService.NonceLength];
        _ = _nonceService.TryWriteNonce(destination, out var charsWritten);
        return new string(destination[..charsWritten]);
    }
}
