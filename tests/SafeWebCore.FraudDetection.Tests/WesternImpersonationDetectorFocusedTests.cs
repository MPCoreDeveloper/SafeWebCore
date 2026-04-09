using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Extensions;
using SafeWebCore.FraudDetection.Models;
using SafeWebCore.FraudDetection.Options;

namespace SafeWebCore.FraudDetection.Tests;

public sealed class WesternImpersonationDetectorFocusedTests
{
    [Fact]
    public void AnalyzeAuthorizedPenTestHeaderShouldBypassDetection()
    {
        // Arrange
        var detector = CreateDetector(options =>
        {
            options.EnablePenTestDetection = true;
            options.PenTestDetection.AuthorizationHeaderName = "X-PenTest-Authorized";
            options.PenTestDetection.AuthorizationHeaderSecret = "secret";
            options.PenTestDetection.SendAuthorizationCheckEmail = false;
        });

        var data = new ClientFingerprintData
        {
            RequestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-PenTest-Authorized"] = "secret"
            }
        };

        // Act
        var report = detector.Analyze(data);

        // Assert
        Assert.True(report.IsDetectionBypassed);
        Assert.True(report.IsAuthorizedPenTest);
        Assert.Equal(RecommendedAction.NoAction, report.RecommendedAction);
        Assert.Contains(FraudTrigger.PenTestBypassAuthorized, report.Triggers);
    }

    [Fact]
    public void AnalyzeZapSignalsShouldDetectScannerAndSendNotification()
    {
        // Arrange
        var sender = new RecordingNotificationSender();
        var detector = CreateDetector(
            configure: options =>
            {
                options.EnableWesternImpersonation = false;
                options.EnablePenTestDetection = true;
                options.PenTestDetection.SendAuthorizationCheckEmail = true;
                options.PenTestDetection.AuthorizationCheckRecipients = ["soc@contoso.com"];
                options.PenTestDetection.ScannerScoreThreshold = 40;
            },
            notificationSender: sender);

        var data = new ClientFingerprintData
        {
            IpAddress = "203.0.113.10",
            RequestPath = "/.env",
            UserAgent = "OWASP ZAP",
            RequestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-ZAP-Initiator"] = "3"
            }
        };

        // Act
        var report = detector.Analyze(data);

        // Assert
        Assert.True(report.IsPenTestScannerDetected);
        Assert.True(report.PenTestAuthorizationEmailSent);
        Assert.True(sender.WasCalled);
        Assert.Contains(FraudTrigger.ScannerZapHeader, report.Triggers);
    }

    [Fact]
    public void AnalyzeTenantOverrideFromConfigurationStoreShouldUseStoreOptions()
    {
        // Arrange
        var store = new TenantOverrideStore(new FraudDetectionOptions
        {
            EnableWesternImpersonation = false,
            EnablePenTestDetection = true,
            PenTestDetection =
            {
                ScannerScoreThreshold = 1,
                SendAuthorizationCheckEmail = false
            }
        });

        var detector = CreateDetector(
            configure: options =>
            {
                options.EnableWesternImpersonation = true;
                options.EnablePenTestDetection = false;
            },
            store: store);

        var data = new ClientFingerprintData
        {
            TenantId = "tenant-a",
            UserAgent = "OWASP ZAP",
            RequestPath = "/scan",
            RequestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        // Act
        var report = detector.Analyze(data);

        // Assert
        Assert.True(report.IsPenTestScannerDetected);
        Assert.Equal(RecommendedAction.BlockRequest, report.RecommendedAction);
    }

    [Fact]
    public void AnalyzeScannerSignalsShouldPublishAuthorizationCheckToRegisteredConsumer()
    {
        // Arrange
        var consumer = new RecordingNotificationConsumer();
        var detector = CreateDetector(
            configure: options =>
            {
                options.EnableWesternImpersonation = false;
                options.EnablePenTestDetection = true;
                options.PenTestDetection.SendAuthorizationCheckEmail = true;
                options.PenTestDetection.AuthorizationCheckRecipients = ["soc@contoso.com"];
                options.PenTestDetection.ScannerScoreThreshold = 40;
            },
            notificationConsumer: consumer);

        var data = new ClientFingerprintData
        {
            IpAddress = "203.0.113.11",
            RequestPath = "/.git/config",
            UserAgent = "OWASP ZAP",
            RequestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-ZAP-Initiator"] = "1"
            }
        };

        // Act
        var report = detector.Analyze(data);

        // Assert
        Assert.True(report.PenTestAuthorizationEmailSent);
        Assert.True(consumer.WasCalled);
    }

    private static IFraudDetector CreateDetector(
        Action<FraudDetectionOptions>? configure = null,
        IFraudDetectionConfigurationStore? store = null,
        IPenTestAuthorizationNotificationSender? notificationSender = null,
        IPenTestAuthorizationNotificationConsumer? notificationConsumer = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        if (notificationSender is not null)
            services.AddSingleton<IPenTestAuthorizationNotificationSender>(notificationSender);

        if (notificationConsumer is not null)
            services.AddSingleton<IPenTestAuthorizationNotificationConsumer>(notificationConsumer);

        if (store is not null)
            services.AddSingleton<IFraudDetectionConfigurationStore>(store);

        services.AddSafeWebCoreFraudDetection(configure ?? (_ => { }));

        return services.BuildServiceProvider().GetRequiredService<IFraudDetector>();
    }

    private sealed class RecordingNotificationSender : IPenTestAuthorizationNotificationSender
    {
        public bool WasCalled { get; private set; }

        public void SendAuthorizationCheck(PenTestAuthorizationNotification notification)
        {
            ArgumentNullException.ThrowIfNull(notification);
            WasCalled = true;
        }
    }

    private sealed class RecordingNotificationConsumer : IPenTestAuthorizationNotificationConsumer
    {
        public bool WasCalled { get; private set; }

        public void OnAuthorizationCheck(PenTestAuthorizationNotification notification)
        {
            ArgumentNullException.ThrowIfNull(notification);
            WasCalled = true;
        }
    }

    private sealed class TenantOverrideStore(FraudDetectionOptions options) : IFraudDetectionConfigurationStore
    {
        public FraudDetectionOptions? GetOptions(string? tenantId)
            => string.Equals(tenantId, "tenant-a", StringComparison.OrdinalIgnoreCase)
                ? options
                : null;

        public IChangeToken GetReloadToken() => new CancellationChangeToken(CancellationToken.None);
    }
}
