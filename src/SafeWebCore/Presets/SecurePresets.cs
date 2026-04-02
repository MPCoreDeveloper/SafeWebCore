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
    public static NetSecureHeadersOptions StrictAPlus()
    {
        // All browser features denied for maximum security
        string[] deniedPermissions =
        [
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
        ];

        return new NetSecureHeadersOptions
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
            PermissionsPolicyValue = string.Join(", ", deniedPermissions),

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

    /// <summary>
    /// Returns a profile-oriented preset for API-only applications.
    /// Keeps strong transport and response hardening while disabling CSP by default,
    /// because APIs typically do not render executable HTML.
    /// </summary>
    /// <returns>A configured <see cref="NetSecureHeadersOptions"/> for API workloads.</returns>
    public static NetSecureHeadersOptions Api()
    {
        var options = CreateFromStrictAPlus();
        options.EnableCsp = false;
        options.UseCspReportOnly = false;
        options.ReferrerPolicyValue = "no-referrer";
        options.XFrameOptionsValue = "DENY";
        return options;
    }

    /// <summary>
    /// Returns a profile-oriented preset for MVC applications.
    /// Uses nonce-based CSP with practical defaults for same-origin page assets.
    /// </summary>
    /// <returns>A configured <see cref="NetSecureHeadersOptions"/> for MVC workloads.</returns>
    public static NetSecureHeadersOptions Mvc()
    {
        var options = CreateFromStrictAPlus();
        options.ReferrerPolicyValue = "strict-origin-when-cross-origin";
        options.Csp = new CspOptions
        {
            DefaultSrc = "'none'",
            ScriptSrc = "'nonce-{nonce}' 'strict-dynamic' https:",
            StyleSrc = "'nonce-{nonce}'",
            ImgSrc = "'self' https: data:",
            FontSrc = "'self' https://fonts.gstatic.com",
            ConnectSrc = "'self'",
            ObjectSrc = "'none'",
            ChildSrc = "",
            WorkerSrc = "'self'",
            ManifestSrc = "'self'",
            BaseUri = "'none'",
            FormAction = "'self'",
            FrameAncestors = "'none'",
            RequireTrustedTypesFor = "'script'",
            TrustedTypes = "'none'",
            EnableUpgradeInsecureRequests = true
        };
        return options;
    }

    /// <summary>
    /// Returns a profile-oriented preset for Blazor applications.
    /// Relaxes CSP sources needed for WebAssembly and framework resource loading.
    /// </summary>
    /// <returns>A configured <see cref="NetSecureHeadersOptions"/> for Blazor workloads.</returns>
    public static NetSecureHeadersOptions Blazor()
    {
        var options = CreateFromStrictAPlus();
        options.ReferrerPolicyValue = "strict-origin-when-cross-origin";
        options.Csp = new CspOptions
        {
            DefaultSrc = "'none'",
            ScriptSrc = "'self' 'nonce-{nonce}' 'strict-dynamic' https:",
            StyleSrc = "'self' 'nonce-{nonce}'",
            ImgSrc = "'self' https: data:",
            FontSrc = "'self' https://fonts.gstatic.com data:",
            ConnectSrc = "'self' wss:",
            MediaSrc = "'self' blob:",
            ObjectSrc = "'none'",
            WorkerSrc = "'self' blob:",
            ManifestSrc = "'self'",
            BaseUri = "'none'",
            FormAction = "'self'",
            FrameAncestors = "'none'",
            RequireTrustedTypesFor = "'script'",
            TrustedTypes = "'none'",
            EnableUpgradeInsecureRequests = true
        };
        return options;
    }

    /// <summary>
    /// Returns a profile-oriented preset for SPA reverse-proxy deployments.
    /// Keeps strict isolation while allowing common static asset and API patterns.
    /// </summary>
    /// <returns>A configured <see cref="NetSecureHeadersOptions"/> for SPA reverse-proxy workloads.</returns>
    public static NetSecureHeadersOptions SpaReverseProxy()
    {
        var options = CreateFromStrictAPlus();
        options.ReferrerPolicyValue = "strict-origin-when-cross-origin";
        options.Csp = new CspOptions
        {
            DefaultSrc = "'none'",
            ScriptSrc = "'self' 'nonce-{nonce}' 'strict-dynamic' https:",
            StyleSrc = "'self' 'nonce-{nonce}'",
            ImgSrc = "'self' https: data: blob:",
            FontSrc = "'self' https: data:",
            ConnectSrc = "'self' https: wss:",
            MediaSrc = "'self' https: blob:",
            ObjectSrc = "'none'",
            WorkerSrc = "'self' blob:",
            ManifestSrc = "'self'",
            BaseUri = "'none'",
            FormAction = "'self'",
            FrameAncestors = "'none'",
            RequireTrustedTypesFor = "'script'",
            TrustedTypes = "'none'",
            EnableUpgradeInsecureRequests = true
        };
        return options;
    }

    private static NetSecureHeadersOptions CreateFromStrictAPlus()
    {
        var options = new NetSecureHeadersOptions();
        options.ApplyPreset(StrictAPlus());
        return options;
    }
}
