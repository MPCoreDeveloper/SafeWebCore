namespace SafeWebCore.Metadata;

/// <summary>
/// Endpoint-level override mode for CSP header emission.
/// </summary>
public enum CspEndpointMode
{
    /// <summary>
    /// Emit enforce-mode <c>Content-Security-Policy</c>.
    /// </summary>
    Enforce,

    /// <summary>
    /// Emit <c>Content-Security-Policy-Report-Only</c>.
    /// </summary>
    ReportOnly
}
