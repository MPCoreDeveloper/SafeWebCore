using Microsoft.Extensions.Logging;
using SafeWebCore.Abstractions;

namespace SafeWebCore.Infrastructure;

/// <summary>
/// Default implementation that logs security events at Information level.
/// This is additive and only active if someone registers ISecurityEventSink (or uses the telemetry package).
/// </summary>
internal sealed partial class LoggingSecurityEventSink(ILogger<LoggingSecurityEventSink> logger) : ISecurityEventSink
{
    public Task WriteAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        // Call unconditionally. With SkipEnabledCheck=true the generated code will check the level efficiently.
        var eventType = securityEvent.EventType.ToString();
        var path = securityEvent.Path ?? string.Empty;
        LogSecurityEvent(logger, eventType, path, securityEvent.Timestamp);

        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "SecurityEvent {EventType} Path={Path} Time={Timestamp}",
        SkipEnabledCheck = true)]
    static partial void LogSecurityEvent(ILogger logger, string eventType, string path, DateTimeOffset timestamp);
}
