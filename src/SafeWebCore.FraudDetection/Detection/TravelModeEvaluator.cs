using SafeWebCore.FraudDetection.Models;
using SafeWebCore.FraudDetection.Options;

namespace SafeWebCore.FraudDetection.Detection;

/// <summary>
/// Post-processes the raw suspicion score by applying a reduction factor when
/// the detected signal pattern is consistent with a legitimate Western traveller
/// rather than a VPN-based impersonation attempt.
/// </summary>
/// <remarks>
/// A pattern is considered consistent travel when the device uses a Western
/// primary language (e.g. Dutch, German, French) but the IP or timezone matches
/// a known vacation destination. This exemption is disabled when
/// <see cref="WesternDetectorOptions.EnableTravelMode"/> is
/// <see langword="false"/>.
/// </remarks>
internal sealed class TravelModeEvaluator(WesternDetectorOptions options)
{
    // Score is multiplied by this factor when a legitimate travel pattern is detected.
    private const float LegitTravelMultiplier = 0.55f;

    /// <summary>
    /// Returns the adjusted score. When the signal combination is consistent
    /// with genuine travel the raw score is reduced by ~45 %; otherwise the
    /// raw score is returned unchanged.
    /// </summary>
    internal int AdjustScore(int rawScore, ClientFingerprintData data)
    {
        if (!options.EnableTravelMode || rawScore == 0)
            return rawScore;

        if (!IsLikelyLegitimateTravel(data))
            return rawScore;

        return (int)(rawScore * LegitTravelMultiplier);
    }

    // ── Travel-pattern detection ───────────────────────────────────────────

    /// <summary>
    /// A travel pattern is confirmed when the device reports a Western primary
    /// language AND is located in either a known vacation country or a known
    /// vacation timezone.
    /// </summary>
    private bool IsLikelyLegitimateTravel(ClientFingerprintData data)
    {
        if (!HasWesternPrimaryLanguage(data))
            return false;

        bool inVacationCountry =
            !string.IsNullOrWhiteSpace(data.ResolvedCountryCode) &&
            options.KnownVacationCountries.Contains(data.ResolvedCountryCode);

        bool inVacationTimezone =
            !string.IsNullOrWhiteSpace(data.SystemTimezone) &&
            options.VacationTimezones.Contains(data.SystemTimezone);

        return inVacationCountry || inVacationTimezone;
    }

    private bool HasWesternPrimaryLanguage(ClientFingerprintData data)
    {
        foreach (var lang in data.BrowserLanguages)
        {
            var primary = lang.Split('-', 2)[0].ToLowerInvariant().Trim();
            if (!options.NonWesternLanguageCodes.Contains(primary))
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
                if (!options.NonWesternLanguageCodes.Contains(primary))
                    return true;
            }
        }

        return false;
    }
}
