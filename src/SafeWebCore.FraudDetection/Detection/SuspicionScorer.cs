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
/// response to those scores via <see cref="WesternDetectorOptions"/> thresholds,
/// not by altering individual weights.
/// </remarks>
internal sealed class SuspicionScorer(WesternDetectorOptions options)
{
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

    // Inconsistency bonuses for Western-IP + non-Western-other combinations.
    // The strongest signal is Western IP + non-Western timezone + non-Western
    // language simultaneously (classic VPN fingerprint).
    private const int FullInconsistencyBonus     = 30;   // tz + lang mismatch
    private const int TimezoneOnlyBonus          = 15;   // tz mismatch only
    private const int LanguageOnlyBonus          = 5;    // lang mismatch only (weaker)

    private const int MaxScore = 100;

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Evaluates all configured signals and returns a raw score capped at
    /// <c>100</c> along with the list of fired trigger keys.
    /// </summary>
    internal (int Score, List<string> Triggers) Evaluate(ClientFingerprintData data)
    {
        List<string> triggers = [];
        int score = 0;

        bool ipIsWestern         = EvaluateIpCountry(data, triggers, ref score);
        bool timezoneIsNonWestern = EvaluateTimezone(data, triggers, ref score);
        bool languageIsNonWestern = EvaluateLanguages(data, triggers, ref score);

        if (options.EnableDeviceFingerprinting)
            EvaluateFontSupport(data, triggers, ref score);

        ApplyInconsistencyBonus(ipIsWestern, timezoneIsNonWestern, languageIsNonWestern, triggers, ref score);

        return (Math.Min(score, MaxScore), triggers);
    }

    // ── Signal evaluators ──────────────────────────────────────────────────

    /// <returns>
    /// <see langword="true"/> when the IP resolves to a confirmed Western country.
    /// </returns>
    private bool EvaluateIpCountry(ClientFingerprintData data, List<string> triggers, ref int score)
    {
        var country = data.ResolvedCountryCode;
        if (string.IsNullOrWhiteSpace(country))
            return false; // unknown — not penalized, but not confirmed Western either

        if (options.AllowedCountries.Contains(country))
            return true; // confirmed Western; no penalty

        if (options.EnableTravelMode && options.KnownVacationCountries.Contains(country))
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

    /// <returns>
    /// <see langword="true"/> when the timezone is in
    /// <see cref="WesternDetectorOptions.SuspiciousTimezones"/>.
    /// </returns>
    private bool EvaluateTimezone(ClientFingerprintData data, List<string> triggers, ref int score)
    {
        var tz = data.SystemTimezone;
        if (string.IsNullOrWhiteSpace(tz))
            return false;

        if (options.SuspiciousTimezones.Contains(tz))
        {
            score += TimezoneNonWesternScore;
            triggers.Add(FraudTrigger.TimezoneNonWestern);
            return true;
        }

        if (options.EnableTravelMode && options.VacationTimezones.Contains(tz))
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
        return options.NonWesternLanguageCodes.Contains(primary);
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
