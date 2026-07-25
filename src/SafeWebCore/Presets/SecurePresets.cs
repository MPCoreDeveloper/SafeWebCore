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
    ///   <item><c>Permissions-Policy</c> — all recognized features denied (modern Chromium tokens only)</item>
    ///   <item><c>Cross-Origin-Embedder-Policy</c> — require-corp</item>
    ///   <item><c>Cross-Origin-Opener-Policy</c> — same-origin</item>
    ///   <item><c>Cross-Origin-Resource-Policy</c> — same-origin</item>
    ///   <item><c>X-DNS-Prefetch-Control</c> — off</item>
    ///   <item><c>X-Permitted-Cross-Domain-Policies</c> — none</item>
    ///   <item><c>Server</c> header — removed</item>
    ///   <item><c>X-Powered-By</c> header — removed</item>
    ///   <item><c>Content-Security-Policy</c> — nonce-based, strict-dynamic, Trusted Types</item>
    /// </list>
    /// </summary>
    /// <returns>A fully configured <see cref="NetSecureHeadersOptions"/> with the strictest A+ settings.</returns>
    public static NetSecureHeadersOptions StrictAPlus()
    {
        // All browser features denied for maximum security.
        // Tokens are limited to features currently recognized by Chromium-based browsers (no invalid directives).
        // Removed stale/invalid tokens that trigger scanner warnings:
        //   ambient-light-sensor, battery, cross-origin-isolated, document-domain,
        //   execution-while-not-rendered, execution-while-out-of-viewport,
        //   navigation-override, sync-xhr,
        //   identity-credentials-get, otp-credentials, publickey-credentials-create, window-management
        // Kept modern valid tokens (2022–2024): clipboard-read, clipboard-write, local-fonts
        string[] deniedPermissions =
        [
            "accelerometer=()",
            "autoplay=()",
            "camera=()",
            "clipboard-read=()",
            "clipboard-write=()",
            "display-capture=()",
            "encrypted-media=()",
            "fullscreen=()",
            "geolocation=()",
            "gyroscope=()",
            "hid=()",
            "idle-detection=()",
            "local-fonts=()",
            "magnetometer=()",
            "microphone=()",
            "midi=()",
            "payment=()",
            "picture-in-picture=()",
            "publickey-credentials-get=()",
            "screen-wake-lock=()",
            "serial=()",
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
            RemoveXPoweredBy = true,

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
    /// Returns a minimal preset for API endpoints.
    /// Emits only API-relevant hardening headers and disables browser document headers
    /// that add little value for JSON or non-HTML responses.
    /// </summary>
    /// <returns>A configured <see cref="NetSecureHeadersOptions"/> for API paths.</returns>
    public static NetSecureHeadersOptions ApiMinimal()
    {
        return new NetSecureHeadersOptions
        {
            EnableHsts = true,
            HstsValue = "max-age=63072000; includeSubDomains; preload",
            EnableXContentTypeOptions = true,
            XContentTypeOptionsValue = "nosniff",
            EnableReferrerPolicy = true,
            ReferrerPolicyValue = "no-referrer",
            EnableXFrameOptions = false,
            EnablePermissionsPolicy = false,
            EnableCoep = false,
            EnableCoop = false,
            EnableCorp = false,
            EnableXDnsPrefetchControl = false,
            EnableXPermittedCrossDomainPolicies = false,
            EnableCsp = false,
            UseCspReportOnly = false,
            RemoveServerHeader = true,
            RemoveXPoweredBy = true,
            CustomPolicies = []
        };
    }

    /// <summary>
    /// Creates a path policy with API-minimal headers for a specific path prefix.
    /// Useful when serving UI pages and APIs from the same host.
    /// </summary>
    /// <param name="pathPrefix">Path prefix to match (for example <c>/api</c>).</param>
    /// <param name="customize">Optional action to override values in the API-minimal preset.</param>
    /// <returns>A configured <see cref="PathPolicyOptions"/> instance for <see cref="NetSecureHeadersOptions.PathPolicies"/>.</returns>
    public static PathPolicyOptions ApiPath(string pathPrefix = "/api", Action<NetSecureHeadersOptions>? customize = null)
    {
        var options = ApiMinimal();
        customize?.Invoke(options);

        return new PathPolicyOptions
        {
            PathPrefix = pathPrefix,
            Options = options
        };
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
            ConnectSrc = "'self' wss: ws:",
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
    /// Returns a Blazor-focused preset with explicit strong support for WebSockets and SignalR.
    /// Use this when your Blazor application relies heavily on real-time WebSocket connections.
    /// </summary>
    public static NetSecureHeadersOptions BlazorWebSocket()
    {
        var options = Blazor();
        // Explicitly ensure both secure and non-secure WebSocket schemes are allowed
        // (ws: is often needed during initial upgrade before wss:)
        options.Csp = options.Csp with
        {
            ConnectSrc = "'self' wss: ws:"
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

    /// <summary>
    /// Returns a preset optimized for applications running behind a reverse proxy (YARP, nginx, Azure Front Door, etc.).
    /// This preset is slightly more permissive on connect sources to accommodate proxied APIs and WebSocket upgrades
    /// while keeping strong security headers.
    /// </summary>
    public static NetSecureHeadersOptions ReverseProxy()
    {
        var options = CreateFromStrictAPlus();
        options.ReferrerPolicyValue = "strict-origin-when-cross-origin";

        options.Csp = new CspOptions
        {
            DefaultSrc = "'none'",
            ScriptSrc = "'self' 'nonce-{nonce}' 'strict-dynamic' https:",
            StyleSrc = "'self' 'nonce-{nonce}'",
            ImgSrc = "'self' https: data:",
            FontSrc = "'self' https:",
            ConnectSrc = "'self' https: wss:",
            MediaSrc = "'self' https:",
            ObjectSrc = "'none'",
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
    /// Returns a preset suitable for applications that expose Swagger / OpenAPI UI.
    /// Swagger UI often requires specific CDN sources and some inline styles.
    /// Other security headers remain strong.
    /// </summary>
    public static NetSecureHeadersOptions Swagger()
    {
        var options = CreateFromStrictAPlus();
        options.ReferrerPolicyValue = "strict-origin-when-cross-origin";

        options.Csp = new CspOptions
        {
            DefaultSrc = "'none'",
            // Swagger UI frequently needs unsafe-inline for styles and loads assets from jsdelivr
            ScriptSrc = "'self' 'nonce-{nonce}' 'strict-dynamic' https://cdn.jsdelivr.net",
            StyleSrc = "'self' 'unsafe-inline' https://cdn.jsdelivr.net",
            ImgSrc = "'self' data: https:",
            FontSrc = "'self' https://cdn.jsdelivr.net",
            ConnectSrc = "'self' https: wss:",
            WorkerSrc = "'self' blob:",
            ObjectSrc = "'none'",
            BaseUri = "'none'",
            FormAction = "'self'",
            FrameAncestors = "'none'",
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
