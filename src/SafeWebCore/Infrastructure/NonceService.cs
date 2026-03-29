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
    /// The length of a generated base64-encoded nonce string.
    /// 32 random bytes → base64: ceil(32/3)*4 = 44 characters.
    /// </summary>
    public const int NonceLength = 44;

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

        Span<char> base64Chars = stackalloc char[NonceLength];
        Convert.TryToBase64Chars(randomBytes, base64Chars, out _);

        return new string(base64Chars);
    }

    /// <summary>
    /// Writes a new cryptographically secure nonce directly into the destination span,
    /// avoiding all heap allocation. Useful for scenarios where the nonce is written
    /// directly into a response buffer or interpolated string handler.
    /// </summary>
    /// <param name="destination">The span to write the base64-encoded nonce into. Must be at least <see cref="NonceLength"/> characters.</param>
    /// <param name="charsWritten">The number of characters written to <paramref name="destination"/>.</param>
    /// <returns><see langword="true"/> if the nonce was written successfully; <see langword="false"/> if <paramref name="destination"/> is too small.</returns>
    public bool TryWriteNonce(Span<char> destination, out int charsWritten)
    {
        if (destination.Length < NonceLength)
        {
            charsWritten = 0;
            return false;
        }

        Span<byte> randomBytes = stackalloc byte[NonceByteLength];
        RandomNumberGenerator.Fill(randomBytes);

        return Convert.TryToBase64Chars(randomBytes, destination, out charsWritten);
    }
}
