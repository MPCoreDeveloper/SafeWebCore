using SafeWebCore.FraudDetection.Models;

namespace SafeWebCore.FraudDetection.Detection;

/// <summary>
/// The single source for the verdict-to-action mapping and the severity comparison, shared by every
/// detector. It lives here so a new <see cref="FraudVerdict"/> member is added in one place, and so an
/// unrecognized verdict fails closed instead of being reported as safe.
/// </summary>
internal static class FraudVerdictMapping
{
    /// <summary>
    /// Maps a verdict to the recommended action. A verdict this method does not recognize fails closed to
    /// <see cref="RecommendedAction.BlockRequest"/>, never to <see cref="RecommendedAction.NoAction"/>.
    /// </summary>
    /// <param name="verdict">The verdict the detector determined.</param>
    /// <returns>The recommended action for the verdict.</returns>
    internal static RecommendedAction DetermineAction(FraudVerdict verdict) => verdict switch
    {
        FraudVerdict.Clean => RecommendedAction.NoAction,
        FraudVerdict.Suspicious => RecommendedAction.Monitor,
        FraudVerdict.HighlySuspicious => RecommendedAction.StepUpAuthentication,
        FraudVerdict.RegionImpersonation => RecommendedAction.BlockRequest,
        // Fail closed: an unrecognized verdict is never treated as safe.
        _ => RecommendedAction.BlockRequest
    };

    /// <summary>
    /// Returns the more severe of two actions, using the enum's ordinal values.
    /// </summary>
    /// <param name="first">The first action.</param>
    /// <param name="second">The second action.</param>
    /// <returns>The action with the highest ordinal.</returns>
    internal static RecommendedAction MaxSeverity(RecommendedAction first, RecommendedAction second)
        => (RecommendedAction)Math.Max((int)first, (int)second);
}
