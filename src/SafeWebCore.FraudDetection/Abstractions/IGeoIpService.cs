namespace SafeWebCore.FraudDetection.Abstractions;

/// <summary>
/// Optional service that resolves geographic information from an IP address.
/// </summary>
/// <remarks>
/// Register your own implementation to enable IP-based country and timezone
/// resolution inside <c>WesternImpersonationDetector</c>.
/// When no implementation is registered the detector relies exclusively on
/// signals pre-populated in <c>ClientFingerprintData</c>.
/// </remarks>
public interface IGeoIpService
{
    /// <summary>
    /// Resolves the ISO 3166-1 alpha-2 country code for the given
    /// <paramref name="ipAddress"/>, or <see langword="null"/> if unknown.
    /// </summary>
    /// <param name="ipAddress">IPv4 or IPv6 address to look up.</param>
    string? GetCountryCode(string ipAddress);

    /// <summary>
    /// Resolves the IANA timezone identifier for the given
    /// <paramref name="ipAddress"/>, or <see langword="null"/> if unknown.
    /// </summary>
    /// <param name="ipAddress">IPv4 or IPv6 address to look up.</param>
    string? GetTimezone(string ipAddress);
}
