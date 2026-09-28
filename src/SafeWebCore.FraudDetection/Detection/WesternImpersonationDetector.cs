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
    private readonly PenTestSignalAnalyzer _penTestSignals = new();

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

        if (PenTestSignalAnalyzer.IsAuthorizedPenTestBypass(data, options.PenTestDetection))
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
            action = FraudVerdictMapping.DetermineAction(verdict);
        }

        var penTestResult = _penTestSignals.EvaluateAndNotify(
            enriched, options, triggers, _notificationSender, _timeProvider, action);
        action = penTestResult.Action;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            LogFraudAnalysisComplete(
                _logger,
                enriched.TenantId,
                finalScore,
                verdict,
                action,
                penTestResult.ScannerDetected,
                triggers.Count);
        }

        var report = new FraudReport
        {
            IsFakeWestern = verdict is FraudVerdict.RegionImpersonation,
            IsNotInWesternCountry = isNotWesternCountry,
            IsPenTestScannerDetected = penTestResult.ScannerDetected,
            IsAuthorizedPenTest = false,
            IsDetectionBypassed = false,
            PenTestAuthorizationEmailSent = penTestResult.EmailSent,
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

    private static FraudVerdict DetermineVerdict(int score, WesternDetectorOptions options) => score switch
    {
        _ when score >= options.FakeWesternThreshold => FraudVerdict.RegionImpersonation,
        _ when score >= options.HighSuspicionThreshold => FraudVerdict.HighlySuspicious,
        _ when score >= options.MediumSuspicionThreshold => FraudVerdict.Suspicious,
        _ => FraudVerdict.Clean
    };

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
