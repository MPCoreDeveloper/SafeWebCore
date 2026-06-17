using Microsoft.Extensions.Options;
using SafeWebCore.FraudDetection.Options;

namespace SafeWebCore.FraudDetection.Infrastructure;

internal sealed class FraudDetectionOptionsValidator : IValidateOptions<FraudDetectionOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, FraudDetectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        ValidateWesternThresholds(options.WesternImpersonation, failures);
        ValidatePenTestOptions(options.PenTestDetection, failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    #pragma warning disable CS0618
    private static void ValidateWesternThresholds(WesternDetectorOptions options, List<string> failures)
    #pragma warning restore CS0618
    {
        if (options.MediumSuspicionThreshold < 0 || options.HighSuspicionThreshold < 0 || options.FakeWesternThreshold < 0)
            failures.Add("Western detector thresholds must be greater than or equal to zero.");

        if (options.MediumSuspicionThreshold > options.HighSuspicionThreshold)
            failures.Add("MediumSuspicionThreshold must be less than or equal to HighSuspicionThreshold.");

        if (options.HighSuspicionThreshold > options.FakeWesternThreshold)
            failures.Add("HighSuspicionThreshold must be less than or equal to FakeWesternThreshold.");
    }

    private static void ValidatePenTestOptions(PenTestDetectionOptions options, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(options.AuthorizationHeaderName))
            failures.Add("PenTestDetection.AuthorizationHeaderName must not be null, empty, or whitespace.");

        if (options.BurstWindow <= TimeSpan.Zero)
            failures.Add("PenTestDetection.BurstWindow must be greater than zero.");

        if (options.BurstRequestThreshold <= 0)
            failures.Add("PenTestDetection.BurstRequestThreshold must be greater than zero.");

        if (options.ScannerScoreThreshold <= 0)
            failures.Add("PenTestDetection.ScannerScoreThreshold must be greater than zero.");

        if (options.NotificationCooldown < TimeSpan.Zero)
            failures.Add("PenTestDetection.NotificationCooldown must be greater than or equal to zero.");

        if (options.SendAuthorizationCheckEmail && options.AuthorizationCheckRecipients.Count == 0)
            failures.Add("PenTestDetection.AuthorizationCheckRecipients must include at least one recipient when email notifications are enabled.");
    }
}
