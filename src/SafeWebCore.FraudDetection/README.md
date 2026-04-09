# SafeWebCore.FraudDetection

Optional fraud-detection module for `SafeWebCore`.

## What it adds

- Western impersonation detection with strong inconsistency signals (IP + timezone + language + script/font support).
- Pen-test and scanner detection (OWASP ZAP, Burp Suite, Tenable/Nessus, and configurable patterns).
- Authorized pen-test bypass via configurable header and optional secret.
- Runtime configuration fallback:
  - Primary: options pattern from `appsettings.json` with `IOptionsMonitor` reload.
  - Secondary: optional `IFraudDetectionConfigurationStore` (database-backed tenant overrides).

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

To attach your own mail module, register a consumer:

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

If you need full control, you can still replace `IPenTestAuthorizationNotificationSender` directly.

## Configuration section (`appsettings.json`)

```json
{
  "SafeWebCore": {
    "FraudDetection": {
      "EnableWesternImpersonation": true,
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
- `FraudReport` includes:
  - Western impersonation verdict and score
  - scanner detection status
  - pen-test bypass/authorization status
  - recommended action

## Notes

- Existing Western-only registration overloads remain supported for backward compatibility.
- The module remains fully optional and only activates when registered.
