using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Models;

namespace SafeWebCore.FraudDetection.Infrastructure;

/// <summary>
/// Internal helper that encapsulates IP-based geo enrichment logic.
/// 
/// <para>
/// This class exists to keep enrichment responsibility out of the detectors
/// (SRP). Both <see cref="Detection.WesternImpersonationDetector"/> and
/// <see cref="Detection.GeoCulturalConsistencyDetector"/> delegate to this
/// helper when an <see cref="IGeoIpService"/> is registered.
/// </para>
/// 
/// <para>
/// <b>Recommended usage:</b> Prefer enriching <see cref="ClientFingerprintData"/>
/// yourself (via ClientFingerprintDataExtensions.EnrichGeoIp)
/// before calling <see cref="Abstractions.IFraudDetector.Analyze"/>. This keeps the
/// detector focused purely on analysis.
/// </para>
/// </summary>
internal static class GeoIpEnricher
{
    /// <summary>
    /// Enriches the given fingerprint with country code and timezone resolved from
    /// the IP address, if an <see cref="IGeoIpService"/> is available and the data
    /// is not already pre-populated.
    /// </summary>
    /// <param name="data">The client fingerprint data to enrich.</param>
    /// <param name="geoIpService">
    /// Optional geo-IP service. When <see langword="null"/>, the original data is returned unchanged.
    /// </param>
    /// <returns>
    /// A (possibly new) <see cref="ClientFingerprintData"/> record with
    /// <see cref="ClientFingerprintData.ResolvedCountryCode"/> and/or
    /// <see cref="ClientFingerprintData.SystemTimezone"/> filled in when a lookup occurred.
    /// </returns>
    public static ClientFingerprintData Enrich(
        ClientFingerprintData data,
        IGeoIpService? geoIpService)
    {
        if (geoIpService is null || string.IsNullOrWhiteSpace(data.IpAddress))
            return data;

        var country = data.ResolvedCountryCode ?? geoIpService.GetCountryCode(data.IpAddress);
        var timezone = data.SystemTimezone ?? geoIpService.GetTimezone(data.IpAddress);

        if (country == data.ResolvedCountryCode && timezone == data.SystemTimezone)
            return data;

        return data with
        {
            ResolvedCountryCode = country,
            SystemTimezone = timezone
        };
    }
}
