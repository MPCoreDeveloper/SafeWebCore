using System.Buffers.Text;
using System.Security.Cryptography;

namespace SafeWebCore;

/// <summary>
/// Generates cryptographically secure nonces for CSP headers.
/// Uses <see cref="RandomNumberGenerator"/> with stack-allocated buffers for zero-heap-allocation generation.
/// </summary>
public sealed class NonceService : INonceService
{
    /// <summary>
    /// Size in bytes of the random data used for nonce generation.
    /// 32 bytes provides 256 bits of entropy — far exceeding the CSP Level 3 minimum of 128 bits.
    /// </summary>
    private const int NonceByteLength = 32;

    /// <summary>
    /// Generates a new cryptographically secure nonce.
    /// Uses 32 bytes of random data, base64 encoded via stack-allocated buffers.
    /// </summary>
    /// <returns>A base64-encoded nonce string (44 characters).</returns>
    public string GenerateNonce()
    {
        // PERF: stackalloc avoids heap allocation on the hot path
        Span<byte> randomBytes = stackalloc byte[NonceByteLength];
        RandomNumberGenerator.Fill(randomBytes);

        // Base64 output length for 32 bytes: ceil(32/3)*4 = 44
        Span<char> base64Chars = stackalloc char[44];
        Convert.TryToBase64Chars(randomBytes, base64Chars, out _);

        return new string(base64Chars);
    }
}
