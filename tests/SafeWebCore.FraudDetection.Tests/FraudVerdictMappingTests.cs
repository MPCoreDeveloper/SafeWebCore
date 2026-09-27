using SafeWebCore.FraudDetection.Detection;
using SafeWebCore.FraudDetection.Models;

namespace SafeWebCore.FraudDetection.Tests;

/// <summary>
/// Guards the append-only enum contract and the fail-closed verdict mappings: an unrecognized verdict is
/// never reported as low risk and never as "no action".
/// </summary>
public sealed class FraudVerdictMappingTests
{
    [Theory]
    [InlineData(FraudVerdict.Clean, RiskLevel.Low)]
    [InlineData(FraudVerdict.Suspicious, RiskLevel.Medium)]
    [InlineData(FraudVerdict.HighlySuspicious, RiskLevel.High)]
    [InlineData(FraudVerdict.RegionImpersonation, RiskLevel.Critical)]
    public void FromScoreAndVerdictMapsEveryKnownVerdict(FraudVerdict verdict, RiskLevel expectedLevel)
    {
        // Act
        var risk = RiskScore.FromScoreAndVerdict(75, verdict);

        // Assert
        Assert.Equal(expectedLevel, risk.Level);
        Assert.Equal(75, risk.Score);
    }

    [Fact]
    public void FromScoreAndVerdictFailsClosedForAnUnrecognizedVerdict()
    {
        // Act
        var risk = RiskScore.FromScoreAndVerdict(0, (FraudVerdict)99);

        // Assert
        Assert.Equal(RiskLevel.Unclassified, risk.Level);
        Assert.NotEqual(RiskLevel.Low, risk.Level);
    }

    [Theory]
    [InlineData(FraudVerdict.Clean, RecommendedAction.NoAction)]
    [InlineData(FraudVerdict.Suspicious, RecommendedAction.Monitor)]
    [InlineData(FraudVerdict.HighlySuspicious, RecommendedAction.StepUpAuthentication)]
    [InlineData(FraudVerdict.RegionImpersonation, RecommendedAction.BlockRequest)]
    public void DetermineActionMapsEveryKnownVerdict(FraudVerdict verdict, RecommendedAction expectedAction)
        => Assert.Equal(expectedAction, FraudVerdictMapping.DetermineAction(verdict));

    [Fact]
    public void DetermineActionFailsClosedForAnUnrecognizedVerdict()
    {
        // Act
        var action = FraudVerdictMapping.DetermineAction((FraudVerdict)99);

        // Assert
        Assert.Equal(RecommendedAction.BlockRequest, action);
        Assert.NotEqual(RecommendedAction.NoAction, action);
    }

    [Fact]
    public void MaxSeverityReturnsTheMoreSevereAction()
    {
        Assert.Equal(
            RecommendedAction.BlockRequest,
            FraudVerdictMapping.MaxSeverity(RecommendedAction.Monitor, RecommendedAction.BlockRequest));

        Assert.Equal(
            RecommendedAction.StepUpAuthentication,
            FraudVerdictMapping.MaxSeverity(RecommendedAction.StepUpAuthentication, RecommendedAction.NoAction));
    }

    [Fact]
    public void EnumMembersKeepTheirDeclaredOrdinals()
    {
        // FraudVerdict values are compared and logged by name; inserting or renumbering changes meaning.
        Assert.Equal(0, (int)FraudVerdict.Clean);
        Assert.Equal(1, (int)FraudVerdict.Suspicious);
        Assert.Equal(2, (int)FraudVerdict.HighlySuspicious);
        Assert.Equal(3, (int)FraudVerdict.RegionImpersonation);
#pragma warning disable CS0618 // FakeWestern stays the obsolete alias and keeps sharing the RegionImpersonation value.
        Assert.Equal(FraudVerdict.RegionImpersonation, FraudVerdict.FakeWestern);
#pragma warning restore CS0618

        Assert.Equal(0, (int)RiskLevel.Low);
        Assert.Equal(1, (int)RiskLevel.Medium);
        Assert.Equal(2, (int)RiskLevel.High);
        Assert.Equal(3, (int)RiskLevel.Critical);
        Assert.Equal(4, (int)RiskLevel.Unclassified);

        // MaxSeverity compares these ordinals, so the order is part of the contract.
        Assert.Equal(0, (int)RecommendedAction.NoAction);
        Assert.Equal(1, (int)RecommendedAction.Monitor);
        Assert.Equal(2, (int)RecommendedAction.StepUpAuthentication);
        Assert.Equal(3, (int)RecommendedAction.BlockRequest);
    }
}
