namespace SafeWebCore.FraudDetection.Options;

/// <summary>
/// Main container for SafeWebCore fraud-detection configuration.
/// </summary>
/// <remarks>
/// Bind this type from configuration using the options pattern (for example
/// from <c>SafeWebCore:FraudDetection</c>) and optionally override it at runtime
/// through <see cref="Abstractions.IFraudDetectionConfigurationStore"/>.
/// 
/// <para>
/// <b>Multi-region / neutral detection (recommended for new usage):</b>
/// Use <see cref="EnableGeoCulturalConsistency"/> + <see cref="GeoCulturalConsistency"/>.
/// This allows protecting any primary region (Western, Gulf/Arabic, Russian/CIS, African, East-Asian, etc.)
/// by configuring expected countries, inconsistent timezones and languages.
/// </para>
/// 
/// <para>
/// <b>Legacy Western-only path:</b>
/// <see cref="EnableWesternImpersonation"/> + <see cref="WesternImpersonation"/> remain fully supported
/// for 100% backward compatibility. Existing configuration continues to work unchanged.
/// </para>
/// </remarks>
public sealed class FraudDetectionOptions
{
    /// <summary>
    /// Configuration section path used by default when binding from
    /// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.
    /// </summary>
    public const string DefaultSectionName = "SafeWebCore:FraudDetection";

    /// <summary>
    /// Enables Western impersonation detection (legacy path).
    /// 
    /// For new projects or when protecting non-Western primary regions,
    /// prefer <see cref="EnableGeoCulturalConsistency"/> instead.
    /// 
    /// Default: <see langword="true"/>.
    /// </summary>
    public bool EnableWesternImpersonation { get; set; } = true;

    /// <summary>
    /// Enables the neutral, multi-region geo-cultural consistency detector.
    /// 
    /// When enabled, the detector uses <see cref="GeoCulturalConsistency"/> to determine
    /// what "expected" vs "inconsistent" signals look like for your primary audience.
    /// 
    /// This is the recommended approach for new configurations because it is not
    /// tied to any specific culture or region.
    /// 
    /// Default: <see langword="false"/> (to keep legacy behavior for existing users).
    /// </summary>
    public bool EnableGeoCulturalConsistency { get; set; }

    /// <summary>
    /// Enables penetration-test and scanner detection.
    /// Default: <see langword="true"/>.
    /// </summary>
    public bool EnablePenTestDetection { get; set; } = true;

    /// <summary>
    /// Western impersonation detector settings (legacy).
    /// Used when <see cref="EnableWesternImpersonation"/> is true.
    /// </summary>
#pragma warning disable CS0618
    public WesternDetectorOptions WesternImpersonation { get; set; } = new();
#pragma warning restore CS0618

    /// <summary>
    /// Neutral geo-cultural consistency settings.
    /// Used when <see cref="EnableGeoCulturalConsistency"/> is true.
    /// 
    /// Configure this for whichever primary region your service serves.
    /// </summary>
    public GeoCulturalConsistencyOptions GeoCulturalConsistency { get; set; } = new();

    /// <summary>
    /// Penetration-test and scanner detection settings.
    /// </summary>
    public PenTestDetectionOptions PenTestDetection { get; set; } = new();
}

#pragma warning restore CS0618
