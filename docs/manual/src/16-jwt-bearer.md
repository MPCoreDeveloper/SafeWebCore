# 16. SafeWebCore.JwtBearer

`SafeWebCore.JwtBearer` is a companion package that fixes a specific, expensive incident: a JWT authority with a typo
answers `401` to every request, and nothing in the log says why. The metadata failure is logged at Information level,
below the default filter, so the application looks like it is rejecting tokens rather than failing to reach its identity
provider.

The package wraps the handler instead of replacing it. Your authentication stays what it was; the configuration and the
metadata traffic become observable, and the token validation parameters become explicit.

## Three entry points

```csharp
using SafeWebCore.JwtBearer;

// 1. the authority guard: fail fast, or fail loud, on a broken authority
builder.Services.AddJwtBearerAuthorityValidation(options =>
{
    options.Scheme = "Bearer";
    options.FailFast = true;
    options.RequireSigningKeys = true;
});

// 2. token hardening: the validation parameters an API usually forgets
builder.Services.AddJwtBearerHardening(options =>
{
    options.ValidateAudience = true;
    options.ValidateIssuer = true;
    options.MaximumTokenLifetime = TimeSpan.FromHours(1);
});

// 3. both, in one call
builder.Services.AddSafeWebCoreJwtBearer();
```

## The authority guard

| Option | Default | Meaning |
| --- | --- | --- |
| `Scheme` | the default scheme | Which registered JWT bearer scheme to validate |
| `FailFast` | `false` | Throw at startup instead of logging an Error and continuing |
| `EnforceStaticConfigurationChecks` | `true` | Reject an authority that is not an absolute URI, or a metadata address that cannot be built |
| `RequireSigningKeys` | `true` | Treat "the metadata returned no signing key" as a configuration failure |
| `PeriodicValidationInterval` | `null` | Re-check the authority on an interval while the application runs |

With `FailFast = false` the guard still changes the incident: every metadata retrieval failure is logged at **Error** for a
permanent failure (HTTP 4xx — the typo case) and at **Warning** for a transient one, at most once per refresh interval.
That log line is the difference between "our auth is down" and "the authority URL is misspelled".

With `FailFast = true` a permanent failure stops startup:

```text
InvalidOperationException: The OpenID Connect metadata for 'https://login.example.com/organisations/...'
could not be retrieved (permanent configuration error, HTTP 4xx). Requests would fail closed until this is fixed.
```

That is the setting to use in a deployment you would rather not start than start broken.

## Token hardening

`JwtBearerHardeningOptions` maps onto `TokenValidationParameters`, with defaults chosen for an API:

| Option | Default | Effect |
| --- | --- | --- |
| `RequireSignedTokens` | `true` | An unsigned token is rejected (`alg: none`) |
| `RequireExpirationTime` | `true` | A token without `exp` is rejected |
| `ValidateLifetime` | `true` | Expiry and not-before are enforced |
| `RequireTokenType` | `true` | `typ` must be in `AllowedTokenTypes` (`JWT`, `at+jwt`) |
| `RequireJwtId`, `RequireNotBefore`, `RequireIssuedAt` | `false` | Opt in when your issuer sets them |
| `MaximumClockSkew` | 5 minutes | The tolerance the framework applies |
| `MaximumTokenLifetime` | `null` | Reject a token issued to live longer than this |
| `ValidateIssuer`, `ValidateAudience` | `false` | Turn on, with `ValidIssuers` / `ValidAudiences` |
| `EnableRuntimeMetadataLogging` | `true` | Keep the loud metadata logging at runtime |

`AllowedAlgorithms` is an allow-list: when it is non-empty, a token signed with anything else is rejected. It is empty by
default because the correct list depends on the issuer — and an empty list means "whatever the metadata offers", which is
the framework's own behaviour.

```csharp
builder.Services.AddJwtBearerHardening(options =>
{
    options.AllowedAlgorithms.Add("RS256");
    options.RequireJwtId = true;
    options.MaximumTokenLifetime = TimeSpan.FromMinutes(30);
    options.ValidAudiences.Add("api://orders");
});
```

## What the package needs from you

The package references `Microsoft.AspNetCore.Authentication.JwtBearer` per target — `10.0.12` on `net10.0` and `11.0.0`
on `net11.0` — because each ASP.NET Core major ships for its own runtime. It does not register a scheme: you still call
`AddAuthentication().AddJwtBearer(...)`, and the guard decorates the configuration manager that handler uses.

> **The package is a guard, not a validator.** It never issues, refreshes or revokes a token, and it does not decide
> authorisation. It makes a misconfiguration loud and the validation parameters explicit — which is exactly the part that
> silently goes wrong.
