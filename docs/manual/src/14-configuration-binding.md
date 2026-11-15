# 14. Configuration Binding

Hard-coding a policy in `Program.cs` is fine until operations needs to tighten it without a build. Binding gives the whole
options model to `appsettings.json` (or any `IConfiguration` source), with the same startup validation and the same
defaults.

## The two overloads

```csharp
builder.Services.AddNetSecureHeadersFromConfiguration(builder.Configuration);          // section "NetSecureHeaders"
builder.Services.AddNetSecureHeadersFromConfiguration(builder.Configuration.GetSection("Security:Headers"));
```

```csharp
public static IServiceCollection AddNetSecureHeadersFromConfiguration(
    this IServiceCollection services,
    IConfiguration configuration,
    string sectionName = "NetSecureHeaders");

public static IServiceCollection AddNetSecureHeadersFromConfiguration(
    this IServiceCollection services,
    IConfigurationSection section);
```

Both bind `NetSecureHeadersOptions` with `.Bind(section)` and `.ValidateOnStart()`: a configuration file that cannot work
fails at startup with the same messages as a code configuration (Chapter 5).

## A file that states the policy

```json
{
  "NetSecureHeaders": {
    "EnableHsts": true,
    "HstsValue": "max-age=31536000; includeSubDomains",
    "EnableOriginAgentCluster": true,
    "RemoveXPoweredBy": true,
    "Csp": {
      "DefaultSrc": "'none'",
      "ScriptSrc": "'nonce-{nonce}' 'strict-dynamic' https:",
      "StyleSrc": "'nonce-{nonce}'",
      "ImgSrc": "'self' https: data: https://cdn.example.com",
      "ConnectSrc": "'self' https://api.example.com",
      "FrameAncestors": "'none'",
      "EnableUpgradeInsecureRequests": true
    },
    "ReportingEndpoints": [
      { "Group": "default", "Url": "https://app.example.com/csp-report" }
    ],
    "PathPolicies": [
      {
        "PathPrefix": "/api",
        "Options": {
          "Csp": { "DefaultSrc": "'none'", "FrameAncestors": "'none'" }
        }
      }
    ],
    "AdditionalHeaders": [
      { "Name": "X-Content-Language", "Value": "en" }
    ]
  }
}
```

Three binding details are worth knowing:

- **Lists bind by index** (`ReportingEndpoints:0:Group`), which the array form above produces. A JSON array is the readable
  way to write it; an environment variable or a key-per-line source has to spell the index.
- **`Csp` is a record with init-only properties**, so a partially specified object binds onto a **default-constructed**
  `CspOptions`, not onto the preset's value. If you bind a policy, bind every directive you care about, or accept the
  `CspOptions` defaults for the rest (Chapter 6 lists them).
- **`CustomPolicies` cannot bind.** It is a list of `IHeaderPolicy` instances; a configuration file cannot name a type
  safely. Register those in code after the binding.

## Adjusting after the binding

`AddNetSecureHeadersFromConfiguration` registers the bound options; a later `Configure` callback sees the bound value and
can adjust it, because options configuration callbacks run in registration order.

```csharp
builder.Services.AddNetSecureHeadersFromConfiguration(builder.Configuration);

builder.Services.Configure<NetSecureHeadersOptions>(options =>
{
    options.CustomPolicies.Add(new RequestIdHeaderPolicy());   // code-only extension point
});
```

The reverse order does not work: a `Configure` registered *before* the binding is overwritten by the binding.

## What belongs in configuration, and what does not

| Belongs | Does not belong |
| --- | --- |
| Header toggles and values | A custom policy class (register in code) |
| The CSP directives | A nonce (generated per request) |
| Path prefixes and their policies | The environment decision (use the `ForEnvironment` overloads, Chapter 10) |
| Reporting endpoints | Secrets — the policy is public, but keep internal hostnames out of a shared file |

Because every value here is public by design — a browser receives all of it on every response — configuration binding adds
convenience, not confidentiality. Review an `appsettings.Production.json` change the way you review a code change: it *is*
a change to what the browser is told.
