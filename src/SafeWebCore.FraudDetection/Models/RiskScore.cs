using System;

namespace SafeWebCore.FraudDetection.Models;

/// <summary>
/// Structured, additive risk assessment for a fraud analysis result.
/// Consumers can use this for policy decisions, logging, metrics, or dashboards
/// without relying on the raw <see cref="FraudReport.SuspicionScore"/> or <see cref="FraudReport.Verdict"/>.
/// </summary>
/// <remarks>
/// This type is additive. Existing properties on <see cref="FraudReport"/> (SuspicionScore, Verdict, RecommendedAction, etc.)
/// remain unchanged in behavior and semantics.
/// </remarks>
public sealed record RiskScore
{
    /// <summary>
    /// A safe default instance representing no risk assessment (score 0, Low).
    /// </summary>
    public static RiskScore None { get; } = new() { Score = 0, Level = RiskLevel.Low };

    /// <summary>
    /// The numeric suspicion score (0–100) that was computed for this analysis.
    /// Mirrors <see cref="FraudReport.SuspicionScore"/> for convenience.
    /// </summary>
    public int Score { get; init; }

    /// <summary>
    /// The qualitative risk level derived from the score and detector thresholds.
    /// </summary>
    public RiskLevel Level { get; init; }

    /// <summary>
    /// Optional human-readable summary for observability (e.g. "High inconsistency: IP + timezone mismatch").
    /// Populated only when the detector chooses to provide one.
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Creates a RiskScore from a raw score and the verdict that was already determined.
    /// This keeps derivation consistent with existing verdict logic.
    /// </summary>
    public static RiskScore FromScoreAndVerdict(int score, FraudVerdict verdict, string? summary = null)
    {
        var level = verdict switch
        {
            FraudVerdict.RegionImpersonation => RiskLevel.Critical,
            FraudVerdict.HighlySuspicious => RiskLevel.High,
            FraudVerdict.Suspicious => RiskLevel.Medium,
            _ => RiskLevel.Low
        };

        return new RiskScore
        {
            Score = Math.Clamp(score, 0, 100),
            Level = level,
            Summary = string.IsNullOrWhiteSpace(summary) ? null : summary
        };
    }
}
