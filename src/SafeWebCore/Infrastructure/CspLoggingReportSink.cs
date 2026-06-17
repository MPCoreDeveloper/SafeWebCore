using Microsoft.Extensions.Logging;
using SafeWebCore.Abstractions;
using SafeWebCore.Models;

namespace SafeWebCore.Infrastructure;

internal sealed partial class CspLoggingReportSink(ILogger<CspLoggingReportSink> logger) : ICspReportSink
{
    /// <inheritdoc />
    public Task WriteAsync(CspViolationReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        LogCspViolation(
            logger,
            report.DocumentUri,
            report.ViolatedDirective,
            report.BlockedUri,
            report.Disposition,
            report.Sample);

        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "CSP violation. DocumentUri={DocumentUri}, ViolatedDirective={ViolatedDirective}, BlockedUri={BlockedUri}, Disposition={Disposition}, Sample={Sample}")]
    private static partial void LogCspViolation(
        ILogger logger,
        string? documentUri,
        string? violatedDirective,
        string? blockedUri,
        string? disposition,
        string? sample);
}
