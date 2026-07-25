namespace SafeWebCore.FraudDetection.Models;

/// <summary>
/// Qualitative risk classification derived from the numeric suspicion score.
/// This is additive metadata and does not replace <see cref="FraudVerdict"/> or the raw <c>SuspicionScore</c>.
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Low risk. No meaningful suspicious signals (corresponds to <see cref="FraudVerdict.Clean"/>).
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium risk. Some inconsistencies detected (corresponds to <see cref="FraudVerdict.Suspicious"/>).
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High risk. Multiple strong inconsistencies (corresponds to <see cref="FraudVerdict.HighlySuspicious"/>).
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical risk — strong evidence of impersonation (corresponds to <see cref="FraudVerdict.RegionImpersonation"/>).
    /// </summary>
    Critical = 3
}
