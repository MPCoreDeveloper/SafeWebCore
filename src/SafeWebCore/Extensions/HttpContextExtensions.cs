using Microsoft.AspNetCore.Http;

namespace SafeWebCore.Extensions;

/// <summary>
/// Extension methods for accessing SafeWebCore features from <see cref="HttpContext"/>.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the CSP nonce generated for the current request by the security headers middleware.
    /// Returns <see langword="null"/> if the middleware has not run or CSP is disabled.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>The base64-encoded nonce string, or <see langword="null"/> if unavailable.</returns>
    /// <example>
    /// <code>
    /// var nonce = HttpContext.GetCspNonce();
    /// &lt;script nonce="@nonce"&gt;console.log('safe');&lt;/script&gt;
    /// </code>
    /// </example>
    public static string? GetCspNonce(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Items[NetSecureHeaders.CspNonceKey] as string;
    }
}
