namespace SafeWebCore.FraudDetection.Models;

/// <summary>
/// Overall fraud verdict derived from the computed suspicion score and
/// the thresholds configured in the active detector options
/// (<see cref="Options.GeoCulturalConsistencyOptions"/> or legacy <c>WesternDetectorOptions</c>).
/// 
/// <para>
/// <b>Recommended:</b> <see cref="RegionImpersonation"/> — neutral name that works for any primary region.
/// </para>
/// <para>
/// <b>Legacy:</b> <see cref="FakeWestern"/> is preserved as an alias for full backward compatibility.
/// </para>
/// </summary>
public enum FraudVerdict
{
    /// <summary>
    /// No meaningful suspicious signals detected.
    /// Score is below the medium suspicion threshold.
    /// </summary>
    Clean,

    /// <summary>
    /// Some inconsistencies detected that warrant passive monitoring.
    /// Score is at or above the medium threshold but below the high threshold.
    /// </summary>
    Suspicious,

    /// <summary>
    /// Multiple strong inconsistencies detected. Score is at or above
    /// the high threshold but below the high-inconsistency / impersonation threshold.
    /// </summary>
    HighlySuspicious,

    /// <summary>
    /// Strong evidence of region impersonation (neutral verdict).
    /// Score is at or above the high inconsistency threshold.
    /// 
    /// This is the recommended verdict name going forward.
    /// </summary>
    RegionImpersonation,

    /// <summary>
    /// Strong evidence of Western impersonation.
    /// Preserved for full backward compatibility.
    /// New code should prefer <see cref="RegionImpersonation"/>.
    /// </summary>
    [System.Obsolete("Use RegionImpersonation instead for new multi-region scenarios.")]
    FakeWestern = RegionImpersonation
}
