namespace SafeWebCore.FraudDetection.Options;

/// <summary>
/// Neutral, multi-region configuration for detecting strong geo-cultural inconsistencies.
/// 
/// The detector answers: "Are the observed signals (IP country, timezone, browser language, fonts)
/// consistent with the expected primary region for this service?"
/// 
/// Configure for any region you serve:
/// - Western Europe + North America
/// - Gulf / Arabic-speaking countries
/// - Russia / CIS
/// - Sub-Saharan Africa
/// - East Asia
/// - Latin America, etc.
/// 
/// All collections are empty by default — populate the sets relevant to your audience.
/// The detector penalizes signals inconsistent with <see cref="ExpectedCountries"/>.
/// </summary>
public sealed class GeoCulturalConsistencyOptions
{
    // ── Expected / Primary Region ──────────────────────────────────────────

    /// <summary>
    /// ISO 3166-1 alpha-2 country codes that represent the primary/expected region
    /// for this configuration (e.g. "NL", "DE", "US", "AE", "RU", "ZA", "JP", ...).
    /// 
    /// Users whose IP resolves to one of these countries receive no geo-IP penalty.
    /// This is the core "allowed / home" set.
    /// </summary>
    public HashSet<string> ExpectedCountries { get; set; } = [];

    // ── Legitimate Travel Destinations ─────────────────────────────────────

    /// <summary>
    /// ISO 3166-1 alpha-2 country codes of destinations that are common for
    /// legitimate travel from the expected region (vacation, business, expats).
    /// 
    /// When <see cref="EnableTravelMode"/> is <see langword="true"/>, users in these
    /// countries receive a substantially reduced IP-country penalty.
    /// </summary>
    public HashSet<string> KnownTravelCountries { get; set; } = [];

    // ── Timezone Configuration ─────────────────────────────────────────────

    /// <summary>
    /// IANA timezone identifiers considered inconsistent with the expected region.
    /// Matched against <see cref="Models.ClientFingerprintData.SystemTimezone"/>.
    /// 
    /// Example for a Western-European configuration: Russian, Chinese, Indian, Middle-Eastern
    /// timezones that are very unlikely for a user physically located in Western Europe.
    /// </summary>
    public HashSet<string> InconsistentTimezones { get; set; } = [];

    /// <summary>
    /// IANA timezone identifiers that are considered normal for legitimate travel
    /// from the expected region. These carry a greatly reduced weight when
    /// <see cref="EnableTravelMode"/> is enabled.
    /// </summary>
    public HashSet<string> TravelTimezones { get; set; } = [];

    // ── Language Configuration ─────────────────────────────────────────────

    /// <summary>
    /// BCP-47 primary language sub-tags considered inconsistent with the expected region.
    /// Matched against <see cref="Models.ClientFingerprintData.BrowserLanguages"/>
    /// and the <c>Accept-Language</c> header.
    /// 
    /// These are languages that are very unlikely to be primary for someone
    /// genuinely located in (or culturally from) the expected region.
    /// </summary>
    public HashSet<string> InconsistentLanguageCodes { get; set; } = [];

    // ── Thresholds ─────────────────────────────────────────────────────────

    /// <summary>
    /// Suspicion score at which the verdict moves from Clean to Suspicious.
    /// Default: <c>30</c>.
    /// </summary>
    public int MediumSuspicionThreshold { get; set; } = 30;

    /// <summary>
    /// Suspicion score at which the verdict moves from Suspicious to HighlySuspicious.
    /// Default: <c>65</c>.
    /// </summary>
    public int HighSuspicionThreshold { get; set; } = 65;

    /// <summary>
    /// Suspicion score at which the verdict becomes "strong region impersonation".
    /// Default: <c>85</c>.
    /// </summary>
    public int HighInconsistencyThreshold { get; set; } = 85;

    // ── Feature Toggles ────────────────────────────────────────────────────

    /// <summary>
    /// When <see langword="true"/>, the detector applies reduced penalties for
    /// <see cref="KnownTravelCountries"/> and <see cref="TravelTimezones"/>.
    /// This helps avoid false positives for legitimate travelers.
    /// Default: <see langword="true"/>.
    /// </summary>
    public bool EnableTravelMode { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, font/script support signals from
    /// <see cref="Models.ClientFingerprintData"/> are included in scoring.
    /// Disable if you do not run client-side font probes.
    /// Default: <see langword="true"/>.
    /// </summary>
    public bool EnableDeviceFingerprinting { get; set; } = true;
}
