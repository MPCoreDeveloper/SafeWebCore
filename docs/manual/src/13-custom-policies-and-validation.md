# 13. Custom Policies, Additional Headers and Validation

Three extension points let an application go beyond the typed options without abandoning the guarantees: a custom header
policy, a plain additional header, and the startup validator that keeps both honest.

## A custom policy

`IHeaderPolicy` is one method. It receives the `HttpResponse` while headers can still be written, so it can compute a
value from the request, the endpoint or a service it was constructed with.

```csharp
using SafeWebCore.Abstractions;

public sealed class RequestIdHeaderPolicy : IHeaderPolicy
{
    public void Apply(HttpResponse response)
    {
        var context = response.HttpContext;
        var id = context.TraceIdentifier;

        response.Headers["X-Request-Id"] = id;
    }
}
```

```csharp
builder.Services.AddNetSecureHeaders(options =>
{
    options.ApplyPreset(SecurePresets.Mvc());
    options.CustomPolicies.Add(new RequestIdHeaderPolicy());
});
```

Policies run after the built-in headers and before the additional headers, which gives a deterministic order:

1. the headers the options enable,
2. `Network-Error-Logging` and `Reporting-Endpoints` when configured,
3. the CSP (`Content-Security-Policy` or `…-Report-Only`),
4. every `AdditionalHeaders` entry,
5. every `CustomPolicies` entry.

A policy that must *replace* a library header should say so in its name and be reviewed as such: the later entries in the
list win, so a custom policy that writes `Content-Security-Policy` after the CSP step overwrites the computed policy,
nonce included.

## An additional header

For a constant header, `AdditionalHeaders` is enough and needs no class:

```csharp
options.AdditionalHeaders =
[
    new AdditionalHeaderOptions { Name = "X-Content-Language", Value = "en" }
];
```

The validator rejects two things about them, both at startup:

- **a duplicate name** (case-insensitive), because the last assignment would win silently;
- **a name the library owns** — `Content-Security-Policy`, `Strict-Transport-Security`, `X-Frame-Options` and the rest of
  the typed set. Assigning the response indexer for one of those overwrites the library value, and for CSP it would drop
  the per-request nonce, which un-authorises every script on the page. `CustomPolicies` is the deliberate escape hatch
  when a header really must be computed.

## The typed builders

Three header values have enough structure to deserve a builder. Each returns a string (or a value record), so it can be
assigned to the matching option.

```csharp
options.ReferrerPolicyValue = new ReferrerPolicyBuilder()
    .StrictOriginWhenCrossOrigin()      // NoReferrer, NoReferrerWhenDowngrade, Origin, OriginWhenCrossOrigin,
    .Build();                          // SameOrigin, StrictOrigin, StrictOriginWhenCrossOrigin, UnsafeUrl

options.PermissionsPolicyValue = new PermissionsPolicyBuilder()
    .Disable(PermissionsFeature.Camera)
    .AllowSelf(PermissionsFeature.Geolocation)
    .Allow(PermissionsFeature.Fullscreen, "'self'", "https://player.example.com")
    .Build();

var crossOrigin = new CrossOriginPolicyBuilder()
    .CoepRequireCorp()                  // or CoepCredentialless
    .CoopSameOrigin()                   // or CoopUnsafeNone, CoopSameOriginAllowPopups
    .CorpSameOrigin()                   // or CorpSameSite, CorpCrossOrigin
    .Build();

options.CoepValue = crossOrigin.Coep;
options.CoopValue = crossOrigin.Coop;
options.CorpValue = crossOrigin.Corp;
```

They exist for the reason presets exist: the three values are the ones people mistype. A builder makes an invalid string
hard to write.

## What the validator checks

`NetSecureHeadersOptionsValidator` runs at startup for the global options **and for every path policy**, and it reports
every problem at once rather than the first:

| Check | Message shape |
| --- | --- |
| `UseCspReportOnly` without `EnableCsp` | "UseCspReportOnly requires EnableCsp to be true. Fix: …" |
| NEL enabled with an empty value / not a JSON object / missing `report_to` or `max_age` | "NelValue must be a JSON object. Fix: …" |
| A path policy with an empty prefix | "Path policy prefix must not be null, empty, or whitespace. Fix: …" |
| Two path policies with the same normalised prefix | "Duplicate path policy prefix '…' is not allowed. …" |
| Two additional headers with the same name | "Duplicate additional header '…' is not allowed. …" |
| An additional header naming a library-owned header | "… is managed by SafeWebCore. Fix: use CustomPolicies …" |
| A reporting endpoint with an empty group, a missing or non-absolute URL, a non-HTTP(S) scheme, or a duplicate group | "… URL must be absolute. Fix: …" |
| The same problems inside a path policy | prefixed with the policy scope, e.g. "Path policy '/api': …" |

Every message names the scope and the fix, because the person reading it is looking at a stack trace in a container log at
the moment a deploy fails.

## Compile-time help

The analyzer package catches the two mistakes that no runtime check can see: registration without
`UseNetSecureHeaders()` (`SWC001`) and a permanently report-only configuration (`SWC002`), plus the two policy smells
(`SWC003` `'unsafe-inline'` without a nonce, `SWC004` an overly broad source). Chapter 17 covers the rules; the point here
is that the validation story has three layers — the compiler, the startup validator, and the diagnostics endpoint — and
each one answers a different question.
