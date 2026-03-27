namespace SafeWebCore;

/// <summary>
/// Constants for HTTP security header names emitted by the middleware.
/// </summary>
public static class HeaderNames
{
    /// <summary>Strict-Transport-Security (HSTS).</summary>
    public const string StrictTransportSecurity = "Strict-Transport-Security";

    /// <summary>X-Frame-Options — click-jacking protection.</summary>
    public const string XFrameOptions = "X-Frame-Options";

    /// <summary>X-Content-Type-Options — MIME-sniffing prevention.</summary>
    public const string XContentTypeOptions = "X-Content-Type-Options";

    /// <summary>Referrer-Policy — controls Referer header leakage.</summary>
    public const string ReferrerPolicy = "Referrer-Policy";

    /// <summary>Permissions-Policy — restricts browser features.</summary>
    public const string PermissionsPolicy = "Permissions-Policy";

    /// <summary>Cross-Origin-Embedder-Policy (COEP).</summary>
    public const string CrossOriginEmbedderPolicy = "Cross-Origin-Embedder-Policy";

    /// <summary>Cross-Origin-Opener-Policy (COOP).</summary>
    public const string CrossOriginOpenerPolicy = "Cross-Origin-Opener-Policy";

    /// <summary>Cross-Origin-Resource-Policy (CORP).</summary>
    public const string CrossOriginResourcePolicy = "Cross-Origin-Resource-Policy";

    /// <summary>Content-Security-Policy (CSP).</summary>
    public const string ContentSecurityPolicy = "Content-Security-Policy";

    /// <summary>X-DNS-Prefetch-Control — prevents DNS prefetch leakage.</summary>
    public const string XDnsPrefetchControl = "X-DNS-Prefetch-Control";

    /// <summary>X-Permitted-Cross-Domain-Policies — restricts Adobe Flash / Acrobat cross-domain policy files.</summary>
    public const string XPermittedCrossDomainPolicies = "X-Permitted-Cross-Domain-Policies";

    /// <summary>Server — removed by middleware to hide server technology.</summary>
    public const string Server = "Server";
}
