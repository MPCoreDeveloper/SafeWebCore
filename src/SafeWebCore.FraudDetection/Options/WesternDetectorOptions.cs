namespace SafeWebCore.FraudDetection.Options;

/// <summary>
/// Legacy configuration for the Western-centric detector.
/// 
/// <para><b>Backward compatibility:</b> This type continues to work exactly as before.</para>
/// 
/// <para><b>Recommended for new work:</b> Use <see cref="GeoCulturalConsistencyOptions"/> instead.
/// It allows protecting any primary region (Western, Gulf/Arabic, Russian/CIS, African, East-Asian, etc.)
/// without cultural bias in the naming.</para>
/// </summary>
[Obsolete("Use GeoCulturalConsistencyOptions for new multi-region scenarios. This type remains fully supported for backward compatibility.")]
public sealed class WesternDetectorOptions
{
    // ── Allowed-country set ────────────────────────────────────────────────

    /// <summary>
    /// ISO 3166-1 alpha-2 country codes considered Western/allowed.
    /// Users whose IP resolves to one of these countries receive no geo-IP
    /// penalty. Default includes the main Western European countries plus
    /// USA, Canada, Australia, and New Zealand.
    /// </summary>
    public HashSet<string> AllowedCountries { get; set; } =
    [
        "NL", "BE", "DE", "FR", "GB", "US", "CA", "AU", "NZ",
        "IE", "AT", "CH", "LU", "DK", "SE", "NO", "FI",
        "IT", "ES", "PT"
    ];

    // ── Travel-mode sets ───────────────────────────────────────────────────

    /// <summary>
    /// ISO 3166-1 alpha-2 country codes of popular vacation destinations for
    /// Western travellers. When <see cref="EnableTravelMode"/> is
    /// <see langword="true"/>, users in these countries receive a substantially
    /// reduced IP-country penalty, supporting legitimate holiday scenarios
    /// (e.g. a Dutch user visiting Turkey or Thailand).
    /// </summary>
    public HashSet<string> KnownVacationCountries { get; set; } =
    [
        "TR", "MA", "TN", "EG", "TH", "ID", "PH", "VN",
        "MX", "CU", "DO", "BR", "AE", "QA", "BH",
        "JP", "SG", "GR", "HR", "MT", "CY", "ZA", "KE", "MU"
    ];

    // ── Timezone sets ──────────────────────────────────────────────────────

    /// <summary>
    /// IANA timezone identifiers strongly associated with non-Western regions.
    /// Matched against <see cref="Models.ClientFingerprintData.SystemTimezone"/>.
    /// </summary>
    public HashSet<string> SuspiciousTimezones { get; set; } =
    [
        // Russia
        "Europe/Moscow",      "Europe/Kaliningrad", "Europe/Samara",
        "Asia/Yekaterinburg", "Asia/Omsk",          "Asia/Krasnoyarsk",
        "Asia/Irkutsk",       "Asia/Yakutsk",        "Asia/Vladivostok",
        "Asia/Magadan",       "Asia/Sakhalin",       "Asia/Kamchatka",
        // China / Taiwan
        "Asia/Shanghai",      "Asia/Chongqing",      "Asia/Urumqi",
        "Asia/Taipei",
        // Korea
        "Asia/Seoul",         "Asia/Pyongyang",
        // India / South Asia
        "Asia/Kolkata",       "Asia/Karachi",        "Asia/Dhaka",
        // Arabic-speaking Middle East (excluding popular expat / vacation hubs)
        "Asia/Riyadh",        "Asia/Baghdad",        "Asia/Aden",
        "Asia/Muscat",
        // Central Asia
        "Asia/Tashkent",      "Asia/Samarkand",      "Asia/Bishkek",
        "Asia/Almaty",        "Asia/Ashgabat",       "Asia/Dushanbe",
        // Iran
        "Asia/Tehran",
        // Belarus / Ukraine (CIS region — can be adjusted by operators)
        "Europe/Minsk"
    ];

    /// <summary>
    /// IANA timezone identifiers of popular vacation destinations for Western
    /// travellers. When <see cref="EnableTravelMode"/> is
    /// <see langword="true"/>, these carry a greatly reduced weight so that
    /// legitimate travellers are not falsely flagged.
    /// </summary>
    public HashSet<string> VacationTimezones { get; set; } =
    [
        "Europe/Istanbul",    "Africa/Casablanca",  "Africa/Tunis",
        "Africa/Cairo",       "Asia/Bangkok",        "Asia/Jakarta",
        "Asia/Manila",        "Asia/Ho_Chi_Minh",   "America/Mexico_City",
        "America/Havana",     "Asia/Dubai",          "Asia/Tokyo",
        "Asia/Singapore",     "Africa/Johannesburg", "Africa/Nairobi",
        "Indian/Mauritius",   "Asia/Nicosia",        "Europe/Athens",
        "Europe/Zagreb",      "Europe/Valletta"
    ];

    // ── Language set ───────────────────────────────────────────────────────

    /// <summary>
    /// BCP-47 primary language sub-tags considered non-Western.
    /// Matched against <see cref="Models.ClientFingerprintData.BrowserLanguages"/>
    /// and the <c>Accept-Language</c> header.
    /// </summary>
    public HashSet<string> NonWesternLanguageCodes { get; set; } =
    [
        // Slavic Cyrillic
        "ru", "uk", "be", "bg", "mk", "sr", "bs",
        // CJK
        "zh", "ja", "ko",
        // Arabic script
        "ar", "fa", "ur", "ps",
        // Indic scripts
        "hi", "mr", "ne", "bn", "gu", "pa", "ta", "te", "kn", "ml",
        // Central Asian
        "kk", "ky", "uz", "tk", "mn", "tg",
        // Hebrew
        "he", "yi",
        // African scripts
        "am", "ti", "so"
    ];

    // ── Thresholds ─────────────────────────────────────────────────────────

    /// <summary>
    /// Suspicion score at which the verdict changes from
    /// <see cref="Models.FraudVerdict.Clean"/> to
    /// <see cref="Models.FraudVerdict.Suspicious"/>.
    /// Default: <c>30</c>.
    /// </summary>
    public int MediumSuspicionThreshold { get; set; } = 30;

    /// <summary>
    /// Suspicion score at which the verdict changes from
    /// <see cref="Models.FraudVerdict.Suspicious"/> to
    /// <see cref="Models.FraudVerdict.HighlySuspicious"/>.
    /// Default: <c>65</c>.
    /// </summary>
    public int HighSuspicionThreshold { get; set; } = 65;

    /// <summary>
    /// Suspicion score at which the verdict changes from
    /// <see cref="Models.FraudVerdict.HighlySuspicious"/> to
    /// <see cref="Models.FraudVerdict.FakeWestern"/>.
    /// Default: <c>85</c>.
    /// </summary>
    public int FakeWesternThreshold { get; set; } = 85;

    // ── Feature toggles ────────────────────────────────────────────────────

    /// <summary>
    /// When <see langword="true"/>, users in <see cref="KnownVacationCountries"/>
    /// and <see cref="VacationTimezones"/> receive a substantially lower penalty,
    /// supporting legitimate holiday scenarios (e.g. a Dutch user in Turkey).
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
