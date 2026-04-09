using SafeWebCore.FraudDetection.Models;

namespace SafeWebCore.FraudDetection.Abstractions;

/// <summary>
/// Analyzes client fingerprint data and returns a fraud assessment report.
/// </summary>
/// <remarks>
/// Register an implementation via
/// <c>services.AddSafeWebCoreFraudDetection()</c> and inject
/// <see cref="IFraudDetector"/> wherever an analysis is needed.
/// </remarks>
public interface IFraudDetector
{
    /// <summary>
    /// Performs a synchronous fraud analysis on the supplied fingerprint
    /// <paramref name="data"/> and returns a <see cref="FraudReport"/>.
    /// </summary>
    /// <param name="data">
    /// Client fingerprint data collected from the HTTP request and optional
    /// client-side JavaScript signals. Include request headers and path to
    /// enable scanner and penetration-test analysis.
    /// </param>
    /// <returns>
    /// A <see cref="FraudReport"/> containing Western-impersonation verdicts,
    /// scanner detection status, penetration-test authorization status, and
    /// recommended action.
    /// </returns>
    FraudReport Analyze(ClientFingerprintData data);
}
