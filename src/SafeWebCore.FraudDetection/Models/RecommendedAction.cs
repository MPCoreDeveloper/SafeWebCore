namespace SafeWebCore.FraudDetection.Models;

/// <summary>
/// Recommended action for the application to take based on the fraud assessment.
/// Prefer soft mitigations (<see cref="Monitor"/>, <see cref="StepUpAuthentication"/>)
/// before resorting to hard blocking.
/// </summary>
public enum RecommendedAction
{
    /// <summary>No action needed. The user appears to be legitimate.</summary>
    NoAction,

    /// <summary>
    /// Log and silently monitor the session. No interruption to the user experience.
    /// Corresponds to <see cref="FraudVerdict.Suspicious"/>.
    /// </summary>
    Monitor,

    /// <summary>
    /// Request an additional verification step such as 2FA, a CAPTCHA, or an
    /// identity confirmation prompt before granting access to sensitive resources.
    /// Corresponds to <see cref="FraudVerdict.HighlySuspicious"/>.
    /// </summary>
    StepUpAuthentication,

    /// <summary>
    /// Block the request entirely. Reserved for the highest-confidence
    /// impersonation signals (<see cref="FraudVerdict.FakeWestern"/>).
    /// </summary>
    BlockRequest
}
