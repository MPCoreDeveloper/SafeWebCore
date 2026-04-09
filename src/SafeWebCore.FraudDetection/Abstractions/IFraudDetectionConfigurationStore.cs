using Microsoft.Extensions.Primitives;
using SafeWebCore.FraudDetection.Options;

namespace SafeWebCore.FraudDetection.Abstractions;

/// <summary>
/// Optional runtime configuration store used to load fraud-detection settings
/// from external sources such as a database.
/// </summary>
/// <remarks>
/// When registered, this store is queried at analysis time and takes precedence
/// over configuration-file options for the targeted tenant.
/// </remarks>
public interface IFraudDetectionConfigurationStore
{
    /// <summary>
    /// Retrieves the latest fraud-detection options for a tenant.
    /// </summary>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <returns>
    /// A tenant-specific options object or <see langword="null"/> when no
    /// override exists and the detector should fall back to options pattern data.
    /// </returns>
    FraudDetectionOptions? GetOptions(string? tenantId);

    /// <summary>
    /// Gets a change token that signals when external configuration changes.
    /// </summary>
    /// <returns>A change token used for runtime invalidation.</returns>
    IChangeToken GetReloadToken();
}
