using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SafeWebCore.Abstractions;
using SafeWebCore.Infrastructure;
using SafeWebCore.Models;

namespace SafeWebCore.Infrastructure;

/// <summary>
/// Middleware to handle CSP violation reports.
/// </summary>
public sealed partial class CspReportMiddleware : IMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<CspReportMiddleware> _logger;
    private readonly IEnumerable<ICspReportSink> _sinks;
    private readonly SecurityEventDispatcher _eventDispatcher;
    private readonly SafeWebCoreMetrics _metrics;

    /// <summary>
    /// Backward-compatible constructor that creates the CSP report middleware
    /// with only the originally shipped dependencies.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="sinks">The report sinks.</param>
    public CspReportMiddleware(
        ILogger<CspReportMiddleware> logger,
        IEnumerable<ICspReportSink> sinks)
        : this(logger, sinks, null, null)
    {
    }

    /// <summary>
    /// Creates the CSP report middleware with optional observability integrations.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="sinks">The report sinks.</param>
    /// <param name="eventDispatcher">Optional dispatcher for security telemetry events.</param>
    /// <param name="metrics">Optional metrics instance for opt-in counters.</param>
    public CspReportMiddleware(
        ILogger<CspReportMiddleware> logger,
        IEnumerable<ICspReportSink> sinks,
        SecurityEventDispatcher? eventDispatcher,
        SafeWebCoreMetrics? metrics)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(sinks);

        _logger = logger;
        _sinks = sinks;
        _eventDispatcher = eventDispatcher ?? new SecurityEventDispatcher([]);
        _metrics = metrics ?? new SafeWebCoreMetrics();
    }

    /// <summary>
    /// Invokes the middleware to handle CSP reports.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.Request.Path == "/csp-report" && context.Request.Method == "POST")
        {
            var report = await ParseReportAsync(context.Request.Body, context.RequestAborted).ConfigureAwait(false);
            if (report is null)
            {
                LogInvalidCspViolationPayload(_logger);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (string.IsNullOrWhiteSpace(report.ViolatedDirective)
                && string.IsNullOrWhiteSpace(report.EffectiveDirective))
            {
                LogInvalidCspViolationPayload(_logger);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            foreach (var sink in _sinks)
            {
                await sink.WriteAsync(report, context.RequestAborted).ConfigureAwait(false);
            }

            // Emit additive security event for telemetry (v1.6 observability)
            _ = _eventDispatcher.EmitAsync(new SecurityEvent
            {
                EventType = SecurityEventType.CspViolation,
                Path = report.DocumentUri,
                Properties = new Dictionary<string, object?>
                {
                    ["ViolatedDirective"] = report.ViolatedDirective ?? report.EffectiveDirective,
                    ["BlockedUri"] = report.BlockedUri,
                    ["Disposition"] = report.Disposition
                }
            }, context.RequestAborted);

            _metrics.CspViolations.Add(1);

            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        await next(context);
    }

    private static async Task<CspViolationReport?> ParseReportAsync(Stream requestBody, CancellationToken cancellationToken)
    {
        try
        {
            using var jsonDocument = await JsonDocument.ParseAsync(requestBody, cancellationToken: cancellationToken).ConfigureAwait(false);

            var payloadElement = jsonDocument.RootElement;
            if (payloadElement.TryGetProperty("csp-report", out var cspReportElement))
            {
                payloadElement = cspReportElement;
            }

            if (payloadElement.ValueKind is not JsonValueKind.Object)
            {
                return null;
            }

            var payload = payloadElement.Deserialize<CspViolationReportPayload>(SerializerOptions);
            if (payload is null)
            {
                return null;
            }

            return new CspViolationReport
            {
                DocumentUri = payload.DocumentUri,
                Referrer = payload.Referrer,
                ViolatedDirective = payload.ViolatedDirective,
                EffectiveDirective = payload.EffectiveDirective,
                OriginalPolicy = payload.OriginalPolicy,
                BlockedUri = payload.BlockedUri,
                Disposition = payload.Disposition,
                StatusCode = payload.StatusCode,
                SourceFile = payload.SourceFile,
                LineNumber = payload.LineNumber,
                ColumnNumber = payload.ColumnNumber,
                Sample = payload.ScriptSample
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Internal payload model matching the browser JSON schema for CSP violation reports.
    /// </summary>
    private sealed record CspViolationReportPayload
    {
        /// <summary>The protected resource URI.</summary>
        [JsonPropertyName("document-uri")]
        public string? DocumentUri { get; init; }

        /// <summary>The referrer URI, if present.</summary>
        [JsonPropertyName("referrer")]
        public string? Referrer { get; init; }

        /// <summary>The violated CSP directive.</summary>
        [JsonPropertyName("violated-directive")]
        public string? ViolatedDirective { get; init; }

        /// <summary>The effective directive that was enforced.</summary>
        [JsonPropertyName("effective-directive")]
        public string? EffectiveDirective { get; init; }

        /// <summary>The original policy string evaluated by the browser.</summary>
        [JsonPropertyName("original-policy")]
        public string? OriginalPolicy { get; init; }

        /// <summary>The URI that was blocked.</summary>
        [JsonPropertyName("blocked-uri")]
        public string? BlockedUri { get; init; }

        /// <summary>The enforcement disposition (e.g., enforce or report).</summary>
        [JsonPropertyName("disposition")]
        public string? Disposition { get; init; }

        /// <summary>The HTTP status code observed by the browser.</summary>
        [JsonPropertyName("status-code")]
        public int? StatusCode { get; init; }

        /// <summary>The source file where the violation originated, when available.</summary>
        [JsonPropertyName("source-file")]
        public string? SourceFile { get; init; }

        /// <summary>The source line number for the violation, when available.</summary>
        [JsonPropertyName("line-number")]
        public int? LineNumber { get; init; }

        /// <summary>The source column number for the violation, when available.</summary>
        [JsonPropertyName("column-number")]
        public int? ColumnNumber { get; init; }

        /// <summary>A browser-provided sample of the violating script.</summary>
        [JsonPropertyName("script-sample")]
        public string? ScriptSample { get; init; }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid CSP violation payload received.")]
    private static partial void LogInvalidCspViolationPayload(ILogger logger);
}
