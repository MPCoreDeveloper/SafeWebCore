using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SafeWebCore.Abstractions;
using SafeWebCore.Models;

namespace SafeWebCore.Infrastructure;

/// <summary>
/// Middleware to handle CSP violation reports.
/// </summary>
public sealed partial class CspReportMiddleware(
    ILogger<CspReportMiddleware> logger,
    IEnumerable<ICspReportSink> sinks) : IMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

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
                LogInvalidCspViolationPayload(logger);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (string.IsNullOrWhiteSpace(report.ViolatedDirective)
                && string.IsNullOrWhiteSpace(report.EffectiveDirective))
            {
                LogInvalidCspViolationPayload(logger);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            foreach (var sink in sinks)
            {
                await sink.WriteAsync(report, context.RequestAborted).ConfigureAwait(false);
            }

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

    private sealed record CspViolationReportPayload
    {
        [JsonPropertyName("document-uri")]
        public string? DocumentUri { get; init; }

        [JsonPropertyName("referrer")]
        public string? Referrer { get; init; }

        [JsonPropertyName("violated-directive")]
        public string? ViolatedDirective { get; init; }

        [JsonPropertyName("effective-directive")]
        public string? EffectiveDirective { get; init; }

        [JsonPropertyName("original-policy")]
        public string? OriginalPolicy { get; init; }

        [JsonPropertyName("blocked-uri")]
        public string? BlockedUri { get; init; }

        [JsonPropertyName("disposition")]
        public string? Disposition { get; init; }

        [JsonPropertyName("status-code")]
        public int? StatusCode { get; init; }

        [JsonPropertyName("source-file")]
        public string? SourceFile { get; init; }

        [JsonPropertyName("line-number")]
        public int? LineNumber { get; init; }

        [JsonPropertyName("column-number")]
        public int? ColumnNumber { get; init; }

        [JsonPropertyName("script-sample")]
        public string? ScriptSample { get; init; }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid CSP violation payload received.")]
    private static partial void LogInvalidCspViolationPayload(ILogger logger);
}
