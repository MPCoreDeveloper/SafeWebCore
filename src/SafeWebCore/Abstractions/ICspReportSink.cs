using SafeWebCore.Models;

namespace SafeWebCore.Abstractions;

/// <summary>
/// Receives parsed CSP violation reports for custom processing.
/// </summary>
public interface ICspReportSink
{
    /// <summary>
    /// Handles a parsed CSP violation report.
    /// </summary>
    /// <param name="report">The parsed CSP violation report.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous sink operation.</returns>
    Task WriteAsync(CspViolationReport report, CancellationToken cancellationToken = default);
}
