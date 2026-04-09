namespace SafeWebCore.FraudDetection.Models;

/// <summary>
/// Well-known trigger keys returned in <see cref="FraudReport.Triggers"/>.
/// Use these constants when checking for specific signals programmatically.
/// </summary>
public static class FraudTrigger
{
    // ── Country / IP signals ───────────────────────────────────────────────

    /// <summary>
    /// The resolved IP country is not in the configured allowed-countries set.
    /// </summary>
    public const string IpNonWestern = "ip_non_western";

    /// <summary>
    /// The resolved IP country is in the known vacation-countries set.
    /// Carries a reduced weight when travel mode is enabled.
    /// </summary>
    public const string IpVacationCountry = "ip_vacation_country";

    // ── Timezone signals ───────────────────────────────────────────────────

    /// <summary>
    /// The reported system timezone is strongly associated with a non-Western region
    /// (e.g. <c>Europe/Moscow</c>, <c>Asia/Kolkata</c>, <c>Asia/Shanghai</c>).
    /// </summary>
    public const string TimezoneNonWestern = "timezone_non_western";

    /// <summary>
    /// The reported system timezone matches a popular vacation destination.
    /// Carries a reduced weight when travel mode is enabled.
    /// </summary>
    public const string TimezoneVacation = "timezone_vacation";

    // ── Language signals ───────────────────────────────────────────────────

    /// <summary>
    /// The <c>navigator.languages</c> list contains a non-Western primary language tag.
    /// </summary>
    public const string BrowserLanguageNonWestern = "browser_language_non_western";

    /// <summary>
    /// The <c>Accept-Language</c> HTTP header contains a non-Western locale.
    /// </summary>
    public const string AcceptLanguageNonWestern = "accept_language_non_western";

    // ── Font / script signals ──────────────────────────────────────────────

    /// <summary>Cyrillic script font support detected on the device.</summary>
    public const string FontCyrillic = "font_cyrillic";

    /// <summary>Arabic script font support detected on the device.</summary>
    public const string FontArabic = "font_arabic";

    /// <summary>CJK (Chinese / Japanese / Korean) font support detected on the device.</summary>
    public const string FontCjk = "font_cjk";

    /// <summary>Devanagari (Hindi / Sanskrit) font support detected on the device.</summary>
    public const string FontDevanagari = "font_devanagari";

    // ── Cross-signal inconsistency bonuses ─────────────────────────────────

    /// <summary>
    /// Western-appearing IP address combined with a non-Western system timezone —
    /// the single strongest indicator of VPN-based impersonation.
    /// </summary>
    public const string InconsistencyIpTimezone = "inconsistency_ip_timezone";

    /// <summary>
    /// Western-appearing IP address combined with non-Western browser language
    /// preferences and a non-Western timezone simultaneously.
    /// </summary>
    public const string InconsistencyIpLanguageAndTimezone = "inconsistency_ip_language_and_timezone";

    /// <summary>
    /// Western-appearing IP address combined with non-Western browser language
    /// preferences only (no timezone mismatch). Weaker signal — may occur in
    /// immigrant or multicultural contexts.
    /// </summary>
    public const string InconsistencyIpLanguageOnly = "inconsistency_ip_language_only";

    // ── Scanner / pentest signals ─────────────────────────────────────────

    /// <summary>
    /// Authorized penetration-test bypass header is present and valid.
    /// </summary>
    public const string PenTestBypassAuthorized = "pentest_bypass_authorized";

    /// <summary>
    /// OWASP ZAP-specific header was detected.
    /// </summary>
    public const string ScannerZapHeader = "scanner_zap_header";

    /// <summary>
    /// A configured scanner-identification header was detected.
    /// </summary>
    public const string ScannerHeader = "scanner_header";

    /// <summary>
    /// User-Agent contains a configured scanner token.
    /// </summary>
    public const string ScannerUserAgent = "scanner_user_agent";

    /// <summary>
    /// Request path contains a known scanner probe fragment.
    /// </summary>
    public const string ScannerPathProbe = "scanner_path_probe";

    /// <summary>
    /// Request burst rate exceeds configured threshold.
    /// </summary>
    public const string ScannerBurstRate = "scanner_burst_rate";
}
