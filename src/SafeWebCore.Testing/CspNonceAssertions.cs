using System.Net.Http;
using Xunit;

namespace SafeWebCore.Testing;

/// <summary>
/// Assertions related to CSP and nonces.
/// </summary>
public static class CspNonceAssertions
{
    /// <summary>
    /// Asserts that the CSP header contains a nonce.
    /// </summary>
    public static void AssertHasNonceInCsp(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var csp = GetCspValue(response);
        Assert.False(string.IsNullOrEmpty(csp), "Expected a CSP header to be present.");
        Assert.Contains("'nonce-", csp!);
    }

    /// <summary>
    /// Asserts that the CSP header does NOT contain a nonce.
    /// </summary>
    public static void AssertHasNoNonceInCsp(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        var csp = GetCspValue(response);
        if (!string.IsNullOrEmpty(csp))
        {
            Assert.DoesNotContain("'nonce-", csp);
        }
    }

    private static string? GetCspValue(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Content-Security-Policy", out var values))
            return string.Join(" ", values);

        if (response.Headers.TryGetValues("Content-Security-Policy-Report-Only", out values))
            return string.Join(" ", values);

        return null;
    }
}
