using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Infrastructure;
using SafeWebCore.FraudDetection.Models;
using SafeWebCore.FraudDetection.Options;

namespace SafeWebCore.FraudDetection.Detection;

/// <summary>
/// Primary implementation of <see cref="IFraudDetector"/> for Western-impersonation,
/// scanner detection, and penetration-test authorization checks.
/// </summary>
public sealed partial class WesternImpersonationDetector : IFraudDetector
{
    private const int ZapHeaderScore = 80;
    private const int HeaderScore = 50;
    private const int UserAgentScore = 35;
    private const int PathProbeScore = 20;
    private const int BurstScore = 30;

    private static readonly FraudDetectionOptions LegacyDefaults = new()
    {
        EnablePenTestDetection = false
    };

    private readonly IFraudDetectionOptionsResolver? _optionsResolver;
    private readonly WesternDetectorOptions? _legacyOptions;
    private readonly IGeoIpService? _geoIpService;
    private readonly IPenTestAuthorizationNotificationSender _notificationSender;
    private readonly ILogger<WesternImpersonationDetector> _logger;
    private readonly TimeProvider _timeProvider;

    private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTimeOffset>> _requestWindows = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastNotifications = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a detector using the legacy Western-only options model.
    /// </summary>
    /// <param name="options">Legacy detector options.</param>
    /// <param name="logger">Logger used for analysis diagnostics.</param>
    /// <param name="geoIpService">Optional geo-IP service.</param>
    public WesternImpersonationDetector(
        IOptions<WesternDetectorOptions> options,
        ILogger<WesternImpersonationDetector> logger,
        IGeoIpService? geoIpService = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _legacyOptions = options.Value;
        _logger = logger;
        _geoIpService = geoIpService;
        _notificationSender = new LoggingPenTestAuthorizationNotificationSender(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<LoggingPenTestAuthorizationNotificationSender>.Instance);
        _timeProvider = TimeProvider.System;
    }

    /// <summary>
    /// Initializes a detector using full runtime-configurable fraud-detection options.
    /// </summary>
    /// <param name="optionsResolver">Runtime options resolver.</param>
    /// <param name="logger">Logger used for analysis diagnostics.</param>
    /// <param name="notificationSender">Authorization-check notification sender.</param>
    /// <param name="geoIpService">Optional geo-IP service.</param>
    /// <param name="timeProvider">Optional time provider for burst and cooldown checks.</param>
    internal WesternImpersonationDetector(
        IFraudDetectionOptionsResolver optionsResolver,
        ILogger<WesternImpersonationDetector> logger,
        IPenTestAuthorizationNotificationSender notificationSender,
        IGeoIpService? geoIpService = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(optionsResolver);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(notificationSender);

        _optionsResolver = optionsResolver;
        _logger = logger;
        _notificationSender = notificationSender;
        _geoIpService = geoIpService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public FraudReport Analyze(ClientFingerprintData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var options = ResolveOptions(data.TenantId);
        var triggers = new List<string>();

        if (IsAuthorizedPenTestBypass(data, options.PenTestDetection))
        {
            triggers.Add(FraudTrigger.PenTestBypassAuthorized);

            return new FraudReport
            {
                IsAuthorizedPenTest = true,
                IsDetectionBypassed = true,
                IsPenTestScannerDetected = false,
                PenTestAuthorizationEmailSent = false,
                SuspicionScore = 0,
                Verdict = FraudVerdict.Clean,
                RecommendedAction = RecommendedAction.NoAction,
                Triggers = triggers,
                TenantId = data.TenantId
            };
        }

        var enriched = EnrichWithGeoIp(data);
        var westernOptions = options.WesternImpersonation;

        int finalScore = 0;
        var verdict = FraudVerdict.Clean;
        var action = RecommendedAction.NoAction;
        bool isNotWesternCountry = false;

        if (options.EnableWesternImpersonation)
        {
            var scorer = new SuspicionScorer(westernOptions);
            var travelEvaluator = new TravelModeEvaluator(westernOptions);

            var (rawScore, westernTriggers) = scorer.Evaluate(enriched);
            finalScore = travelEvaluator.AdjustScore(rawScore, enriched);
            triggers.AddRange(westernTriggers);

            isNotWesternCountry =
                !string.IsNullOrWhiteSpace(enriched.ResolvedCountryCode) &&
                !westernOptions.AllowedCountries.Contains(enriched.ResolvedCountryCode);

            verdict = DetermineVerdict(finalScore, westernOptions);
            action = DetermineAction(verdict);
        }

        bool scannerDetected = false;
        bool emailSent = false;

        if (options.EnablePenTestDetection)
        {
            var scanner = EvaluateScannerSignals(enriched, options.PenTestDetection, triggers);
            scannerDetected = scanner.IsDetected;

            if (scannerDetected)
            {
                if (ShouldSendNotification(enriched, options.PenTestDetection))
                {
                    _notificationSender.SendAuthorizationCheck(new PenTestAuthorizationNotification
                    {
                        Recipients = options.PenTestDetection.AuthorizationCheckRecipients,
                        Subject = options.PenTestDetection.AuthorizationCheckSubject,
                        IpAddress = enriched.IpAddress,
                        RequestPath = enriched.RequestPath,
                        Triggers = triggers,
                        TenantId = enriched.TenantId
                    });

                    emailSent = true;
                    StoreNotificationTimestamp(enriched);
                }

                action = MaxSeverity(action, options.PenTestDetection.ScannerRecommendedAction);
            }
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            LogFraudAnalysisComplete(
                _logger,
                enriched.TenantId,
                finalScore,
                verdict,
                action,
                scannerDetected,
                triggers.Count);
        }

        return new FraudReport
        {
            IsFakeWestern = verdict is FraudVerdict.FakeWestern,
            IsNotInWesternCountry = isNotWesternCountry,
            IsPenTestScannerDetected = scannerDetected,
            IsAuthorizedPenTest = false,
            IsDetectionBypassed = false,
            PenTestAuthorizationEmailSent = emailSent,
            SuspicionScore = finalScore,
            Triggers = triggers,
            Verdict = verdict,
            RecommendedAction = action,
            TenantId = data.TenantId
        };
    }

    private FraudDetectionOptions ResolveOptions(string? tenantId)
    {
        if (_optionsResolver is null)
        {
            if (_legacyOptions is null)
                return LegacyDefaults;

            return new FraudDetectionOptions
            {
                EnableWesternImpersonation = true,
                EnablePenTestDetection = false,
                WesternImpersonation = _legacyOptions,
                PenTestDetection = new PenTestDetectionOptions()
            };
        }

        return _optionsResolver.GetCurrent(tenantId);
    }

    private static bool IsAuthorizedPenTestBypass(ClientFingerprintData data, PenTestDetectionOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AuthorizationHeaderName))
            return false;

        if (!TryGetHeaderValue(data, options.AuthorizationHeaderName, out var suppliedValue))
            return false;

        if (string.IsNullOrWhiteSpace(options.AuthorizationHeaderSecret))
            return !string.IsNullOrWhiteSpace(suppliedValue);

        return string.Equals(
            suppliedValue,
            options.AuthorizationHeaderSecret,
            StringComparison.Ordinal);
    }

    private (bool IsDetected, int Score) EvaluateScannerSignals(
        ClientFingerprintData data,
        PenTestDetectionOptions options,
        List<string> triggers)
    {
        int score = 0;

        if (TryGetHeaderValue(data, "X-ZAP-Initiator", out _))
        {
            score += ZapHeaderScore;
            triggers.Add(FraudTrigger.ScannerZapHeader);
        }

        foreach (var headerName in options.ScannerHeaders)
        {
            if (!TryGetHeaderValue(data, headerName, out _))
                continue;

            score += HeaderScore;
            triggers.Add(FraudTrigger.ScannerHeader);
            break;
        }

        if (!string.IsNullOrWhiteSpace(data.UserAgent) &&
            options.ScannerUserAgentTokens.Any(token =>
                data.UserAgent.Contains(token, StringComparison.OrdinalIgnoreCase)))
        {
            score += UserAgentScore;
            triggers.Add(FraudTrigger.ScannerUserAgent);
        }

        if (!string.IsNullOrWhiteSpace(data.RequestPath) &&
            options.ScannerPathFragments.Any(fragment =>
                data.RequestPath.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            score += PathProbeScore;
            triggers.Add(FraudTrigger.ScannerPathProbe);
        }

        if (IsBurstRateExceeded(data, options))
        {
            score += BurstScore;
            triggers.Add(FraudTrigger.ScannerBurstRate);
        }

        return (score >= options.ScannerScoreThreshold, score);
    }

    private bool IsBurstRateExceeded(ClientFingerprintData data, PenTestDetectionOptions options)
    {
        var sourceKey = GetSourceKey(data);
        var now = data.RequestTimestampUtc ?? _timeProvider.GetUtcNow();

        var queue = _requestWindows.GetOrAdd(sourceKey, _ => new ConcurrentQueue<DateTimeOffset>());
        queue.Enqueue(now);

        while (queue.TryPeek(out var ts) && (now - ts) > options.BurstWindow)
            queue.TryDequeue(out _);

        return queue.Count >= options.BurstRequestThreshold;
    }

    private bool ShouldSendNotification(ClientFingerprintData data, PenTestDetectionOptions options)
    {
        if (!options.SendAuthorizationCheckEmail)
            return false;

        if (options.AuthorizationCheckRecipients.Count == 0)
            return false;

        var sourceKey = GetSourceKey(data);
        var now = data.RequestTimestampUtc ?? _timeProvider.GetUtcNow();

        if (!_lastNotifications.TryGetValue(sourceKey, out var lastSentAt))
            return true;

        return (now - lastSentAt) >= options.NotificationCooldown;
    }

    private void StoreNotificationTimestamp(ClientFingerprintData data)
    {
        var sourceKey = GetSourceKey(data);
        _lastNotifications[sourceKey] = data.RequestTimestampUtc ?? _timeProvider.GetUtcNow();
    }

    private static bool TryGetHeaderValue(ClientFingerprintData data, string headerName, out string value)
    {
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(headerName) || data.RequestHeaders.Count == 0)
            return false;

        if (!data.RequestHeaders.TryGetValue(headerName, out var foundValue))
            return false;

        value = foundValue ?? string.Empty;
        return true;
    }

    private static string GetSourceKey(ClientFingerprintData data)
        => $"{data.TenantId ?? "default"}:{data.FingerprintVisitorId ?? "unknown"}:{data.IpAddress ?? "unknown"}";

    private ClientFingerprintData EnrichWithGeoIp(ClientFingerprintData data)
    {
        if (_geoIpService is null || string.IsNullOrWhiteSpace(data.IpAddress))
            return data;

        var country = data.ResolvedCountryCode ?? _geoIpService.GetCountryCode(data.IpAddress);
        var timezone = data.SystemTimezone ?? _geoIpService.GetTimezone(data.IpAddress);

        if (country == data.ResolvedCountryCode && timezone == data.SystemTimezone)
            return data;

        return data with
        {
            ResolvedCountryCode = country,
            SystemTimezone = timezone
        };
    }

    private static FraudVerdict DetermineVerdict(int score, WesternDetectorOptions options) => score switch
    {
        _ when score >= options.FakeWesternThreshold => FraudVerdict.FakeWestern,
        _ when score >= options.HighSuspicionThreshold => FraudVerdict.HighlySuspicious,
        _ when score >= options.MediumSuspicionThreshold => FraudVerdict.Suspicious,
        _ => FraudVerdict.Clean
    };

    private static RecommendedAction DetermineAction(FraudVerdict verdict) => verdict switch
    {
        FraudVerdict.Clean => RecommendedAction.NoAction,
        FraudVerdict.Suspicious => RecommendedAction.Monitor,
        FraudVerdict.HighlySuspicious => RecommendedAction.StepUpAuthentication,
        FraudVerdict.FakeWestern => RecommendedAction.BlockRequest,
        _ => RecommendedAction.NoAction
    };

    private static RecommendedAction MaxSeverity(RecommendedAction first, RecommendedAction second)
        => (RecommendedAction)Math.Max((int)first, (int)second);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Fraud analysis complete — Tenant={TenantId} Score={Score}, Verdict={Verdict}, Action={Action}, Scanner={ScannerDetected}, TriggerCount={TriggerCount}")]
    private static partial void LogFraudAnalysisComplete(
        ILogger logger,
        string? tenantId,
        int score,
        FraudVerdict verdict,
        RecommendedAction action,
        bool scannerDetected,
        int triggerCount);
}
