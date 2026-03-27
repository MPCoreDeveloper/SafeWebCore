namespace SafeWebCore;

/// <summary>
/// Main entry point for SafeWebCore library.
/// </summary>
public static class NetSecureHeaders
{
    /// <summary>
    /// The key used to store the CSP nonce in HttpContext.Items.
    /// </summary>
    public const string CspNonceKey = "CspNonce";
}
