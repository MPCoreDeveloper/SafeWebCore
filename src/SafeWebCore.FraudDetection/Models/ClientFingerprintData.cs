namespace SafeWebCore.FraudDetection.Models;

/// <summary>
/// Aggregated fingerprint signals collected from the client's HTTP request and
/// optional client-side JavaScript detection.
/// Pass this record to <see cref="Abstractions.IFraudDetector.Analyze"/> to
/// receive a <see cref="FraudReport"/>.
/// </summary>
/// <remarks>
/// All properties are optional. Providing more signals improves accuracy.
/// Pre-populate <see cref="ResolvedCountryCode"/> when you already have a
/// geo-IP result to avoid a second lookup by the detector.
/// </remarks>
public sealed record ClientFingerprintData
{
    // ── Network signals ────────────────────────────────────────────────────

    /// <summary>
    /// Client IP address (IPv4 or IPv6). Used for geo-IP resolution when an
    /// <see cref="Abstractions.IGeoIpService"/> is registered.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// Pre-resolved ISO 3166-1 alpha-2 country code (e.g. <c>"NL"</c>).
    /// Supply this directly if you already have a geo-IP result; the detector
    /// will not call <see cref="Abstractions.IGeoIpService"/> for this field
    /// when it is non-null.
    /// </summary>
    public string? ResolvedCountryCode { get; init; }

    // ── HTTP header signals ────────────────────────────────────────────────

    /// <summary>
    /// Value of the <c>Accept-Language</c> HTTP header, e.g.
    /// <c>"ru-RU,ru;q=0.9,en-US;q=0.8"</c>.
    /// </summary>
    public string? AcceptLanguage { get; init; }

    /// <summary>Value of the <c>User-Agent</c> HTTP header.</summary>
    public string? UserAgent { get; init; }

    // ── Client-side timezone signals ───────────────────────────────────────

    /// <summary>
    /// IANA timezone identifier reported by the browser, e.g.
    /// <c>"Europe/Moscow"</c> or <c>"Asia/Kolkata"</c>.
    /// Obtain via <c>Intl.DateTimeFormat().resolvedOptions().timeZone</c>.
    /// </summary>
    public string? SystemTimezone { get; init; }

    /// <summary>
    /// UTC offset in minutes as reported by <c>new Date().getTimezoneOffset()</c>.
    /// Note: this value is <em>negative</em> east of UTC.
    /// Used as a fallback when <see cref="SystemTimezone"/> is unavailable.
    /// </summary>
    public int? TimezoneOffsetMinutes { get; init; }

    // ── Browser language signals ───────────────────────────────────────────

    /// <summary>
    /// Full ordered list of language tags from <c>navigator.languages</c>,
    /// e.g. <c>["ru-RU", "ru", "en-US"]</c>.
    /// </summary>
    public IReadOnlyList<string> BrowserLanguages { get; init; } = [];

    // ── Device fingerprint / font signals ─────────────────────────────────

    /// <summary>
    /// FingerprintJS (or compatible library) visitor identifier. Stored in the
    /// report for correlation; not directly scored by the built-in detector.
    /// </summary>
    public string? FingerprintVisitorId { get; init; }

    /// <summary>
    /// <see langword="true"/> when the device can render Cyrillic script,
    /// as determined by a client-side canvas font-probe.
    /// </summary>
    public bool HasCyrillicFontSupport { get; init; }

    /// <summary>
    /// <see langword="true"/> when the device can render Arabic script,
    /// as determined by a client-side canvas font-probe.
    /// </summary>
    public bool HasArabicFontSupport { get; init; }

    /// <summary>
    /// <see langword="true"/> when the device can render CJK
    /// (Chinese / Japanese / Korean) script,
    /// as determined by a client-side canvas font-probe.
    /// </summary>
    public bool HasCjkFontSupport { get; init; }

    /// <summary>
    /// <see langword="true"/> when the device can render Devanagari script
    /// (Hindi / Sanskrit / Marathi), as determined by a client-side canvas
    /// font-probe.
    /// </summary>
    public bool HasDevanagariSupport { get; init; }

    /// <summary>
    /// Additional font family names installed on the device as detected
    /// client-side. Useful for custom scoring extensions.
    /// </summary>
    public IReadOnlyList<string> InstalledFonts { get; init; } = [];

    // ── Optional session metadata ──────────────────────────────────────────

    /// <summary>
    /// Screen resolution string reported by the client, e.g. <c>"1920x1080"</c>.
    /// Available for custom scoring extensions.
    /// </summary>
    public string? ScreenResolution { get; init; }

    /// <summary>
    /// Optional tenant identifier used to resolve tenant-specific fraud settings.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// HTTP method of the analyzed request.
    /// </summary>
    public string? RequestMethod { get; init; }

    /// <summary>
    /// Request path of the analyzed request.
    /// </summary>
    public string? RequestPath { get; init; }

    /// <summary>
    /// Request timestamp in UTC. When omitted, the detector uses current UTC time.
    /// </summary>
    public DateTimeOffset? RequestTimestampUtc { get; init; }

    /// <summary>
    /// Request headers mapped as key/value pairs for scanner heuristics and bypass checks.
    /// Header names are matched case-insensitively.
    /// </summary>
    public IReadOnlyDictionary<string, string> RequestHeaders { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
