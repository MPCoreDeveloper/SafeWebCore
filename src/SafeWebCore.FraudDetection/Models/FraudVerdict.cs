namespace SafeWebCore.FraudDetection.Models;

/// <summary>
/// Overall fraud verdict derived from the computed suspicion score and
/// the thresholds configured in <c>WesternDetectorOptions</c>.
/// </summary>
public enum FraudVerdict
{
    /// <summary>
    /// No meaningful suspicious signals detected.
    /// Score is below <c>WesternDetectorOptions.MediumSuspicionThreshold</c>.
    /// </summary>
    Clean,

    /// <summary>
    /// Some inconsistencies detected that warrant passive monitoring.
    /// Score is at or above <c>MediumSuspicionThreshold</c> but below
    /// <c>HighSuspicionThreshold</c>.
    /// </summary>
    Suspicious,

    /// <summary>
    /// Multiple strong inconsistencies detected. Score is at or above
    /// <c>HighSuspicionThreshold</c> but below <c>FakeWesternThreshold</c>.
    /// </summary>
    HighlySuspicious,

    /// <summary>
    /// Strong evidence of Western impersonation. Score is at or above
    /// <c>FakeWesternThreshold</c> (default 85).
    /// </summary>
    FakeWestern
}
