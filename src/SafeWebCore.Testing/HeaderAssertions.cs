using System.Net.Http;
using Xunit;

namespace SafeWebCore.Testing;

/// <summary>
/// Extension methods for asserting common SafeWebCore security headers on HTTP responses.
/// </summary>
public static class HeaderAssertions
{
    /// <summary>
    /// Asserts that the most important security headers are present.
    /// </summary>
    public static void AssertHasSecurityHeaders(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.AssertHeaderExists("Strict-Transport-Security");
        response.AssertHeaderExists("X-Frame-Options");
        response.AssertHeaderExists("X-Content-Type-Options");
        response.AssertHeaderExists("Referrer-Policy");
        response.AssertHeaderExists("Permissions-Policy");
        response.AssertHeaderExists("Cross-Origin-Embedder-Policy");
        response.AssertHeaderExists("Cross-Origin-Opener-Policy");
        response.AssertHeaderExists("Cross-Origin-Resource-Policy");
        response.AssertHeaderExists("Content-Security-Policy");
    }

    /// <summary>
    /// Asserts that a specific header exists.
    /// </summary>
    public static void AssertHeaderExists(this HttpResponseMessage response, string headerName)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(headerName);

        bool exists = response.Headers.Contains(headerName) || response.Content.Headers.Contains(headerName);

        Assert.True(exists, $"Expected header '{headerName}' to be present, but it was not found.");
    }

    /// <summary>
    /// Asserts that the response contains Content-Security-Policy in enforce mode.
    /// </summary>
    public static void AssertHasCspEnforceMode(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        bool hasEnforce = response.Headers.Contains("Content-Security-Policy");
        bool hasReportOnly = response.Headers.Contains("Content-Security-Policy-Report-Only");

        Assert.True(hasEnforce, "Expected 'Content-Security-Policy' header (enforce mode), but it was not found.");
        Assert.False(hasReportOnly, "Response is in report-only mode, but enforce mode was expected.");
    }

    /// <summary>
    /// Asserts that the response contains Content-Security-Policy-Report-Only.
    /// </summary>
    public static void AssertHasCspReportOnlyMode(this HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        Assert.True(
            response.Headers.Contains("Content-Security-Policy-Report-Only"),
            "Expected 'Content-Security-Policy-Report-Only' header, but it was not found.");
    }
}
