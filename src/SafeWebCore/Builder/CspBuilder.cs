using SafeWebCore.Options;

namespace SafeWebCore.Builder;

/// <summary>
/// Fluent builder for configuring Content Security Policy (CSP) options.
/// <para>
/// Implements the full <b>CSP Level 3</b> (W3C Recommendation) directive set and forward-looking
/// <b>CSP Level 4</b> features including Trusted Types and <c>fenced-frame-src</c>.
/// </para>
/// <para>
/// All methods return <c>this</c> for chaining. Call <see cref="Build"/> at the end to produce
/// the immutable <see cref="CspOptions"/> record.
/// </para>
/// <example>
/// <code>
/// var csp = new CspBuilder()
///     .DefaultSrc("'none'")
///     .ScriptSrc("'nonce-{nonce}' 'strict-dynamic'")
///     .StyleSrc("'nonce-{nonce}'")
///     .ImgSrc("'self' https://cdn.example.com data:")
///     .ConnectSrc("'self' https://api.example.com wss://ws.example.com")
///     .FrameSrc("https://www.youtube.com")
///     .FrameAncestors("'none'")
///     .BaseUri("'none'")
///     .FormAction("'self'")
///     .RequireTrustedTypesFor("'script'")
///     .UpgradeInsecureRequests()
///     .Build();
/// </code>
/// </example>
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description><b>CSP Level 3:</b> <c>worker-src</c>, <c>manifest-src</c>, <c>frame-src</c> (split from <c>child-src</c>),
///   <c>script-src-elem/attr</c>, <c>style-src-elem/attr</c>, <c>report-to</c>, nonce/hash support, <c>strict-dynamic</c>.</description></item>
///   <item><description><b>CSP Level 4 (emerging):</b> Trusted Types (<c>require-trusted-types-for</c>, <c>trusted-types</c>),
///   <c>fenced-frame-src</c> (Privacy Sandbox).</description></item>
///   <item><description><b>Deprecated directives:</b> <c>report-uri</c> and <c>block-all-mixed-content</c> are intentionally
///   excluded from the builder. They remain available on <see cref="CspOptions"/> with <c>[Obsolete]</c> attributes
///   for backward compatibility.</description></item>
/// </list>
/// <para>
/// <b>Directive values</b> are space-separated source expressions. To allow multiple origins, combine them
/// in a single string: <c>"'self' https://cdn1.example.com https://cdn2.example.com"</c>.
/// </para>
/// <para>
/// <b>Nonce placeholder:</b> Use <c>{nonce}</c> in directive values. At runtime the middleware replaces it
/// with the per-request cryptographic nonce:
/// <c>script-src 'nonce-{nonce}' 'strict-dynamic'</c> → <c>script-src 'nonce-k7sJ2mP9xQ...' 'strict-dynamic'</c>.
/// </para>
/// <para>
/// <b>Hash-based allowlisting:</b> Pass hash tokens directly in any directive value:
/// <c>.ScriptSrc("'sha256-abc123...' 'strict-dynamic'")</c>. This is a CSP Level 3 feature for
/// allowing specific inline scripts/styles by their SHA-256, SHA-384, or SHA-512 digest.
/// </para>
/// <para>
/// <b>Validate your policy:</b> After deploying, test your CSP headers using these tools:
/// </para>
/// <list type="bullet">
///   <item><description><see href="https://securityheaders.com/">securityheaders.com</see> — Scans all response
///   headers and grades your site A+ through F. Validates HSTS, CSP, Permissions-Policy, and more.</description></item>
///   <item><description><see href="https://csp-evaluator.withgoogle.com/">Google CSP Evaluator</see> — Analyzes your
///   Content-Security-Policy for common misconfigurations (e.g. missing <c>object-src</c>, <c>'unsafe-inline'</c>
///   without nonce, missing <c>'strict-dynamic'</c>).</description></item>
/// </list>
/// </remarks>
public sealed class CspBuilder
{
    private CspOptions _options = new();

    // ── Fetch directives (CSP Level 2 + Level 3) ───────────────────────────

    /// <summary>
    /// Sets the <c>default-src</c> directive — the fallback for all fetch directives not explicitly set.
    /// </summary>
    /// <param name="value">
    /// Space-separated source expressions. Use <c>"'none'"</c> for the strictest policy (recommended).
    /// </param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.DefaultSrc("'none'")</code></example>
    public CspBuilder DefaultSrc(string value) { _options = _options with { DefaultSrc = value }; return this; }

    /// <summary>
    /// Sets the <c>script-src</c> directive — controls which scripts can execute.
    /// </summary>
    /// <param name="value">
    /// Space-separated source expressions. Recommended: <c>"'nonce-{nonce}' 'strict-dynamic'"</c>
    /// for nonce-based enforcement with trust propagation.
    /// <para>Supports CSP Level 3 nonce (<c>'nonce-...'</c>), hash (<c>'sha256-...'</c>),
    /// and <c>'strict-dynamic'</c> keywords.</para>
    /// </param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.ScriptSrc("'nonce-{nonce}' 'strict-dynamic'")</code></example>
    public CspBuilder ScriptSrc(string value) { _options = _options with { ScriptSrc = value }; return this; }

    /// <summary>
    /// Sets the <c>script-src-elem</c> directive (CSP Level 3) — restricts <c>&lt;script&gt;</c> elements
    /// and inline <c>javascript:</c> navigation. Falls back to <c>script-src</c> if not set.
    /// </summary>
    /// <param name="value">Space-separated source expressions. Empty string disables this directive (inherits from <c>script-src</c>).</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.ScriptSrcElem("'self' https://cdn.example.com")</code></example>
    public CspBuilder ScriptSrcElem(string value) { _options = _options with { ScriptSrcElem = value }; return this; }

    /// <summary>
    /// Sets the <c>script-src-attr</c> directive (CSP Level 3) — restricts inline event handlers
    /// such as <c>onclick</c> and <c>onload</c>. Falls back to <c>script-src</c> if not set.
    /// </summary>
    /// <param name="value">Space-separated source expressions. Empty string disables this directive (inherits from <c>script-src</c>).</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.ScriptSrcAttr("'none'")</code></example>
    public CspBuilder ScriptSrcAttr(string value) { _options = _options with { ScriptSrcAttr = value }; return this; }

    /// <summary>
    /// Sets the <c>style-src</c> directive — controls which stylesheets can be applied.
    /// </summary>
    /// <param name="value">
    /// Space-separated source expressions. Recommended: <c>"'nonce-{nonce}'"</c> for nonce-based enforcement.
    /// <para>Supports CSP Level 3 nonce and hash keywords.</para>
    /// </param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.StyleSrc("'nonce-{nonce}'")</code></example>
    public CspBuilder StyleSrc(string value) { _options = _options with { StyleSrc = value }; return this; }

    /// <summary>
    /// Sets the <c>style-src-elem</c> directive (CSP Level 3) — restricts <c>&lt;style&gt;</c> elements
    /// and <c>&lt;link rel="stylesheet"&gt;</c>. Falls back to <c>style-src</c> if not set.
    /// </summary>
    /// <param name="value">Space-separated source expressions. Empty string disables this directive (inherits from <c>style-src</c>).</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.StyleSrcElem("'self' https://cdn.example.com")</code></example>
    public CspBuilder StyleSrcElem(string value) { _options = _options with { StyleSrcElem = value }; return this; }

    /// <summary>
    /// Sets the <c>style-src-attr</c> directive (CSP Level 3) — restricts inline <c>style</c> attributes.
    /// Falls back to <c>style-src</c> if not set.
    /// </summary>
    /// <param name="value">Space-separated source expressions. Empty string disables this directive (inherits from <c>style-src</c>).</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.StyleSrcAttr("'unsafe-inline'")</code></example>
    public CspBuilder StyleSrcAttr(string value) { _options = _options with { StyleSrcAttr = value }; return this; }

    /// <summary>
    /// Sets the <c>img-src</c> directive — controls which image sources are allowed.
    /// </summary>
    /// <param name="value">
    /// Space-separated source expressions. Common values include <c>'self'</c>, <c>https:</c>, <c>data:</c>,
    /// and specific CDN origins.
    /// </param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.ImgSrc("'self' https://cdn.example.com data:")</code></example>
    public CspBuilder ImgSrc(string value) { _options = _options with { ImgSrc = value }; return this; }

    /// <summary>
    /// Sets the <c>font-src</c> directive — controls which font sources are allowed.
    /// Falls back to <c>default-src</c> if not set.
    /// </summary>
    /// <param name="value">Space-separated source expressions.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.FontSrc("'self' https://fonts.gstatic.com")</code></example>
    public CspBuilder FontSrc(string value) { _options = _options with { FontSrc = value }; return this; }

    /// <summary>
    /// Sets the <c>connect-src</c> directive — controls which URLs can be loaded via
    /// <c>XMLHttpRequest</c>, <c>fetch()</c>, <c>WebSocket</c>, <c>EventSource</c>, and <c>navigator.sendBeacon()</c>.
    /// Falls back to <c>default-src</c> if not set.
    /// </summary>
    /// <param name="value">Space-separated source expressions.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.ConnectSrc("'self' https://api.example.com wss://ws.example.com")</code></example>
    public CspBuilder ConnectSrc(string value) { _options = _options with { ConnectSrc = value }; return this; }

    /// <summary>
    /// Sets the <c>media-src</c> directive — controls which <c>&lt;audio&gt;</c> and <c>&lt;video&gt;</c>
    /// sources are allowed. Falls back to <c>default-src</c> if not set.
    /// </summary>
    /// <param name="value">Space-separated source expressions.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.MediaSrc("'self' https://media.example.com")</code></example>
    public CspBuilder MediaSrc(string value) { _options = _options with { MediaSrc = value }; return this; }

    /// <summary>
    /// Sets the <c>object-src</c> directive — controls <c>&lt;object&gt;</c>, <c>&lt;embed&gt;</c>, and
    /// <c>&lt;applet&gt;</c> sources. Should always be set to <c>"'none'"</c> to prevent Flash/plugin-based attacks.
    /// </summary>
    /// <param name="value">Space-separated source expressions. Recommended: <c>"'none'"</c>.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.ObjectSrc("'none'")</code></example>
    public CspBuilder ObjectSrc(string value) { _options = _options with { ObjectSrc = value }; return this; }

    /// <summary>
    /// Sets the <c>child-src</c> directive — restricts nested browsing contexts (<c>&lt;frame&gt;</c>,
    /// <c>&lt;iframe&gt;</c>) and workers. In CSP Level 3, prefer <see cref="FrameSrc"/> for frames
    /// and <see cref="WorkerSrc"/> for workers. Falls back to <c>default-src</c> if not set.
    /// </summary>
    /// <param name="value">Space-separated source expressions.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.ChildSrc("'none'")</code></example>
    public CspBuilder ChildSrc(string value) { _options = _options with { ChildSrc = value }; return this; }

    /// <summary>
    /// Sets the <c>frame-src</c> directive (CSP Level 3) — controls which sources can be loaded in
    /// <c>&lt;frame&gt;</c> and <c>&lt;iframe&gt;</c> elements. Split from <c>child-src</c> in CSP Level 3
    /// to separate frame and worker policies. Falls back to <c>child-src</c> → <c>default-src</c>.
    /// </summary>
    /// <param name="value">Space-separated source expressions.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.FrameSrc("https://www.youtube.com https://player.vimeo.com")</code></example>
    public CspBuilder FrameSrc(string value) { _options = _options with { FrameSrc = value }; return this; }

    /// <summary>
    /// Sets the <c>worker-src</c> directive (CSP Level 3) — controls which sources can be used as
    /// <c>Worker</c>, <c>SharedWorker</c>, and <c>ServiceWorker</c>. Split from <c>child-src</c> in
    /// CSP Level 3. Falls back to <c>child-src</c> → <c>script-src</c> → <c>default-src</c>.
    /// </summary>
    /// <param name="value">Space-separated source expressions.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.WorkerSrc("'self' blob:")</code></example>
    public CspBuilder WorkerSrc(string value) { _options = _options with { WorkerSrc = value }; return this; }

    /// <summary>
    /// Sets the <c>manifest-src</c> directive (CSP Level 3) — controls which sources can serve
    /// web app manifests. Falls back to <c>default-src</c> if not set.
    /// </summary>
    /// <param name="value">Space-separated source expressions.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.ManifestSrc("'self'")</code></example>
    public CspBuilder ManifestSrc(string value) { _options = _options with { ManifestSrc = value }; return this; }

    /// <summary>
    /// Sets the <c>fenced-frame-src</c> directive (CSP Level 4 / Privacy Sandbox, 2025+) —
    /// controls which sources can be loaded in <c>&lt;fencedframe&gt;</c> elements.
    /// This is an emerging directive for privacy-preserving embedded content.
    /// </summary>
    /// <param name="value">Space-separated source expressions.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.FencedFrameSrc("'self'")</code></example>
    public CspBuilder FencedFrameSrc(string value) { _options = _options with { FencedFrameSrc = value }; return this; }

    // ── Document directives ────────────────────────────────────────────────

    /// <summary>
    /// Sets the <c>base-uri</c> directive — restricts which URIs can be used in <c>&lt;base&gt;</c> elements.
    /// Should be set to <c>"'none'"</c> or <c>"'self'"</c> to prevent base-tag hijacking attacks.
    /// </summary>
    /// <param name="value">Space-separated source expressions. Recommended: <c>"'none'"</c>.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.BaseUri("'none'")</code></example>
    public CspBuilder BaseUri(string value) { _options = _options with { BaseUri = value }; return this; }

    /// <summary>
    /// Sets the <c>sandbox</c> directive — applies sandbox restrictions similar to the
    /// <c>&lt;iframe sandbox&gt;</c> attribute. Restricts actions like popups, scripts, forms, etc.
    /// </summary>
    /// <param name="value">
    /// Space-separated sandbox flags. Common flags: <c>allow-scripts</c>, <c>allow-same-origin</c>,
    /// <c>allow-forms</c>, <c>allow-popups</c>, <c>allow-modals</c>.
    /// </param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.Sandbox("allow-scripts allow-same-origin")</code></example>
    public CspBuilder Sandbox(string value) { _options = _options with { Sandbox = value }; return this; }

    // ── Navigation directives ──────────────────────────────────────────────

    /// <summary>
    /// Sets the <c>form-action</c> directive — restricts which URLs can be used as the target
    /// of form submissions (<c>&lt;form action="..."&gt;</c>).
    /// </summary>
    /// <param name="value">Space-separated source expressions. Recommended: <c>"'self'"</c>.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.FormAction("'self'")</code></example>
    public CspBuilder FormAction(string value) { _options = _options with { FormAction = value }; return this; }

    /// <summary>
    /// Sets the <c>frame-ancestors</c> directive — controls which parent pages can embed this page
    /// via <c>&lt;frame&gt;</c>, <c>&lt;iframe&gt;</c>, <c>&lt;object&gt;</c>, or <c>&lt;embed&gt;</c>.
    /// This is the CSP replacement for the <c>X-Frame-Options</c> header.
    /// </summary>
    /// <param name="value">
    /// Space-separated source expressions. Use <c>"'none'"</c> to prevent all embedding (equivalent to
    /// <c>X-Frame-Options: DENY</c>) or <c>"'self'"</c> for same-origin only.
    /// </param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.FrameAncestors("'none'")</code></example>
    public CspBuilder FrameAncestors(string value) { _options = _options with { FrameAncestors = value }; return this; }

    // ── Trusted Types (CSP Level 3 / emerging Level 4) ─────────────────────

    /// <summary>
    /// Sets the <c>require-trusted-types-for</c> directive (CSP Level 3 / emerging Level 4) —
    /// enforces Trusted Types for dangerous DOM sinks like <c>innerHTML</c>, <c>document.write()</c>,
    /// and <c>eval()</c>. Prevents DOM-based XSS at the API level.
    /// </summary>
    /// <param name="value">Currently only <c>"'script'"</c> is defined. This guards all script-execution DOM sinks.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.RequireTrustedTypesFor("'script'")</code></example>
    public CspBuilder RequireTrustedTypesFor(string value) { _options = _options with { RequireTrustedTypesFor = value }; return this; }

    /// <summary>
    /// Sets the <c>trusted-types</c> directive (CSP Level 3 / emerging Level 4) —
    /// controls which Trusted Type policy names are allowed. Use <c>"'none'"</c> to block all policies,
    /// or list named policies your application needs.
    /// </summary>
    /// <param name="value">
    /// Space-separated policy names. Use <c>"'none'"</c> to disallow all, <c>"'allow-duplicates'"</c>
    /// to permit re-creating policies with the same name.
    /// </param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.TrustedTypes("myPolicy 'allow-duplicates'")</code></example>
    public CspBuilder TrustedTypes(string value) { _options = _options with { TrustedTypes = value }; return this; }

    // ── Transport ──────────────────────────────────────────────────────────

    /// <summary>
    /// Enables or disables the <c>upgrade-insecure-requests</c> directive — instructs the browser
    /// to automatically upgrade all HTTP requests to HTTPS before making them.
    /// Enabled by default.
    /// </summary>
    /// <param name="enable"><c>true</c> (default) to include the directive; <c>false</c> to omit it.</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.UpgradeInsecureRequests()</code></example>
    public CspBuilder UpgradeInsecureRequests(bool enable = true) { _options = _options with { EnableUpgradeInsecureRequests = enable }; return this; }

    // ── Reporting (CSP Level 3) ────────────────────────────────────────────

    /// <summary>
    /// Sets the <c>report-to</c> directive (CSP Level 3, Reporting API v1) — specifies the endpoint
    /// group name for receiving CSP violation reports. Replaces the deprecated <c>report-uri</c> directive.
    /// <para>
    /// The group name must match a <c>Reporting-Endpoints</c> response header entry. Use with
    /// <see cref="SafeWebCore.Infrastructure.CspReportMiddleware"/> for built-in reporting.
    /// </para>
    /// </summary>
    /// <param name="value">The reporting endpoint group name (e.g. <c>"default"</c>, <c>"csp-endpoint"</c>).</param>
    /// <returns>This builder for chaining.</returns>
    /// <example><code>.ReportTo("default")</code></example>
    public CspBuilder ReportTo(string value) { _options = _options with { ReportTo = value }; return this; }

    // ── Build ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds and returns the immutable <see cref="CspOptions"/> record configured by this builder.
    /// The resulting record can be further modified using C# <c>with</c> expressions if needed.
    /// </summary>
    /// <returns>An immutable <see cref="CspOptions"/> containing all configured CSP directives.</returns>
    public CspOptions Build() => _options;
}
