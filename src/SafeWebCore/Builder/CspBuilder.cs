using SafeWebCore.Options;

namespace SafeWebCore.Builder;

/// <summary>
/// Fluent builder for configuring Content Security Policy (CSP) options.
/// All methods return <c>this</c> for chaining. Call <see cref="Build"/> to produce the immutable <see cref="CspOptions"/>.
/// </summary>
public sealed class CspBuilder
{
    private CspOptions _options = new();

    // ── Fetch directives ───────────────────────────────────────────────────

    /// <summary>Sets the <c>default-src</c> directive.</summary>
    public CspBuilder DefaultSrc(string value) { _options = _options with { DefaultSrc = value }; return this; }

    /// <summary>Sets the <c>script-src</c> directive.</summary>
    public CspBuilder ScriptSrc(string value) { _options = _options with { ScriptSrc = value }; return this; }

    /// <summary>Sets the <c>script-src-elem</c> directive (CSP Level 3).</summary>
    public CspBuilder ScriptSrcElem(string value) { _options = _options with { ScriptSrcElem = value }; return this; }

    /// <summary>Sets the <c>script-src-attr</c> directive (CSP Level 3).</summary>
    public CspBuilder ScriptSrcAttr(string value) { _options = _options with { ScriptSrcAttr = value }; return this; }

    /// <summary>Sets the <c>style-src</c> directive.</summary>
    public CspBuilder StyleSrc(string value) { _options = _options with { StyleSrc = value }; return this; }

    /// <summary>Sets the <c>style-src-elem</c> directive (CSP Level 3).</summary>
    public CspBuilder StyleSrcElem(string value) { _options = _options with { StyleSrcElem = value }; return this; }

    /// <summary>Sets the <c>style-src-attr</c> directive (CSP Level 3).</summary>
    public CspBuilder StyleSrcAttr(string value) { _options = _options with { StyleSrcAttr = value }; return this; }

    /// <summary>Sets the <c>img-src</c> directive.</summary>
    public CspBuilder ImgSrc(string value) { _options = _options with { ImgSrc = value }; return this; }

    /// <summary>Sets the <c>font-src</c> directive.</summary>
    public CspBuilder FontSrc(string value) { _options = _options with { FontSrc = value }; return this; }

    /// <summary>Sets the <c>connect-src</c> directive.</summary>
    public CspBuilder ConnectSrc(string value) { _options = _options with { ConnectSrc = value }; return this; }

    /// <summary>Sets the <c>media-src</c> directive.</summary>
    public CspBuilder MediaSrc(string value) { _options = _options with { MediaSrc = value }; return this; }

    /// <summary>Sets the <c>object-src</c> directive.</summary>
    public CspBuilder ObjectSrc(string value) { _options = _options with { ObjectSrc = value }; return this; }

    /// <summary>Sets the <c>child-src</c> directive.</summary>
    public CspBuilder ChildSrc(string value) { _options = _options with { ChildSrc = value }; return this; }

    /// <summary>Sets the <c>worker-src</c> directive.</summary>
    public CspBuilder WorkerSrc(string value) { _options = _options with { WorkerSrc = value }; return this; }

    /// <summary>Sets the <c>manifest-src</c> directive.</summary>
    public CspBuilder ManifestSrc(string value) { _options = _options with { ManifestSrc = value }; return this; }

    /// <summary>Sets the <c>fenced-frame-src</c> directive (Privacy Sandbox / 2025+).</summary>
    public CspBuilder FencedFrameSrc(string value) { _options = _options with { FencedFrameSrc = value }; return this; }

    // ── Document directives ────────────────────────────────────────────────

    /// <summary>Sets the <c>base-uri</c> directive.</summary>
    public CspBuilder BaseUri(string value) { _options = _options with { BaseUri = value }; return this; }

    /// <summary>Sets the <c>sandbox</c> directive. E.g. <c>"allow-scripts allow-same-origin"</c>.</summary>
    public CspBuilder Sandbox(string value) { _options = _options with { Sandbox = value }; return this; }

    // ── Navigation directives ──────────────────────────────────────────────

    /// <summary>Sets the <c>form-action</c> directive.</summary>
    public CspBuilder FormAction(string value) { _options = _options with { FormAction = value }; return this; }

    /// <summary>Sets the <c>frame-ancestors</c> directive.</summary>
    public CspBuilder FrameAncestors(string value) { _options = _options with { FrameAncestors = value }; return this; }

    // ── Trusted Types ──────────────────────────────────────────────────────

    /// <summary>Sets the <c>require-trusted-types-for</c> directive. E.g. <c>"'script'"</c>.</summary>
    public CspBuilder RequireTrustedTypesFor(string value) { _options = _options with { RequireTrustedTypesFor = value }; return this; }

    /// <summary>Sets the <c>trusted-types</c> directive. E.g. <c>"myPolicy 'allow-duplicates'"</c>.</summary>
    public CspBuilder TrustedTypes(string value) { _options = _options with { TrustedTypes = value }; return this; }

    // ── Transport ──────────────────────────────────────────────────────────

    /// <summary>Enables or disables <c>upgrade-insecure-requests</c>.</summary>
    public CspBuilder UpgradeInsecureRequests(bool enable = true) { _options = _options with { EnableUpgradeInsecureRequests = enable }; return this; }

    /// <summary>
    /// Enables or disables <c>block-all-mixed-content</c>.
    /// <para><b>Deprecated in CSP Level 3.</b> Modern browsers block mixed content by default.</para>
    /// </summary>
    [Obsolete("block-all-mixed-content is deprecated in CSP Level 3. Use UpgradeInsecureRequests instead.")]
    public CspBuilder BlockAllMixedContent(bool enable = true)
    {
#pragma warning disable CS0618
        _options = _options with { EnableBlockAllMixedContent = enable };
#pragma warning restore CS0618
        return this;
    }

    // ── Reporting ──────────────────────────────────────────────────────────

    /// <summary>Sets the <c>report-to</c> group name (Reporting API v1).</summary>
    public CspBuilder ReportTo(string value) { _options = _options with { ReportTo = value }; return this; }

    /// <summary>
    /// Sets the legacy <c>report-uri</c> endpoint.
    /// <para><b>Deprecated in CSP Level 3.</b> Use <see cref="ReportTo"/> with the Reporting API instead.</para>
    /// </summary>
    [Obsolete("report-uri is deprecated in CSP Level 3. Use ReportTo with the Reporting API instead.")]
    public CspBuilder ReportUri(string value)
    {
#pragma warning disable CS0618
        _options = _options with { ReportUri = value };
#pragma warning restore CS0618
        return this;
    }

    // ── Build ──────────────────────────────────────────────────────────────

    /// <summary>Builds the immutable <see cref="CspOptions"/>.</summary>
    public CspOptions Build() => _options;
}
