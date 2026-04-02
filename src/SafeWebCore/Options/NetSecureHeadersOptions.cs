using SafeWebCore.Abstractions;

namespace SafeWebCore.Options;

/// <summary>
/// Options for configuring the <c>NetSecureHeadersMiddleware</c>.
/// All headers default to secure values that target an A+ rating on securityheaders.com.
/// Disable individual headers by setting their <c>Enable*</c> property to <see langword="false"/>.
/// </summary>
public sealed class NetSecureHeadersOptions
{
    // ── Transport security ─────────────────────────────────────────────────

    /// <summary>Enables <c>Strict-Transport-Security</c> (HSTS). Default: <see langword="true"/>.</summary>
    public bool EnableHsts { get; set; } = true;

    /// <summary>HSTS value. Default: <c>max-age=31536000; includeSubDomains; preload</c>.</summary>
    public string HstsValue { get; set; } = "max-age=31536000; includeSubDomains; preload";

    // ── Framing protection ─────────────────────────────────────────────────

    /// <summary>Enables <c>X-Frame-Options</c>. Default: <see langword="true"/>.</summary>
    public bool EnableXFrameOptions { get; set; } = true;

    /// <summary>X-Frame-Options value. Default: <c>DENY</c>.</summary>
    public string XFrameOptionsValue { get; set; } = "DENY";

    // ── MIME-type sniffing ─────────────────────────────────────────────────

    /// <summary>Enables <c>X-Content-Type-Options</c>. Default: <see langword="true"/>.</summary>
    public bool EnableXContentTypeOptions { get; set; } = true;

    /// <summary>X-Content-Type-Options value. Default: <c>nosniff</c>.</summary>
    public string XContentTypeOptionsValue { get; set; } = "nosniff";

    // ── Referrer ───────────────────────────────────────────────────────────

    /// <summary>Enables <c>Referrer-Policy</c>. Default: <see langword="true"/>.</summary>
    public bool EnableReferrerPolicy { get; set; } = true;

    /// <summary>Referrer-Policy value. Default: <c>strict-origin-when-cross-origin</c>.</summary>
    public string ReferrerPolicyValue { get; set; } = "strict-origin-when-cross-origin";

    // ── Permissions ────────────────────────────────────────────────────────

    /// <summary>Enables <c>Permissions-Policy</c>. Default: <see langword="true"/>.</summary>
    public bool EnablePermissionsPolicy { get; set; } = true;

    /// <summary>Permissions-Policy value. Default disables camera, microphone, and geolocation.</summary>
    public string PermissionsPolicyValue { get; set; } = "camera=(), microphone=(), geolocation=()";

    // ── Cross-Origin isolation ─────────────────────────────────────────────

    /// <summary>Enables <c>Cross-Origin-Embedder-Policy</c> (COEP). Default: <see langword="true"/>.</summary>
    public bool EnableCoep { get; set; } = true;

    /// <summary>COEP value. Default: <c>require-corp</c>.</summary>
    public string CoepValue { get; set; } = "require-corp";

    /// <summary>Enables <c>Cross-Origin-Opener-Policy</c> (COOP). Default: <see langword="true"/>.</summary>
    public bool EnableCoop { get; set; } = true;

    /// <summary>COOP value. Default: <c>same-origin</c>.</summary>
    public string CoopValue { get; set; } = "same-origin";

    /// <summary>Enables <c>Cross-Origin-Resource-Policy</c> (CORP). Default: <see langword="true"/>.</summary>
    public bool EnableCorp { get; set; } = true;

    /// <summary>CORP value. Default: <c>same-origin</c>.</summary>
    public string CorpValue { get; set; } = "same-origin";

    // ── DNS prefetch control ───────────────────────────────────────────────

    /// <summary>Enables <c>X-DNS-Prefetch-Control</c>. Default: <see langword="true"/>.</summary>
    public bool EnableXDnsPrefetchControl { get; set; } = true;

    /// <summary>X-DNS-Prefetch-Control value. Default: <c>off</c> (prevents DNS leak).</summary>
    public string XDnsPrefetchControlValue { get; set; } = "off";

    // ── Cross-domain policies ──────────────────────────────────────────────

    /// <summary>Enables <c>X-Permitted-Cross-Domain-Policies</c>. Default: <see langword="true"/>.</summary>
    public bool EnableXPermittedCrossDomainPolicies { get; set; } = true;

    /// <summary>X-Permitted-Cross-Domain-Policies value. Default: <c>none</c>.</summary>
    public string XPermittedCrossDomainPoliciesValue { get; set; } = "none";

    // ── Optional additional headers ───────────────────────────────────────

    /// <summary>Enables <c>Origin-Agent-Cluster</c>. Default: <see langword="false"/>.</summary>
    public bool EnableOriginAgentCluster { get; set; }

    /// <summary>Origin-Agent-Cluster value. Default: <c>?1</c>.</summary>
    public string OriginAgentClusterValue { get; set; } = "?1";

    /// <summary>Enables <c>X-Robots-Tag</c>. Default: <see langword="false"/>.</summary>
    public bool EnableXRobotsTag { get; set; }

    /// <summary>X-Robots-Tag value. Default: <c>noindex, nofollow</c>.</summary>
    public string XRobotsTagValue { get; set; } = "noindex, nofollow";

    /// <summary>Enables <c>Clear-Site-Data</c>. Default: <see langword="false"/>.</summary>
    public bool EnableClearSiteData { get; set; }

    /// <summary>Clear-Site-Data value. Default: <c>"cache", "cookies", "storage"</c>.</summary>
    public string ClearSiteDataValue { get; set; } = "\"cache\", \"cookies\", \"storage\"";

    // ── Server identity ────────────────────────────────────────────────────

    /// <summary>Removes the <c>Server</c> header to hide server technology. Default: <see langword="true"/>.</summary>
    public bool RemoveServerHeader { get; set; } = true;

    // ── Content Security Policy ────────────────────────────────────────────

    /// <summary>Enables <c>Content-Security-Policy</c>. Default: <see langword="true"/>.</summary>
    public bool EnableCsp { get; set; } = true;

    /// <summary>
    /// Emits CSP in <c>Content-Security-Policy-Report-Only</c> mode instead of enforce mode.
    /// Default: <see langword="false"/>.
    /// </summary>
    public bool UseCspReportOnly { get; set; }

    /// <summary>CSP options. Configure via <see cref="Builder.CspBuilder"/> or assign directly.</summary>
    public CspOptions Csp { get; set; } = new();

    // ── Path-specific policies ──────────────────────────────────────────────

    /// <summary>
    /// Path-specific policy overrides applied when request paths match a configured prefix.
    /// Longest matching prefix wins.
    /// </summary>
    public List<PathPolicyOptions> PathPolicies { get; set; } = [];

    // ── Extensibility ──────────────────────────────────────────────────────

    /// <summary>Custom header policies applied after the built-in headers.</summary>
    public List<IHeaderPolicy> CustomPolicies { get; set; } = [];

    /// <summary>
    /// Copies all property values from the specified preset into this instance.
    /// Used internally by preset-based configuration methods to avoid repetitive property assignments.
    /// </summary>
    /// <param name="preset">The source options to copy values from.</param>
    internal void ApplyPreset(NetSecureHeadersOptions preset)
    {
        EnableHsts = preset.EnableHsts;
        HstsValue = preset.HstsValue;
        EnableXFrameOptions = preset.EnableXFrameOptions;
        XFrameOptionsValue = preset.XFrameOptionsValue;
        EnableXContentTypeOptions = preset.EnableXContentTypeOptions;
        XContentTypeOptionsValue = preset.XContentTypeOptionsValue;
        EnableReferrerPolicy = preset.EnableReferrerPolicy;
        ReferrerPolicyValue = preset.ReferrerPolicyValue;
        EnablePermissionsPolicy = preset.EnablePermissionsPolicy;
        PermissionsPolicyValue = preset.PermissionsPolicyValue;
        EnableCoep = preset.EnableCoep;
        CoepValue = preset.CoepValue;
        EnableCoop = preset.EnableCoop;
        CoopValue = preset.CoopValue;
        EnableCorp = preset.EnableCorp;
        CorpValue = preset.CorpValue;
        EnableXDnsPrefetchControl = preset.EnableXDnsPrefetchControl;
        XDnsPrefetchControlValue = preset.XDnsPrefetchControlValue;
        EnableXPermittedCrossDomainPolicies = preset.EnableXPermittedCrossDomainPolicies;
        XPermittedCrossDomainPoliciesValue = preset.XPermittedCrossDomainPoliciesValue;
        EnableOriginAgentCluster = preset.EnableOriginAgentCluster;
        OriginAgentClusterValue = preset.OriginAgentClusterValue;
        EnableXRobotsTag = preset.EnableXRobotsTag;
        XRobotsTagValue = preset.XRobotsTagValue;
        EnableClearSiteData = preset.EnableClearSiteData;
        ClearSiteDataValue = preset.ClearSiteDataValue;
        RemoveServerHeader = preset.RemoveServerHeader;
        EnableCsp = preset.EnableCsp;
        UseCspReportOnly = preset.UseCspReportOnly;
        Csp = preset.Csp;
        PathPolicies = preset.PathPolicies;
        CustomPolicies = preset.CustomPolicies;
    }
}
