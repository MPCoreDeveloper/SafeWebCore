using SafeWebCore.Options;

namespace SafeWebCore.Presets;

/// <summary>
/// Pre-configured security presets targeting top scores on securityheaders.com
/// and Google CSP Evaluator. Use these as a starting point and customize as needed.
/// </summary>
public static class SecurePresets
{
    /// <summary>
    /// Returns the strictest possible <see cref="NetSecureHeadersOptions"/> targeting
    /// an <b>A+</b> rating on <c>securityheaders.com</c> and a passing grade on
    /// <c>Google CSP Evaluator</c>.
    /// <para>
    /// This preset locks down <em>everything</em> by default. You will likely need to
    /// relax individual directives (e.g. <c>connect-src</c>, <c>img-src</c>) depending
    /// on your application's requirements.
    /// </para>
    /// <para><b>Headers configured:</b></para>
    /// <list type="bullet">
    ///   <item><c>Strict-Transport-Security</c> — 2 years, includeSubDomains, preload</item>
    ///   <item><c>X-Frame-Options</c> — DENY</item>
    ///   <item><c>X-Content-Type-Options</c> — nosniff</item>
    ///   <item><c>Referrer-Policy</c> — no-referrer (strictest)</item>
    ///   <item><c>Permissions-Policy</c> — all features denied</item>
    ///   <item><c>Cross-Origin-Embedder-Policy</c> — require-corp</item>
    ///   <item><c>Cross-Origin-Opener-Policy</c> — same-origin</item>
    ///   <item><c>Cross-Origin-Resource-Policy</c> — same-origin</item>
    ///   <item><c>X-DNS-Prefetch-Control</c> — off</item>
    ///   <item><c>X-Permitted-Cross-Domain-Policies</c> — none</item>
    ///   <item><c>Server</c> header — removed</item>
    ///   <item><c>Content-Security-Policy</c> — nonce-based, strict-dynamic, Trusted Types</item>
    /// </list>
    /// </summary>
    /// <returns>A fully configured <see cref="NetSecureHeadersOptions"/> with the strictest A+ settings.</returns>
    public static NetSecureHeadersOptions StrictAPlus() => new()
    {
        // ── Transport security ─────────────────────────────────────────
        EnableHsts = true,
        HstsValue = "max-age=63072000; includeSubDomains; preload", // 2 years

        // ── Framing protection ─────────────────────────────────────────
        EnableXFrameOptions = true,
        XFrameOptionsValue = "DENY",

        // ── MIME-type sniffing ─────────────────────────────────────────
        EnableXContentTypeOptions = true,
        XContentTypeOptionsValue = "nosniff",

        // ── Referrer — strictest: never send referrer ──────────────────
        EnableReferrerPolicy = true,
        ReferrerPolicyValue = "no-referrer",

        // ── Permissions — deny ALL browser features ────────────────────
        EnablePermissionsPolicy = true,
        PermissionsPolicyValue = string.Join(", ", [
            "accelerometer=()",
            "ambient-light-sensor=()",
            "autoplay=()",
            "battery=()",
            "camera=()",
            "cross-origin-isolated=()",
            "display-capture=()",
            "document-domain=()",
            "encrypted-media=()",
            "execution-while-not-rendered=()",
            "execution-while-out-of-viewport=()",
            "fullscreen=()",
            "geolocation=()",
            "gyroscope=()",
            "hid=()",
            "idle-detection=()",
            "magnetometer=()",
            "microphone=()",
            "midi=()",
            "navigation-override=()",
            "payment=()",
            "picture-in-picture=()",
            "publickey-credentials-get=()",
            "screen-wake-lock=()",
            "serial=()",
            "sync-xhr=()",
            "usb=()",
            "web-share=()",
            "xr-spatial-tracking=()"
        ]),

        // ── Cross-Origin isolation ─────────────────────────────────────
        EnableCoep = true,
        CoepValue = "require-corp",

        EnableCoop = true,
        CoopValue = "same-origin",

        EnableCorp = true,
        CorpValue = "same-origin",

        // ── DNS prefetch control ───────────────────────────────────────
        EnableXDnsPrefetchControl = true,
        XDnsPrefetchControlValue = "off",

        // ── Cross-domain policies ──────────────────────────────────────
        EnableXPermittedCrossDomainPolicies = true,
        XPermittedCrossDomainPoliciesValue = "none",

        // ── Server identity ────────────────────────────────────────────
        RemoveServerHeader = true,

        // ── Content Security Policy — maximum lockdown ─────────────────
        EnableCsp = true,
        Csp = new CspOptions
        {
            DefaultSrc = "'none'",
            ScriptSrc = "'nonce-{nonce}' 'strict-dynamic'",
            ScriptSrcElem = "",
            ScriptSrcAttr = "",
            StyleSrc = "'nonce-{nonce}'",
            StyleSrcElem = "",
            StyleSrcAttr = "",
            ImgSrc = "'self'",
            FontSrc = "'self'",
            ConnectSrc = "'self'",
            MediaSrc = "",       // inherits 'none' from default-src
            ObjectSrc = "'none'",
            ChildSrc = "'none'",
            WorkerSrc = "'self'",
            ManifestSrc = "'self'",
            FencedFrameSrc = "",
            BaseUri = "'none'",
            Sandbox = "",
            FormAction = "'self'",
            FrameAncestors = "'none'",
            RequireTrustedTypesFor = "'script'",
            TrustedTypes = "'none'",
            ReportTo = "",
            EnableUpgradeInsecureRequests = true,
        },

        // ── No custom policies by default ──────────────────────────────
        CustomPolicies = []
    };
}
