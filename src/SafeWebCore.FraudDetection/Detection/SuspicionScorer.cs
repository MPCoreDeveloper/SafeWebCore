using SafeWebCore.FraudDetection.Models;
using SafeWebCore.FraudDetection.Options;

namespace SafeWebCore.FraudDetection.Detection;

/// <summary>
/// Internal engine that evaluates <see cref="ClientFingerprintData"/> and
/// produces a raw suspicion score together with the individual trigger keys
/// that contributed to it.
/// </summary>
/// <remarks>
/// Score weights are intentionally internal constants. Administrators tune the
/// response to those scores via detector options thresholds,
/// not by altering individual weights.
/// 
/// Supports both legacy WesternDetectorOptions and the neutral GeoCulturalConsistencyOptions.
/// </remarks>
internal sealed class SuspicionScorer
{
    private readonly HashSet<string> _expectedCountries;
    private readonly HashSet<string> _travelCountries;
    private readonly HashSet<string> _inconsistentTimezones;
    private readonly HashSet<string> _travelTimezones;
    private readonly HashSet<string> _inconsistentLanguages;
    private readonly bool _enableTravelMode;
    private readonly bool _enableDeviceFingerprinting;

    /// <summary>
    /// Creates a scorer from legacy Western-centric options (for backward compatibility).
    /// </summary>
#pragma warning disable CS0618
    public SuspicionScorer(WesternDetectorOptions options)
#pragma warning restore CS0618
        : this(
            options?.AllowedCountries ?? [],
            options?.KnownVacationCountries ?? [],
            options?.SuspiciousTimezones ?? [],
            options?.VacationTimezones ?? [],
            options?.NonWesternLanguageCodes ?? [],
            options?.EnableTravelMode ?? true,
            options?.EnableDeviceFingerprinting ?? true)
    {
    }

    /// <summary>
    /// Creates a scorer from the neutral geo-cultural options.
    /// </summary>
    public SuspicionScorer(GeoCulturalConsistencyOptions options)
        : this(
            options?.ExpectedCountries ?? [],
            options?.KnownTravelCountries ?? [],
            options?.InconsistentTimezones ?? [],
            options?.TravelTimezones ?? [],
            options?.InconsistentLanguageCodes ?? [],
            options?.EnableTravelMode ?? true,
            options?.EnableDeviceFingerprinting ?? true)
    {
    }

    private SuspicionScorer(
        HashSet<string> expectedCountries,
        HashSet<string> travelCountries,
        HashSet<string> inconsistentTimezones,
        HashSet<string> travelTimezones,
        HashSet<string> inconsistentLanguages,
        bool enableTravelMode,
        bool enableDeviceFingerprinting)
    {
        _expectedCountries = expectedCountries;
        _travelCountries = travelCountries;
        _inconsistentTimezones = inconsistentTimezones;
        _travelTimezones = travelTimezones;
        _inconsistentLanguages = inconsistentLanguages;
        _enableTravelMode = enableTravelMode;
        _enableDeviceFingerprinting = enableDeviceFingerprinting;
    }

    // ── Per-signal score weights ───────────────────────────────────────────

    private const int IpNonWesternScore          = 20;
    private const int IpVacationScore            = 5;
    private const int TimezoneNonWesternScore    = 25;
    private const int TimezoneVacationScore      = 5;
    private const int BrowserLanguageScore       = 15;
    private const int AcceptLanguageScore        = 10;
    private const int FontCyrillicScore          = 20;
    private const int FontArabicScore            = 20;
    private const int FontCjkScore               = 15;
    private const int FontDevanagariScore        = 15;

    // Inconsistency bonuses for expected-region IP + inconsistent other signals.
    private const int FullInconsistencyBonus     = 30;
    private const int TimezoneOnlyBonus          = 15;
    private const int LanguageOnlyBonus          = 5;

    private const int MaxScore = 100;

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates all configured signals and returns a raw suspicion score capped at 100
    /// along with the individual trigger keys.
    /// </summary>
    internal (int Score, List<string> Triggers) Evaluate(ClientFingerprintData data)
    {
        List<string> triggers = [];
        int score = 0;

        bool ipIsExpected           = EvaluateIpCountry(data, triggers, ref score);
        bool timezoneIsInconsistent = EvaluateTimezone(data, triggers, ref score);
        bool languageIsInconsistent = EvaluateLanguages(data, triggers, ref score);

        if (_enableDeviceFingerprinting)
            EvaluateFontSupport(data, triggers, ref score);

        ApplyInconsistencyBonus(ipIsExpected, timezoneIsInconsistent, languageIsInconsistent, triggers, ref score);

        return (Math.Min(score, MaxScore), triggers);
    }

    // ── Signal evaluators ──────────────────────────────────────────────────

    private bool EvaluateIpCountry(ClientFingerprintData data, List<string> triggers, ref int score)
    {
        var country = data.ResolvedCountryCode;
        if (string.IsNullOrWhiteSpace(country))
            return false;

        if (_expectedCountries.Contains(country))
            return true;

        if (_enableTravelMode && _travelCountries.Contains(country))
        {
            score += IpVacationScore;
            triggers.Add(FraudTrigger.IpVacationCountry);
        }
        else
        {
            score += IpNonWesternScore;
            triggers.Add(FraudTrigger.IpNonWestern);
        }

        return false;
    }

    private bool EvaluateTimezone(ClientFingerprintData data, List<string> triggers, ref int score)
    {
        var tz = data.SystemTimezone;
        if (string.IsNullOrWhiteSpace(tz))
            return false;

        if (_inconsistentTimezones.Contains(tz))
        {
            score += TimezoneNonWesternScore;
            triggers.Add(FraudTrigger.TimezoneNonWestern);
            return true;
        }

        if (_enableTravelMode && _travelTimezones.Contains(tz))
        {
            score += TimezoneVacationScore;
            triggers.Add(FraudTrigger.TimezoneVacation);
        }

        return false;
    }

    /// <returns>
    /// <see langword="true"/> when any non-Western language is detected.
    /// </returns>
    private bool EvaluateLanguages(ClientFingerprintData data, List<string> triggers, ref int score)
    {
        bool found = false;

        // navigator.languages — ordered by user preference
        foreach (var lang in data.BrowserLanguages)
        {
            if (!IsNonWesternLanguage(lang))
                continue;

            score += BrowserLanguageScore;
            triggers.Add(FraudTrigger.BrowserLanguageNonWestern);
            found = true;
            break; // one hit is sufficient from this source
        }

        // Accept-Language HTTP header
        if (!string.IsNullOrWhiteSpace(data.AcceptLanguage))
        {
            foreach (var lang in ParseAcceptLanguage(data.AcceptLanguage))
            {
                if (!IsNonWesternLanguage(lang))
                    continue;

                score += AcceptLanguageScore;
                triggers.Add(FraudTrigger.AcceptLanguageNonWestern);
                found = true;
                break;
            }
        }

        return found;
    }

    private static void EvaluateFontSupport(ClientFingerprintData data, List<string> triggers, ref int score)
    {
        if (data.HasCyrillicFontSupport)
        {
            score += FontCyrillicScore;
            triggers.Add(FraudTrigger.FontCyrillic);
        }

        if (data.HasArabicFontSupport)
        {
            score += FontArabicScore;
            triggers.Add(FraudTrigger.FontArabic);
        }

        if (data.HasCjkFontSupport)
        {
            score += FontCjkScore;
            triggers.Add(FraudTrigger.FontCjk);
        }

        if (data.HasDevanagariSupport)
        {
            score += FontDevanagariScore;
            triggers.Add(FraudTrigger.FontDevanagari);
        }
    }

    /// <summary>
    /// Applies a cross-signal inconsistency bonus when a Western IP coexists
    /// with non-Western timezone and/or language signals.
    /// A VPN user with a Russian timezone and Russian browser language receives
    /// the full <c>FullInconsistencyBonus</c>; language-only mismatches (which
    /// can occur legitimately in immigrant communities) receive a much smaller bonus.
    /// </summary>
    private static void ApplyInconsistencyBonus(
        bool ipIsWestern,
        bool timezoneIsNonWestern,
        bool languageIsNonWestern,
        List<string> triggers,
        ref int score)
    {
        if (!ipIsWestern)
            return;

        if (timezoneIsNonWestern && languageIsNonWestern)
        {
            score += FullInconsistencyBonus;
            triggers.Add(FraudTrigger.InconsistencyIpLanguageAndTimezone);
        }
        else if (timezoneIsNonWestern)
        {
            score += TimezoneOnlyBonus;
            triggers.Add(FraudTrigger.InconsistencyIpTimezone);
        }
        else if (languageIsNonWestern)
        {
            // Deliberately low: immigrant / multicultural users have Western IPs
            // but non-Western browser languages.
            score += LanguageOnlyBonus;
            triggers.Add(FraudTrigger.InconsistencyIpLanguageOnly);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private bool IsNonWesternLanguage(string langTag)
    {
        // "ru-RU" → "ru",  "zh-Hans-CN" → "zh"
        var primary = langTag.Split('-', 2)[0].ToLowerInvariant().Trim();
        return _inconsistentLanguages.Contains(primary);
    }

    private static IEnumerable<string> ParseAcceptLanguage(string headerValue)
    {
        // "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7" → ["ru-RU", "ru", "en-US", "en"]
        foreach (var segment in headerValue.Split(','))
        {
            var lang = segment.Split(';')[0].Trim();
            if (!string.IsNullOrEmpty(lang))
                yield return lang;
        }
    }
}
