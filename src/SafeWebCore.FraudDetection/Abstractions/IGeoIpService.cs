namespace SafeWebCore.FraudDetection.Abstractions;

/// <summary>
/// Optional abstraction for resolving geographic information (country + timezone)
/// from a client IP address.
/// </summary>
/// <remarks>
/// <para>
/// <b>This service is optional.</b> The fraud detection module works perfectly well
/// without it. You only need to implement <see cref="IGeoIpService"/> if you want the
/// detectors to automatically enrich <see cref="Models.ClientFingerprintData"/> when
/// only an IP address is available.
/// </para>
/// 
/// <para>
/// <b>Recommended approach (preferred):</b><br/>
/// Enrich <see cref="Models.ClientFingerprintData.ResolvedCountryCode"/> and
/// <see cref="Models.ClientFingerprintData.SystemTimezone"/> yourself as early as possible
/// (e.g. in middleware or a request pipeline) using your own geo-IP provider, then pass the
/// already-enriched fingerprint to <see cref="Abstractions.IFraudDetector.Analyze"/>.
/// 
/// This keeps concerns separated: you control when and how geo resolution happens,
/// and the detector stays focused on analysis only.
/// </para>
/// 
/// <para>
/// <b>Convenience fallback:</b><br/>
/// If you register an <see cref="IGeoIpService"/>, the detectors will automatically
/// call it (via <see cref="Infrastructure.GeoIpEnricher"/>) as a last resort when:
/// <list type="bullet">
///   <item><description><see cref="Models.ClientFingerprintData.IpAddress"/> is present, and</description></item>
///   <item><description><see cref="Models.ClientFingerprintData.ResolvedCountryCode"/> or <see cref="Models.ClientFingerprintData.SystemTimezone"/> is still <see langword="null"/>.</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// You can also use the extension method
/// <c>ClientFingerprintData.EnrichGeoIp(IGeoIpService?)</c> to explicitly enrich data
/// before analysis.
/// </para>
/// </remarks>
/// <example>
/// Implementing a simple wrapper around MaxMind or a similar provider:
/// <code>
/// public sealed class MaxMindGeoIpService : IGeoIpService
/// {
///     private readonly DatabaseReader _reader;
///
///     public MaxMindGeoIpService(DatabaseReader reader) => _reader = reader;
///
///     public string? GetCountryCode(string ipAddress)
///         => _reader.TryCountry(ipAddress, out var response)
///             ? response.Country.IsoCode
///             : null;
///
///     public string? GetTimezone(string ipAddress)
///         => _reader.TryCity(ipAddress, out var response)
///             ? response.Location.TimeZone
///             : null;
/// }
/// </code>
/// </example>
public interface IGeoIpService
{
    /// <summary>
    /// Resolves the ISO 3166-1 alpha-2 country code for the given IP address.
    /// </summary>
    /// <param name="ipAddress">IPv4 or IPv6 address.</param>
    /// <returns>Two-letter country code (e.g. "NL", "US", "AE"), or <see langword="null"/> if unknown.</returns>
    string? GetCountryCode(string ipAddress);

    /// <summary>
    /// Resolves the IANA timezone identifier for the given IP address.
    /// </summary>
    /// <param name="ipAddress">IPv4 or IPv6 address.</param>
    /// <returns>IANA timezone (e.g. "Europe/Amsterdam", "Asia/Dubai"), or <see langword="null"/> if unknown.</returns>
    string? GetTimezone(string ipAddress);
}
