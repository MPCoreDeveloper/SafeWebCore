using Microsoft.Extensions.Options;
using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Options;

namespace SafeWebCore.FraudDetection.Infrastructure;

internal sealed class FraudDetectionOptionsResolver(
    IOptionsMonitor<FraudDetectionOptions> optionsMonitor,
    IFraudDetectionConfigurationStore? configurationStore = null) : IFraudDetectionOptionsResolver
{
    /// <inheritdoc />
    public FraudDetectionOptions GetCurrent(string? tenantId)
        => configurationStore?.GetOptions(tenantId) ?? optionsMonitor.CurrentValue;
}
