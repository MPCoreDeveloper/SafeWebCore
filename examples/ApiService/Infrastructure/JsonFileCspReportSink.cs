using System.Text.Json;
using SafeWebCore.Abstractions;
using SafeWebCore.Models;

namespace SafeWebCore.Examples.ApiService.Infrastructure;

/// <summary>
/// Custom <see cref="ICspReportSink"/> that appends each CSP violation as a
/// JSON-lines entry to <c>csp-violations.jsonl</c> next to the binary.
/// Register alongside the built-in <c>CspLoggingReportSink</c> to capture
/// violations for offline analysis or forwarding to a SIEM.
/// </summary>
public sealed class JsonFileCspReportSink : ICspReportSink
{
    private static readonly string FilePath =
        Path.Combine(AppContext.BaseDirectory, "csp-violations.jsonl");

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { WriteIndented = false };

    // PERF: Lock protects concurrent writes from multiple request threads.
    private readonly Lock _writeLock = new();

    /// <inheritdoc />
    public async Task WriteAsync(CspViolationReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var line = JsonSerializer.Serialize(report, JsonOptions);

        // Offload the synchronous file append off the thread pool thread.
        await Task.Run(() =>
        {
            lock (_writeLock)
            {
                File.AppendAllText(FilePath, line + Environment.NewLine);
            }
        }, cancellationToken).ConfigureAwait(false);
    }
}
