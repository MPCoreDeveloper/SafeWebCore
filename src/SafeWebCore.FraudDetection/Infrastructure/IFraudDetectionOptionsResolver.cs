using SafeWebCore.FraudDetection.Options;

namespace SafeWebCore.FraudDetection.Infrastructure;

/// <summary>
/// Resolves the effective fraud-detection options at runtime.
/// </summary>
internal interface IFraudDetectionOptionsResolver
{
    FraudDetectionOptions GetCurrent(string? tenantId);
}
