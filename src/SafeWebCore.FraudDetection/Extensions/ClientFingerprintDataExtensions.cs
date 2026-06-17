using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Infrastructure;
using SafeWebCore.FraudDetection.Models;

namespace SafeWebCore.FraudDetection.Extensions;

/// <summary>
/// Extension methods for enriching <see cref="ClientFingerprintData"/> with geographic information.
/// </summary>
/// <remarks>
/// <para>
/// <b>Recommended pattern:</b> Enrich fingerprint data as early as possible (e.g. in middleware,
/// an endpoint filter, or a dedicated service) before passing it to
/// <see cref="Abstractions.IFraudDetector.Analyze"/>.
/// </para>
/// 
/// <para>
/// This keeps the fraud detector focused purely on analysis (SRP) and makes the data flow explicit.
/// </para>
/// </remarks>
public static class ClientFingerprintDataExtensions
{
    /// <summary>
    /// Enriches this <see cref="ClientFingerprintData"/> with country code and timezone
    /// resolved from the IP address using the provided <see cref="IGeoIpService"/>.
    /// </summary>
    /// <param name="data">The fingerprint data to enrich.</param>
    /// <param name="geoIpService">
    /// The geo-IP service to use for lookup. If <see langword="null"/>, the original data is returned.
    /// </param>
    /// <returns>
    /// A new (or the same) <see cref="ClientFingerprintData"/> record with
    /// <see cref="ClientFingerprintData.ResolvedCountryCode"/> and/or
    /// <see cref="ClientFingerprintData.SystemTimezone"/> populated when a lookup was performed.
    /// </returns>
    /// <example>
    /// <code>
    /// var fingerprint = new ClientFingerprintData
    /// {
    ///     IpAddress = context.Connection.RemoteIpAddress?.ToString(),
    ///     // ... other signals from client
    /// };
    ///
    /// // Preferred: enrich before analysis
    /// if (geoIpService is not null)
    ///     fingerprint = fingerprint.EnrichGeoIp(geoIpService);
    ///
    /// var report = detector.Analyze(fingerprint);
    /// </code>
    /// </example>
    public static ClientFingerprintData EnrichGeoIp(
        this ClientFingerprintData data,
        IGeoIpService? geoIpService)
    {
        return GeoIpEnricher.Enrich(data, geoIpService);
    }
}
