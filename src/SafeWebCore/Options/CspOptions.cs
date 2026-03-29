namespace SafeWebCore.Options;

using System.Text;

/// <summary>
/// Immutable configuration record for Content Security Policy (CSP) directives.
/// Defaults target an A+ score on securityheaders.com and Google CSP Evaluator.
/// <para>
/// <b>Full CSP Level 3</b> (W3C Recommendation) compliance with forward-looking
/// <b>CSP Level 4</b> support.
/// </para>
/// </summary>
/// <remarks>
/// <para><b>CSP Level 3 directives:</b></para>
/// <list type="bullet">
///   <item><description><b>Fetch:</b> <c>default-src</c>, <c>script-src</c>, <c>style-src</c>, <c>img-src</c>, <c>font-src</c>,
///   <c>connect-src</c>, <c>media-src</c>, <c>object-src</c>, <c>child-src</c>, <c>frame-src</c>,
///   <c>worker-src</c>, <c>manifest-src</c></description></item>
///   <item><description><b>Granular (L3):</b> <c>script-src-elem</c>, <c>script-src-attr</c>, <c>style-src-elem</c>, <c>style-src-attr</c></description></item>
///   <item><description><b>Document:</b> <c>base-uri</c>, <c>sandbox</c></description></item>
///   <item><description><b>Navigation:</b> <c>form-action</c>, <c>frame-ancestors</c></description></item>
///   <item><description><b>Reporting:</b> <c>report-to</c> (L3, replacing <c>report-uri</c>)</description></item>
///   <item><description><b>Transport:</b> <c>upgrade-insecure-requests</c></description></item>
///   <item><description><b>Nonce/hash:</b> <c>'nonce-{nonce}'</c>, <c>'sha256-...'</c>, <c>'sha384-...'</c>, <c>'sha512-...'</c></description></item>
///   <item><description><b>Trust propagation:</b> <c>'strict-dynamic'</c></description></item>
/// </list>
/// <para><b>CSP Level 4 (emerging) directives:</b></para>
/// <list type="bullet">
///   <item><description><b>Trusted Types:</b> <c>require-trusted-types-for</c>, <c>trusted-types</c></description></item>
///   <item><description><b>Privacy Sandbox:</b> <c>fenced-frame-src</c></description></item>
/// </list>
/// <para><b>Deprecated directives</b> (<c>report-uri</c>, <c>block-all-mixed-content</c>) are retained
/// with <c>[Obsolete]</c> attributes for backward compatibility.</para>
/// <para>
/// Use <see cref="SafeWebCore.Builder.CspBuilder"/> for a fluent API, or C# <c>with</c> expressions
/// to modify individual directives from a preset.
/// </para>
/// <para>
/// <b>Validate your policy after deployment:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see href="https://securityheaders.com/">securityheaders.com</see> — Grades all security
///   headers (A+ through F), including CSP, HSTS, Permissions-Policy, and X-Frame-Options.</description></item>
///   <item><description><see href="https://csp-evaluator.withgoogle.com/">Google CSP Evaluator</see> — Analyzes your
///   CSP for misconfigurations such as missing <c>object-src</c>, <c>'unsafe-inline'</c> without nonce, or
///   missing <c>'strict-dynamic'</c>.</description></item>
/// </list>
/// </remarks>
public record CspOptions
{
    // ── Fetch directives ───────────────────────────────────────────────────

    /// <summary>Default fallback for all fetch directives. Default: <c>'none'</c>.</summary>
    public string DefaultSrc { get; init; } = "'none'";

    /// <summary>Restricts script execution. Default: nonce-based with strict-dynamic.</summary>
    public string ScriptSrc { get; init; } = "'nonce-{nonce}' 'strict-dynamic' https:";

    /// <summary>Restricts inline script elements (<c>&lt;script&gt;</c>). Empty = inherits from script-src.</summary>
    public string ScriptSrcElem { get; init; } = "";

    /// <summary>Restricts inline event handlers. Empty = inherits from script-src.</summary>
    public string ScriptSrcAttr { get; init; } = "";

    /// <summary>Restricts stylesheet loading. Default: nonce-based.</summary>
    public string StyleSrc { get; init; } = "'nonce-{nonce}'";

    /// <summary>Restricts <c>&lt;style&gt;</c> elements. Empty = inherits from style-src.</summary>
    public string StyleSrcElem { get; init; } = "";

    /// <summary>Restricts inline style attributes. Empty = inherits from style-src.</summary>
    public string StyleSrcAttr { get; init; } = "";

    /// <summary>Restricts image sources. Default: <c>'self' https: data:</c>.</summary>
    public string ImgSrc { get; init; } = "'self' https: data:";

    /// <summary>Restricts font loading sources. Empty = inherits from default-src.</summary>
    public string FontSrc { get; init; } = "";

    /// <summary>Restricts XHR, WebSocket, fetch(), EventSource. Empty = inherits from default-src.</summary>
    public string ConnectSrc { get; init; } = "";

    /// <summary>Restricts <c>&lt;audio&gt;</c> and <c>&lt;video&gt;</c> sources. Empty = inherits from default-src.</summary>
    public string MediaSrc { get; init; } = "";

    /// <summary>Restricts <c>&lt;object&gt;</c>, <c>&lt;embed&gt;</c>, and <c>&lt;applet&gt;</c> sources. Default: <c>'none'</c>.</summary>
    public string ObjectSrc { get; init; } = "'none'";

    /// <summary>Restricts nested browsing contexts (<c>&lt;frame&gt;</c>, <c>&lt;iframe&gt;</c>). Empty = inherits from default-src.</summary>
    public string ChildSrc { get; init; } = "";

    /// <summary>Restricts <c>&lt;frame&gt;</c> and <c>&lt;iframe&gt;</c> sources (CSP Level 3, split from child-src). Empty = falls back to child-src → default-src.</summary>
    public string FrameSrc { get; init; } = "";

    /// <summary>Restricts <c>Worker</c>, <c>SharedWorker</c>, <c>ServiceWorker</c> sources. Empty = inherits from child-src → default-src.</summary>
    public string WorkerSrc { get; init; } = "";

    /// <summary>Restricts web app manifest sources. Empty = inherits from default-src.</summary>
    public string ManifestSrc { get; init; } = "";

    /// <summary>Restricts <c>&lt;fencedframe&gt;</c> sources (Privacy Sandbox / 2025+). Empty = disabled.</summary>
    public string FencedFrameSrc { get; init; } = "";

    // ── Document directives ────────────────────────────────────────────────

    /// <summary>Restricts <c>&lt;base&gt;</c> URIs. Default: <c>'none'</c>.</summary>
    public string BaseUri { get; init; } = "'none'";

    /// <summary>Applies sandbox restrictions (like <c>&lt;iframe sandbox&gt;</c>). Empty = disabled.</summary>
    public string Sandbox { get; init; } = "";

    // ── Navigation directives ──────────────────────────────────────────────

    /// <summary>Restricts form submission targets. Default: <c>'self'</c>.</summary>
    public string FormAction { get; init; } = "'self'";

    /// <summary>Restricts parents that can embed this page. Default: <c>'none'</c> (replaces X-Frame-Options).</summary>
    public string FrameAncestors { get; init; } = "'none'";

    // ── Trusted Types (CSP Level 3 / 2025+) ────────────────────────────────

    /// <summary>Enforces Trusted Types for DOM XSS sinks. E.g. <c>'script'</c>. Empty = disabled.</summary>
    public string RequireTrustedTypesFor { get; init; } = "";

    /// <summary>Controls which Trusted Type policies are allowed. E.g. <c>'none'</c> or <c>myPolicy</c>. Empty = disabled.</summary>
    public string TrustedTypes { get; init; } = "";

    // ── Reporting ──────────────────────────────────────────────────────────

    /// <summary>Reporting API v1 group name for CSP violations (Reporting-Endpoints header). Empty = disabled.</summary>
    public string ReportTo { get; init; } = "";

    /// <summary>Legacy report-uri endpoint for CSP violations. Empty = disabled. Prefer <see cref="ReportTo"/>.</summary>
    [Obsolete("report-uri is deprecated in CSP Level 3. Use ReportTo with the Reporting API instead.")]
    public string ReportUri { get; init; } = "";

    // ── Transport / mixed-content ──────────────────────────────────────────

    /// <summary>Upgrades HTTP requests to HTTPS. Default: enabled.</summary>
    public bool EnableUpgradeInsecureRequests { get; init; } = true;

    /// <summary>
    /// Blocks mixed content. Default: disabled.
    /// <para><b>Deprecated in CSP Level 3</b> — modern browsers block mixed content by default.
    /// Use <see cref="EnableUpgradeInsecureRequests"/> instead.</para>
    /// </summary>
    [Obsolete("block-all-mixed-content is deprecated in CSP Level 3. Modern browsers block mixed content by default. Use EnableUpgradeInsecureRequests instead.")]
    public bool EnableBlockAllMixedContent { get; init; }

    /// <summary>
    /// Builds the CSP header value string from the configured directives.
    /// </summary>
    /// <returns>A semicolon-separated CSP policy string ready for the Content-Security-Policy header.</returns>
    public string Build()
    {
        // PERF: StringBuilder avoids List<string> + intermediate $"{name} {value}" allocations
        var sb = new StringBuilder(512);

        // Fetch directives
        AppendDirective(sb, "default-src", DefaultSrc);
        AppendDirective(sb, "script-src", ScriptSrc);
        AppendDirective(sb, "script-src-elem", ScriptSrcElem);
        AppendDirective(sb, "script-src-attr", ScriptSrcAttr);
        AppendDirective(sb, "style-src", StyleSrc);
        AppendDirective(sb, "style-src-elem", StyleSrcElem);
        AppendDirective(sb, "style-src-attr", StyleSrcAttr);
        AppendDirective(sb, "img-src", ImgSrc);
        AppendDirective(sb, "font-src", FontSrc);
        AppendDirective(sb, "connect-src", ConnectSrc);
        AppendDirective(sb, "media-src", MediaSrc);
        AppendDirective(sb, "object-src", ObjectSrc);
        AppendDirective(sb, "child-src", ChildSrc);
        AppendDirective(sb, "frame-src", FrameSrc);
        AppendDirective(sb, "worker-src", WorkerSrc);
        AppendDirective(sb, "manifest-src", ManifestSrc);
        AppendDirective(sb, "fenced-frame-src", FencedFrameSrc);

        // Document directives
        AppendDirective(sb, "base-uri", BaseUri);
        AppendDirective(sb, "sandbox", Sandbox);

        // Navigation directives
        AppendDirective(sb, "form-action", FormAction);
        AppendDirective(sb, "frame-ancestors", FrameAncestors);

        // Trusted Types
        AppendDirective(sb, "require-trusted-types-for", RequireTrustedTypesFor);
        AppendDirective(sb, "trusted-types", TrustedTypes);

        // Transport
        if (EnableUpgradeInsecureRequests)
            AppendBooleanDirective(sb, "upgrade-insecure-requests");

#pragma warning disable CS0618 // Obsolete member access is intentional — we still emit the directive when enabled
        if (EnableBlockAllMixedContent)
            AppendBooleanDirective(sb, "block-all-mixed-content");
#pragma warning restore CS0618

        // Reporting
        AppendDirective(sb, "report-to", ReportTo);

#pragma warning disable CS0618
        AppendDirective(sb, "report-uri", ReportUri);
#pragma warning restore CS0618

        return sb.ToString();
    }

    private static void AppendDirective(StringBuilder sb, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (sb.Length > 0) sb.Append("; ");
            sb.Append(name).Append(' ').Append(value);
        }
    }

    private static void AppendBooleanDirective(StringBuilder sb, string name)
    {
        if (sb.Length > 0) sb.Append("; ");
        sb.Append(name);
    }
}
