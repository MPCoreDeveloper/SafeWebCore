using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Infrastructure;
using SafeWebCore.FraudDetection.Models;
using SafeWebCore.FraudDetection.Options;

namespace SafeWebCore.FraudDetection.Detection;

/// <summary>
/// Legacy detector implementation using Western-centric naming and defaults.
/// 
/// <para><b>Full backward compatibility:</b> This class, its constructors, and the
/// <see cref="Options.WesternDetectorOptions"/> it consumes remain 100% supported.</para>
/// 
/// <para><b>Recommended for new scenarios:</b> Use <see cref="GeoCulturalConsistencyDetector"/>
/// + <see cref="Options.GeoCulturalConsistencyOptions"/> instead. The neutral detector allows
/// protecting any primary region (Western, Gulf/Arabic, Russian/CIS, African, East-Asian, etc.)
/// by simple configuration, without culturally biased naming.</para>
/// </summary>
/// <remarks>
/// <para>
/// <b>Geo-IP enrichment:</b> This detector no longer performs geo-IP lookups itself.
/// If you pass an <see cref="IGeoIpService"/>, the detector will use
/// <see cref="Infrastructure.GeoIpEnricher"/> as a fallback to populate
/// <see cref="Models.ClientFingerprintData.ResolvedCountryCode"/> and
/// <see cref="Models.ClientFingerprintData.SystemTimezone"/> when they are missing.
/// </para>
/// 
/// <para>
/// <b>Preferred pattern:</b> Enrich the fingerprint data yourself before calling
/// <see cref="Analyze"/> (using your own geo-IP logic or
/// <see cref="Extensions.ClientFingerprintDataExtensions.EnrichGeoIp"/>).
/// This keeps the detector focused purely on analysis.
/// </para>
/// </remarks>
[Obsolete("Use GeoCulturalConsistencyDetector + GeoCulturalConsistencyOptions for new multi-region use cases. This type remains fully functional for backward compatibility.")]
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
    private readonly IFraudEventDispatcher _fraudEventDispatcher;
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
        _notificationSender = new DispatchingPenTestAuthorizationNotificationSender(
        [
            new LoggingPenTestAuthorizationNotificationSender(
                NullLogger<LoggingPenTestAuthorizationNotificationSender>.Instance)
        ]);
        _fraudEventDispatcher = new NoOpFraudEventDispatcher();
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
    /// <param name="fraudEventDispatcher">Optional dispatcher for fraud events to sinks (additive).</param>
    internal WesternImpersonationDetector(
        IFraudDetectionOptionsResolver optionsResolver,
        ILogger<WesternImpersonationDetector> logger,
        IPenTestAuthorizationNotificationSender notificationSender,
        IGeoIpService? geoIpService = null,
        TimeProvider? timeProvider = null,
        IFraudEventDispatcher? fraudEventDispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(optionsResolver);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(notificationSender);

        _optionsResolver = optionsResolver;
        _logger = logger;
        _notificationSender = notificationSender;
        _geoIpService = geoIpService;
        _fraudEventDispatcher = fraudEventDispatcher ?? new NoOpFraudEventDispatcher();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public FraudReport Analyze(ClientFingerprintData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var options = ResolveOptions(data.TenantId);
        List<string> triggers = [];

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
                Risk = RiskScore.FromScoreAndVerdict(0, FraudVerdict.Clean),
                Verdict = FraudVerdict.Clean,
                RecommendedAction = RecommendedAction.NoAction,
                Triggers = triggers,
                TenantId = data.TenantId
            };
        }

        var enriched = GeoIpEnricher.Enrich(data, _geoIpService);
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

        var report = new FraudReport
        {
            IsFakeWestern = verdict is FraudVerdict.RegionImpersonation,
            IsNotInWesternCountry = isNotWesternCountry,
            IsPenTestScannerDetected = scannerDetected,
            IsAuthorizedPenTest = false,
            IsDetectionBypassed = false,
            PenTestAuthorizationEmailSent = emailSent,
            SuspicionScore = finalScore,
            Risk = RiskScore.FromScoreAndVerdict(finalScore, verdict),
            Triggers = triggers,
            Verdict = verdict,
            RecommendedAction = action,
            TenantId = data.TenantId
        };

        _fraudEventDispatcher.Dispatch(new FraudEvent
        {
            Report = report,
            Fingerprint = null,
            Timestamp = _timeProvider.GetUtcNow()
        });

        return report;
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

    private static FraudVerdict DetermineVerdict(int score, WesternDetectorOptions options) => score switch
    {
        _ when score >= options.FakeWesternThreshold => FraudVerdict.RegionImpersonation,
        _ when score >= options.HighSuspicionThreshold => FraudVerdict.HighlySuspicious,
        _ when score >= options.MediumSuspicionThreshold => FraudVerdict.Suspicious,
        _ => FraudVerdict.Clean
    };

    private static RecommendedAction DetermineAction(FraudVerdict verdict) => verdict switch
    {
        FraudVerdict.Clean => RecommendedAction.NoAction,
        FraudVerdict.Suspicious => RecommendedAction.Monitor,
        FraudVerdict.HighlySuspicious => RecommendedAction.StepUpAuthentication,
        FraudVerdict.RegionImpersonation => RecommendedAction.BlockRequest,
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
