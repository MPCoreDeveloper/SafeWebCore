namespace SafeWebCore.FraudDetection.Models;

/// <summary>
/// Result of a fraud analysis produced by <see cref="Abstractions.IFraudDetector.Analyze"/>.
/// </summary>
public sealed record FraudReport
{
    /// <summary>
    /// <see langword="true"/> when the evidence strongly indicates the user is
    /// impersonating an identity from the expected/primary region (neutral property).
    /// 
    /// This is set when <see cref="Verdict"/> is <see cref="FraudVerdict.RegionImpersonation"/>.
    /// Recommended for new code that uses the multi-region <see cref="Options.GeoCulturalConsistencyOptions"/>.
    /// </summary>
    public bool IsRegionImpersonation { get; init; }

    /// <summary>
    /// <see langword="true"/> when the resolved IP country is outside the
    /// configured expected countries (neutral property).
    /// 
    /// Used with <see cref="Options.GeoCulturalConsistencyOptions.ExpectedCountries"/>.
    /// </summary>
    public bool IsNotInExpectedRegion { get; init; }

    /// <summary>
    /// <see langword="true"/> when the evidence strongly indicates the user is
    /// impersonating a Western identity while originating from a non-Western region.
    /// 
    /// Preserved for 100% backward compatibility.
    /// New code should prefer <see cref="IsRegionImpersonation"/>.
    /// Set when <see cref="Verdict"/> is <see cref="FraudVerdict.FakeWestern"/> (alias for RegionImpersonation).
    /// </summary>
    public bool IsFakeWestern { get; init; }

    /// <summary>
    /// <see langword="true"/> when the resolved IP country is outside the
    /// configured allowed Western countries.
    /// 
    /// Preserved for backward compatibility.
    /// New code should prefer <see cref="IsNotInExpectedRegion"/>.
    /// </summary>
    public bool IsNotInWesternCountry { get; init; }

    /// <summary>
    /// Aggregated suspicion score in the range <c>0</c> (clean) to <c>100</c>
    /// (certain impersonation). Scores are capped at 100 regardless of how many
    /// signals fire simultaneously.
    /// </summary>
    public int SuspicionScore { get; init; }

    /// <summary>
    /// Individual trigger keys that contributed to the suspicion score.
    /// See <see cref="FraudTrigger"/> for the full set of well-known values.
    /// </summary>
    public IReadOnlyList<string> Triggers { get; init; } = [];

    /// <summary>
    /// Overall verdict derived from <see cref="SuspicionScore"/> and the
    /// configured thresholds in the active detector options
    /// (<see cref="Options.GeoCulturalConsistencyOptions"/> or legacy Western options).
    /// </summary>
    public FraudVerdict Verdict { get; init; }

    /// <summary>
    /// Recommended action for the application. Prefer soft mitigations
    /// (<see cref="RecommendedAction.Monitor"/>,
    /// <see cref="RecommendedAction.StepUpAuthentication"/>) over hard
    /// blocking wherever possible.
    /// </summary>
    public RecommendedAction RecommendedAction { get; init; }

    /// <summary>
    /// <see langword="true"/> when scanner heuristics indicate a penetration-test tool
    /// such as OWASP ZAP, Burp Suite, or Tenable/Nessus.
    /// </summary>
    public bool IsPenTestScannerDetected { get; init; }

    /// <summary>
    /// <see langword="true"/> when the configured authorization bypass header was
    /// present and valid for the request.
    /// </summary>
    public bool IsAuthorizedPenTest { get; init; }

    /// <summary>
    /// <see langword="true"/> when all detections were bypassed due to a valid
    /// authorized penetration-test header.
    /// </summary>
    public bool IsDetectionBypassed { get; init; }

    /// <summary>
    /// <see langword="true"/> when an authorization-check email notification was
    /// sent for a detected scanner request.
    /// </summary>
    public bool PenTestAuthorizationEmailSent { get; init; }

    /// <summary>
    /// Effective tenant identifier used during analysis.
    /// </summary>
    public string? TenantId { get; init; }
}
