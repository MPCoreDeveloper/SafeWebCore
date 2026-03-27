namespace SafeWebCore;

/// <summary>
/// Interface for generating cryptographically secure nonces for CSP.
/// </summary>
public interface INonceService
{
    /// <summary>
    /// Generates a new cryptographically secure nonce.
    /// </summary>
    /// <returns>A base64-encoded nonce string.</returns>
    string GenerateNonce();
}
