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
                // Base config must be valid for startup validation even though the store overrides it.
                options.PenTestDetection.SendAuthorizationCheckEmail = false;
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

    [Fact]
    public void AnalyzeEmitsFraudEventToRegisteredSink()
    {
        // Arrange
        var spy = new RecordingFraudEventSink();
        var detector = CreateDetector(
            configure: options =>
            {
                options.EnableGeoCulturalConsistency = true;
                options.GeoCulturalConsistency.ExpectedCountries = ["NL"];
                options.GeoCulturalConsistency.HighInconsistencyThreshold = 50;
                // Ensure pen-test email validation does not fail at startup in this test
                options.PenTestDetection.SendAuthorizationCheckEmail = false;
            },
            fraudEventSink: spy);

        var data = new ClientFingerprintData
        {
            ResolvedCountryCode = "RU",
            SystemTimezone = "Europe/Amsterdam",
            RequestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        // Act
        var report = detector.Analyze(data);

        // Assert
        Assert.NotNull(spy.LastEvent);
        Assert.Same(report, spy.LastEvent.Report);
        Assert.True(report.IsRegionImpersonation || report.SuspicionScore > 0);
    }

    [Fact]
    public void AnalyzeBypassPathPopulatesRiskAsLow()
    {
        var detector = CreateDetector(options =>
        {
            options.EnablePenTestDetection = true;
            options.PenTestDetection.AuthorizationHeaderName = "X-PenTest";
            options.PenTestDetection.AuthorizationHeaderSecret = "ok";
            options.PenTestDetection.SendAuthorizationCheckEmail = false;
        });

        var data = new ClientFingerprintData
        {
            RequestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["X-PenTest"] = "ok" }
        };

        var report = detector.Analyze(data);

        Assert.Equal(0, report.SuspicionScore);
        Assert.Equal(FraudVerdict.Clean, report.Verdict);
        Assert.Equal(RiskLevel.Low, report.Risk.Level);
        Assert.Equal(0, report.Risk.Score);
        Assert.Equal(RecommendedAction.NoAction, report.RecommendedAction);
    }

    [Fact]
    public void AnalyzeHighInconsistencyPopulatesRiskAsCritical()
    {
        // Use legacy Western path (default in the test helper) to avoid detector selection timing issues in DI.
        // Configure thresholds so that a strong multi-signal case produces RegionImpersonation.
        var detector = CreateDetector(options =>
        {
            options.EnableWesternImpersonation = true;
            options.EnableGeoCulturalConsistency = false;
            options.EnablePenTestDetection = false; // simplify

#pragma warning disable CS0618
            var w = options.WesternImpersonation;
            w.AllowedCountries = ["NL"];
            w.SuspiciousTimezones = ["Europe/Moscow"];
            w.NonWesternLanguageCodes = ["ru"];
            w.MediumSuspicionThreshold = 20;
            w.HighSuspicionThreshold = 35;
            w.FakeWesternThreshold = 45; // below what RU + Moscow + ru will score
#pragma warning restore CS0618
        });

        var data = new ClientFingerprintData
        {
            ResolvedCountryCode = "RU",
            SystemTimezone = "Europe/Moscow",
            BrowserLanguages = ["ru-RU"]
        };

        var report = detector.Analyze(data);

        Assert.True(report.SuspicionScore > 0);
        Assert.Equal(FraudVerdict.RegionImpersonation, report.Verdict);
        Assert.Equal(RiskLevel.Critical, report.Risk.Level);
        Assert.Equal(report.SuspicionScore, report.Risk.Score);
        Assert.Equal(RecommendedAction.BlockRequest, report.RecommendedAction);
    }

    private static IFraudDetector CreateDetector(
        Action<FraudDetectionOptions>? configure = null,
        IFraudDetectionConfigurationStore? store = null,
        IPenTestAuthorizationNotificationSender? notificationSender = null,
        IPenTestAuthorizationNotificationConsumer? notificationConsumer = null,
        IFraudEventSink? fraudEventSink = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        if (notificationSender is not null)
            services.AddSingleton<IPenTestAuthorizationNotificationSender>(notificationSender);

        if (notificationConsumer is not null)
            services.AddSingleton<IPenTestAuthorizationNotificationConsumer>(notificationConsumer);

        if (store is not null)
            services.AddSingleton<IFraudDetectionConfigurationStore>(store);

        if (fraudEventSink is not null)
            services.AddSingleton<IFraudEventSink>(fraudEventSink);

        services.AddSafeWebCoreFraudDetection(opts =>
        {
            // Always disarm email notification validation for tests (validator requires recipients when enabled)
            opts.PenTestDetection.SendAuthorizationCheckEmail = false;
            opts.PenTestDetection.AuthorizationCheckRecipients.Clear();

            configure?.Invoke(opts);
        });

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

    private sealed class RecordingFraudEventSink : IFraudEventSink
    {
        public FraudEvent? LastEvent { get; private set; }

        public void OnFraudEvent(FraudEvent fraudEvent)
        {
            LastEvent = fraudEvent;
        }
    }
}
