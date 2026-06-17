# SafeWebCore.FraudDetection

Optional fraud-detection module for `SafeWebCore`.

## What it adds

- **Neutral geo-cultural consistency detection** (recommended): detect strong inconsistencies between observed signals (IP country, timezone, browser language, device fonts) and your configured expected/primary region.
  - Works for any region: Western Europe+NA, Gulf/Arabic, Russia/CIS, Sub-Saharan Africa, East Asia, Latin America, etc.
  - Configure via `GeoCulturalConsistencyOptions` + `EnableGeoCulturalConsistency`.
- **Legacy Western impersonation mode** (fully supported for backward compatibility): uses `WesternDetectorOptions` + `EnableWesternImpersonation`.
- Pen-test and scanner detection (OWASP ZAP, Burp Suite, Tenable/Nessus, and configurable patterns).
- Authorized pen-test bypass via configurable header and optional secret.
- Runtime configuration fallback:
  - Primary: options pattern from `appsettings.json` with `IOptionsMonitor` reload.
  - Secondary: optional `IFraudDetectionConfigurationStore` (database-backed tenant overrides).

## Geo-IP Resolution & Data Enrichment

**You do not need a geo-IP service for SafeWebCore.FraudDetection to work.**

The detectors mainly care about these fields on `ClientFingerprintData`:

- `ResolvedCountryCode` (strongly preferred)
- `SystemTimezone` (strongly preferred)
- `IpAddress` (only used as a last-resort fallback)

### Recommended approach (cleanest)

Resolve the country and timezone yourself as early as possible (middleware, endpoint filter, or a dedicated service) and populate `ResolvedCountryCode` + `SystemTimezone` directly.

This is the **preferred pattern** because:
- You control the provider, caching, and error handling
- No hidden side effects inside the detector
- The detector stays focused purely on analysis (SRP)

Example:

```csharp
// In middleware or a request pipeline
var ip = context.Connection.RemoteIpAddress?.ToString();
var fingerprint = await BuildClientFingerprintAsync(context);

if (!string.IsNullOrWhiteSpace(ip) && geoIpService is not null)
{
    fingerprint = fingerprint.EnrichGeoIp(geoIpService);
}

// Later...
var report = fraudDetector.Analyze(fingerprint);
```

You can also call the extension method manually at any point:

```csharp
var enriched = fingerprint.EnrichGeoIp(geoIpService);
var report = fraudDetector.Analyze(enriched);
```

### Convenience fallback (optional)

If you register an `IGeoIpService`, the detectors will automatically use it as a **fallback** when:

- `IpAddress` is present, **and**
- `ResolvedCountryCode` or `SystemTimezone` is still `null`

```csharp
builder.Services.AddSingleton<IGeoIpService, MyGeoIpService>();
```

### Implementing `IGeoIpService`

```csharp
public sealed class MyGeoIpService : IGeoIpService
{
    public string? GetCountryCode(string ipAddress)
    {
        // your lookup (MaxMind GeoIP2, IP2Location, internal API, etc.)
        return null;
    }

    public string? GetTimezone(string ipAddress)
    {
        return null;
    }
}
```

### When you can skip geo-IP entirely

If your frontend already sends reliable country and timezone values (via `Intl.DateTimeFormat`, FingerprintJS, etc.), just map them into `ResolvedCountryCode` and `SystemTimezone`. No `IGeoIpService` registration is required at all.

## Registration (Minimal API)

```csharp
using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Extensions;

builder.Services.AddSingleton<IFraudDetectionConfigurationStore, ExampleDatabaseFraudDetectionConfigurationStore>();
builder.Services.AddSafeWebCoreFraudDetection(builder.Configuration);
```

Programmatic registration is also supported:

```csharp
builder.Services.AddSafeWebCoreFraudDetection(options =>
{
    options.EnableWesternImpersonation = true;
    options.EnablePenTestDetection = true;
    options.PenTestDetection.AuthorizationHeaderName = "X-PenTest-Authorized";
});
```

## Event-driven notification hook (custom mail module)

By default, scanner authorization-check notifications are dispatched to all registered
`IPenTestAuthorizationNotificationConsumer` implementations. The package registers a logging consumer out of the box.

To attach your own mail module, either register a consumer directly or register an injectable mail client.

### Option A: custom consumer

```csharp
using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Extensions;
using SafeWebCore.FraudDetection.Models;

builder.Services.AddSafeWebCoreFraudDetection(builder.Configuration);
builder.Services.AddPenTestAuthorizationNotificationConsumer<PenTestMailNotificationConsumer>();

public sealed class PenTestMailNotificationConsumer(IMailService mailService) : IPenTestAuthorizationNotificationConsumer
{
    public void OnAuthorizationCheck(PenTestAuthorizationNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        mailService.Send(
            notification.Recipients,
            notification.Subject,
            $"Potential scanner activity from {notification.IpAddress} on {notification.RequestPath}");
    }
}
```

### Option B: injectable mail client abstraction

```csharp
using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Extensions;
using SafeWebCore.FraudDetection.Models;

builder.Services.AddSafeWebCoreFraudDetection(builder.Configuration);
builder.Services.AddPenTestAuthorizationNotificationMailClient<SmtpPenTestNotificationMailClient>();

public sealed class SmtpPenTestNotificationMailClient(ISmtpSender smtpSender) : IPenTestAuthorizationNotificationMailClient
{
    public void SendAuthorizationCheck(PenTestAuthorizationNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        smtpSender.Send(
            notification.Recipients,
            notification.Subject,
            $"Potential scanner activity from {notification.IpAddress} on {notification.RequestPath}");
    }
}
```

If you need full control, you can still replace `IPenTestAuthorizationNotificationSender` directly.

## Multi-region / neutral detection (recommended for new projects)

Instead of the legacy "Western impersonation" naming, you can now protect **any** primary region:

```csharp
builder.Services.AddSafeWebCoreFraudDetection(options =>
{
    options.EnableGeoCulturalConsistency = true;
    options.EnableWesternImpersonation = false; // optional: disable legacy path
    options.EnablePenTestDetection = true;

    var geo = options.GeoCulturalConsistency;

    // Example: protect a Gulf / Arabic-speaking primary audience
    geo.ExpectedCountries = ["AE", "SA", "QA", "KW", "BH", "OM"];
    geo.InconsistentTimezones = ["Europe/Moscow", "Asia/Shanghai", "America/New_York"];
    geo.InconsistentLanguageCodes = ["ru", "zh", "ja", "ko", "en", "es"];
    geo.KnownTravelCountries = ["TR", "EG", "JO", "LB", "MA"];
    geo.TravelTimezones = ["Europe/Istanbul", "Africa/Cairo"];

    geo.MediumSuspicionThreshold = 30;
    geo.HighSuspicionThreshold = 65;
    geo.HighInconsistencyThreshold = 85;
});
```

### Example region profiles

```csharp
// Russian / CIS focused
geo.ExpectedCountries = ["RU", "BY", "KZ", "KG", "AM", "AZ"];
geo.InconsistentTimezones = ["Asia/Shanghai", "America/New_York", "Europe/London"];
geo.InconsistentLanguageCodes = ["zh", "ja", "ko", "ar", "en", "es"];

// Sub-Saharan Africa focused (example)
geo.ExpectedCountries = ["ZA", "NG", "KE", "GH", "TZ", "UG"];
geo.InconsistentTimezones = ["Europe/Moscow", "Asia/Shanghai", "America/Sao_Paulo"];
geo.InconsistentLanguageCodes = ["ru", "zh", "ar", "hi"];

// East Asia focused (example)
geo.ExpectedCountries = ["JP", "KR", "TW", "SG", "HK"];
geo.InconsistentTimezones = ["Europe/Moscow", "America/New_York", "Asia/Kolkata"];
geo.InconsistentLanguageCodes = ["ru", "ar", "hi", "es"];
```

## Configuration section (`appsettings.json`) — legacy Western example

```json
{
  "SafeWebCore": {
    "FraudDetection": {
      "EnableWesternImpersonation": true,
      "EnableGeoCulturalConsistency": false,
      "EnablePenTestDetection": true,
      "WesternImpersonation": {
        "EnableTravelMode": true,
        "HighSuspicionThreshold": 65,
        "FakeWesternThreshold": 85
      },
      "PenTestDetection": {
        "AuthorizationHeaderName": "X-PenTest-Authorized",
        "AuthorizationHeaderSecret": "replace-with-secure-secret",
        "ScannerHeaders": [ "X-ZAP-Initiator", "X-ZAP-Scan-ID", "X-Burp" ],
        "ScannerUserAgentTokens": [ "owasp zap", "burp", "tenable", "nessus" ],
        "BurstWindow": "00:00:15",
        "BurstRequestThreshold": 30,
        "ScannerScoreThreshold": 60,
        "ScannerRecommendedAction": "BlockRequest",
        "SendAuthorizationCheckEmail": true,
        "AuthorizationCheckRecipients": [ "soc@contoso.com" ],
        "NotificationCooldown": "00:10:00",
        "AuthorizationCheckSubject": "Is this an authorized penetration test?"
      }
    }
  }
}
```

## Database-backed configuration example

```csharp
using Microsoft.Extensions.Primitives;
using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Options;

public sealed class ExampleDatabaseFraudDetectionConfigurationStore : IFraudDetectionConfigurationStore
{
    public FraudDetectionOptions? GetOptions(string? tenantId)
    {
        // Query your DB by tenantId and map to FraudDetectionOptions.
        return null;
    }

    public IChangeToken GetReloadToken() => NullChangeToken.Singleton;
}
```

When the store returns `null`, the module falls back to options pattern values.

## Public API

- `IFraudDetector.Analyze(ClientFingerprintData data)`
- `FraudReport` now contains both neutral and legacy properties:
  - `IsRegionImpersonation` / `IsNotInExpectedRegion` (recommended, neutral)
  - `IsFakeWestern` / `IsNotInWesternCountry` (legacy, still populated for compatibility)
  - `Verdict` can be `RegionImpersonation` (neutral) or the legacy `FakeWestern` alias.
- `FraudDetectionOptions`:
  - `EnableGeoCulturalConsistency` + `GeoCulturalConsistency` (recommended)
  - `EnableWesternImpersonation` + `WesternImpersonation` (legacy, fully supported)
- Scanner/pen-test properties remain unchanged.

## Migration from Western-only to neutral detection

If you previously used only `EnableWesternImpersonation` + `WesternImpersonation`, your code continues to work without changes.

To move to the neutral model:

1. Set `EnableGeoCulturalConsistency = true`.
2. Configure `GeoCulturalConsistency.ExpectedCountries`, `InconsistentTimezones`, `InconsistentLanguageCodes`, etc.
3. Optionally set `EnableWesternImpersonation = false` if you no longer need the legacy path.
4. Consume the neutral report properties (`IsRegionImpersonation`, `IsNotInExpectedRegion`, `Verdict == RegionImpersonation`).

## Notes

- Existing Western-only registration overloads remain supported for backward compatibility.
- The module remains fully optional and only activates when registered.
