using SafeWebCore.FraudDetection.Models;
using SafeWebCore.FraudDetection.Options;

namespace SafeWebCore.FraudDetection.Detection;

/// <summary>
/// Post-processes the raw suspicion score by applying a reduction factor when
/// the detected signal pattern is consistent with a legitimate traveller
/// rather than a region-impersonation attempt.
/// </summary>
/// <remarks>
/// A pattern is considered consistent travel when the device uses a language
/// that is not marked as inconsistent for the expected region, but the IP or timezone
/// matches a known travel destination. This helps reduce false positives for real travellers.
/// The exemption is disabled when <see cref="GeoCulturalConsistencyOptions.EnableTravelMode"/>
/// (or the legacy equivalent) is <see langword="false"/>.
/// </remarks>
internal sealed class TravelModeEvaluator
{
    private readonly HashSet<string> _travelCountries;
    private readonly HashSet<string> _travelTimezones;
    private readonly HashSet<string> _inconsistentLanguages;
    private readonly bool _enableTravelMode;

    // Score is multiplied by this factor when a legitimate travel pattern is detected.
    private const float LegitTravelMultiplier = 0.55f;

    /// <summary>
    /// Creates evaluator from legacy Western-centric options.
    /// </summary>
#pragma warning disable CS0618
    public TravelModeEvaluator(WesternDetectorOptions options)
#pragma warning restore CS0618
        : this(
            options.KnownVacationCountries,
            options.VacationTimezones,
            options.NonWesternLanguageCodes,
            options.EnableTravelMode)
    {
    }

    /// <summary>
    /// Creates evaluator from neutral geo-cultural options.
    /// </summary>
    public TravelModeEvaluator(GeoCulturalConsistencyOptions options)
        : this(
            options.KnownTravelCountries,
            options.TravelTimezones,
            options.InconsistentLanguageCodes,
            options.EnableTravelMode)
    {
    }

    private TravelModeEvaluator(
        HashSet<string> travelCountries,
        HashSet<string> travelTimezones,
        HashSet<string> inconsistentLanguages,
        bool enableTravelMode)
    {
        _travelCountries = travelCountries;
        _travelTimezones = travelTimezones;
        _inconsistentLanguages = inconsistentLanguages;
        _enableTravelMode = enableTravelMode;
    }

    /// <summary>
    /// Returns the adjusted score. When the signal combination is consistent
    /// with genuine travel the raw score is reduced by ~45 %; otherwise the
    /// raw score is returned unchanged.
    /// </summary>
    internal int AdjustScore(int rawScore, ClientFingerprintData data)
    {
        if (!_enableTravelMode || rawScore == 0)
            return rawScore;

        if (!IsLikelyLegitimateTravel(data))
            return rawScore;

        return (int)(rawScore * LegitTravelMultiplier);
    }

    // ── Travel-pattern detection ───────────────────────────────────────────

    /// <summary>
    /// A travel pattern is confirmed when the device reports a language that is
    /// not marked as inconsistent AND is located in either a known travel country
    /// or a known travel timezone.
    /// </summary>
    private bool IsLikelyLegitimateTravel(ClientFingerprintData data)
    {
        if (!HasNonInconsistentLanguage(data))
            return false;

        bool inTravelCountry =
            !string.IsNullOrWhiteSpace(data.ResolvedCountryCode) &&
            _travelCountries.Contains(data.ResolvedCountryCode);

        bool inTravelTimezone =
            !string.IsNullOrWhiteSpace(data.SystemTimezone) &&
            _travelTimezones.Contains(data.SystemTimezone);

        return inTravelCountry || inTravelTimezone;
    }

    private bool HasNonInconsistentLanguage(ClientFingerprintData data)
    {
        foreach (var lang in data.BrowserLanguages)
        {
            var primary = lang.Split('-', 2)[0].ToLowerInvariant().Trim();
            if (!_inconsistentLanguages.Contains(primary))
                return true;
        }

        if (!string.IsNullOrWhiteSpace(data.AcceptLanguage))
        {
            foreach (var segment in data.AcceptLanguage.Split(','))
            {
                var lang = segment.Split(';')[0].Trim();
                if (string.IsNullOrEmpty(lang))
                    continue;

                var primary = lang.Split('-', 2)[0].ToLowerInvariant();
                if (!_inconsistentLanguages.Contains(primary))
                    return true;
            }
        }

        return false;
    }
}
