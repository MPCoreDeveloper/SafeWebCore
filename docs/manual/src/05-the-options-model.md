# 5. The Options Model

Everything the middleware writes comes from one object: `NetSecureHeadersOptions`. It is a mutable class with defaults,
so it binds from configuration, reads like a settings file and can be validated. Its `Csp` property is a **record**, so a
policy is replaced rather than mutated.

## The shape

| Group | Members | What they control |
| --- | --- | --- |
| Transport | `EnableHsts`, `HstsValue` | `Strict-Transport-Security` |
| Framing | `EnableXFrameOptions`, `XFrameOptionsValue` | `X-Frame-Options` |
| Sniffing | `EnableXContentTypeOptions`, `XContentTypeOptionsValue` | `X-Content-Type-Options` |
| Referrer | `EnableReferrerPolicy`, `ReferrerPolicyValue` | `Referrer-Policy` |
| Permissions | `EnablePermissionsPolicy`, `PermissionsPolicyValue` | `Permissions-Policy` |
| Cross-origin | `EnableCoep` / `CoepValue`, `EnableCoop` / `CoopValue`, `EnableCorp` / `CorpValue` | COEP, COOP, CORP |
| Legacy and hints | `EnableXDnsPrefetchControl`, `EnableXPermittedCrossDomainPolicies`, `EnableOriginAgentCluster`, `EnableXRobotsTag`, `EnableClearSiteData` (each with a `…Value`) | The remaining headers |
| Server identity | `RemoveServerHeader`, `RemoveXPoweredBy` | Removal in `OnStarting` |
| CSP | `EnableCsp`, `UseCspReportOnly`, `Csp` | The policy and its mode |
| Reporting | `EnableNel`, `NelValue`, `ReportingEndpoints` | `Network-Error-Logging` and `Reporting-Endpoints` |
| Extension points | `PathPolicies`, `AdditionalHeaders`, `CustomPolicies` | Chapters 8, 13 and 9 |

## Defaults

The defaults are the contract, and they are deliberately the strict ones for the headers that cost nothing:

| Option | Default |
| --- | --- |
| `EnableHsts`, `HstsValue` | `true`, `max-age=31536000; includeSubDomains; preload` |
| `EnableXFrameOptions`, `XFrameOptionsValue` | `true`, `DENY` |
| `EnableXContentTypeOptions`, `XContentTypeOptionsValue` | `true`, `nosniff` |
| `EnableReferrerPolicy`, `ReferrerPolicyValue` | `true`, `strict-origin-when-cross-origin` |
| `EnablePermissionsPolicy`, `PermissionsPolicyValue` | `true`, `camera=(), microphone=(), geolocation=()` |
| `EnableCoep` / `EnableCoop` / `EnableCorp` | `true` / `true` / `true` (`require-corp`, `same-origin`, `same-origin`) |
| `EnableXDnsPrefetchControl`, `EnableXPermittedCrossDomainPolicies` | `true` (`off`, `none`) |
| `EnableOriginAgentCluster`, `EnableXRobotsTag`, `EnableClearSiteData` | `false` — they change behaviour and must be opted into |
| `RemoveServerHeader`, `RemoveXPoweredBy` | `true`, `false` |
| `EnableNel` | `false` |
| `EnableCsp`, `UseCspReportOnly` | `true`, `false` |

> **Why some default to `false`.** `Origin-Agent-Cluster` changes how the origin behaves, `X-Robots-Tag: noindex` removes
> a site from search results, and `Clear-Site-Data` deletes a visitor's storage. Defaulting those on would be a
> behaviour change disguised as a security default, so each one is opt-in.

## Configuring

```csharp
builder.Services.AddNetSecureHeaders(options =>
{
    options.EnableOriginAgentCluster = true;
    options.EnableClearSiteData = false;      // keep the default explicitly, if it matters
    options.RemoveXPoweredBy = true;

    options.ReferrerPolicyValue = new ReferrerPolicyBuilder()
        .StrictOriginWhenCrossOrigin()
        .Build();
});
```

The typed builders (`ReferrerPolicyBuilder`, `PermissionsPolicyBuilder`, `CrossOriginPolicyBuilder`) exist for the three
values that are easy to get subtly wrong; see Chapter 13. Everything else is a string, and the validator (Chapter 13)
checks the combinations.

## Validation fails at startup

`AddNetSecureHeaders` registers `NetSecureHeadersOptionsValidator` and calls `ValidateOnStart()`. A configuration that
cannot work — `UseCspReportOnly` without `EnableCsp`, a path policy with an empty prefix, two policies with the same
normalised prefix, an `AdditionalHeaders` entry that names a header the library owns — stops the application at startup
with the message that says how to fix it. That is intentional: a header misconfiguration that survives until the first
request is a misconfiguration nobody sees.

```text
OptionsValidationException: Path policy '/api': UseCspReportOnly requires EnableCsp to be true.
Fix: set EnableCsp = true, or set UseCspReportOnly = false.
```

## Presets are options too

`SecurePresets.Mvc()` returns a `NetSecureHeadersOptions`; `ApplyPreset` copies its values onto yours. There is no
separate "preset mode" and no hidden state: after the copy, the object is the whole configuration, and a test can assert
it directly (Chapter 4).
