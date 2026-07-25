using Microsoft.Extensions.Logging;
using SafeWebCore.FraudDetection.Abstractions;

namespace SafeWebCore.FraudDetection.Infrastructure;

/// <summary>
/// Default sink that logs fraud events at Information level when enabled.
/// Registered automatically but only produces output if logging level allows it.
/// </summary>
internal sealed partial class LoggingFraudEventSink(ILogger<LoggingFraudEventSink> logger) : IFraudEventSink
{
    public void OnFraudEvent(FraudEvent fraudEvent)
    {
        if (fraudEvent?.Report is not { } report)
            return;

        if (!logger.IsEnabled(LogLevel.Information))
            return;

        // Use LoggerMessage with pre-computed strings to avoid CA1873 (evaluation when disabled)
        var verdict = report.Verdict.ToString();
        var action = report.RecommendedAction.ToString();
        var tenant = report.TenantId ?? string.Empty;
        var riskLevel = report.Risk.Level.ToString();

        LogFraudEvent(logger, verdict, report.SuspicionScore, action, tenant, report.Triggers.Count, riskLevel);
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "FraudEvent Verdict={Verdict} Score={Score} Action={Action} Tenant={TenantId} TriggerCount={TriggerCount} Risk={RiskLevel}")]
    private static partial void LogFraudEvent(
        ILogger logger,
        string verdict,
        int score,
        string action,
        string tenantId,
        int triggerCount,
        string riskLevel);
}
