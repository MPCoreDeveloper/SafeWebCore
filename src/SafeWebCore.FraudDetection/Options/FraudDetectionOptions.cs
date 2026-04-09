namespace SafeWebCore.FraudDetection.Options;

/// <summary>
/// Main container for SafeWebCore fraud-detection configuration.
/// </summary>
/// <remarks>
/// Bind this type from configuration using the options pattern (for example
/// from <c>SafeWebCore:FraudDetection</c>) and optionally override it at runtime
/// through <see cref="Abstractions.IFraudDetectionConfigurationStore"/>.
/// </remarks>
public sealed class FraudDetectionOptions
{
    /// <summary>
    /// Configuration section path used by default when binding from
    /// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.
    /// </summary>
    public const string DefaultSectionName = "SafeWebCore:FraudDetection";

    /// <summary>
    /// Enables Western impersonation detection.
    /// Default: <see langword="true"/>.
    /// </summary>
    public bool EnableWesternImpersonation { get; set; } = true;

    /// <summary>
    /// Enables penetration-test and scanner detection.
    /// Default: <see langword="true"/>.
    /// </summary>
    public bool EnablePenTestDetection { get; set; } = true;

    /// <summary>
    /// Western impersonation detector settings.
    /// </summary>
    public WesternDetectorOptions WesternImpersonation { get; set; } = new();

    /// <summary>
    /// Penetration-test and scanner detection settings.
    /// </summary>
    public PenTestDetectionOptions PenTestDetection { get; set; } = new();
}
