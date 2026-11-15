# 18. Architecture and Compatibility

## Layers

The library is layered so that a contract can be read without the machinery behind it:

| Layer | Namespaces and folders | Owns |
| --- | --- | --- |
| Contracts | `Abstractions`, `Options`, `Models`, `Constants`, `Metadata`, `Attributes` | Interfaces, option groups, value records, header names, endpoint metadata |
| Adapters | `Infrastructure`, `Builder`, `TagHelpers` | Implementations behind the ports; the fluent builders |
| Pipeline | `Middleware` | The request filter that writes headers and raises events |
| Composition root | `Extensions` | The only place that registers services and adds middleware; `Presets` is data it consumes |
| Distribution | `SafeWebCore.Analyzers`, `SafeWebCore.Testing` | Build-time diagnostics and test helpers; outside the runtime graph |

Two rules make the layering observable rather than aspirational. **No package references another** (except
`SafeWebCore.Testing` → `SafeWebCore`): shared behaviour is promoted into the core package or deliberately duplicated, never
reached sideways. And **exactly one composition root**: every registration goes through the `Extensions` classes, so a
question about "what gets registered" has one file to read.

## One resolver, two callers

The middleware and the diagnostics endpoint both resolve the effective policy through the same internal resolver
(`PathPolicyResolver`). That is not a convenience; it is the property that makes the preview trustworthy. If the two had
separate copies of the longest-prefix rule, the preview would eventually describe a configuration that is not applied —
and the first person to notice would be the one debugging an incident.

The same idea applies to the nonce: `INonceService` is a port, `NonceService` is the implementation, and the pipeline
depends on the port. A test can substitute a deterministic nonce; an application can substitute a faster generator.

## State and concurrency

- **Options are read once.** The middleware builds its path policies, CSP templates and Reporting-Endpoints values in its
  constructor. Changing `IOptions` at runtime does not change the pipeline; a configuration change is a restart.
- **Nothing per request is shared.** The nonce lives in `HttpContext.Items`; the header dictionary belongs to the response.
  The middleware is safe under concurrent requests by construction, not by locking.
- **Dispatchers isolate failures.** Both the security-event dispatcher and the fraud-event dispatcher invoke each sink
  inside its own `try` and count a failure. A broken observer cannot break a response and cannot stop the next sink.

## The compatibility promise

Normal releases are **100% backward compatible**: additive, opt-in changes only. In practice that means:

| Change | Allowed in a normal release? |
| --- | --- |
| A new option with a safe default | Yes |
| A new overload, a new extension method, a new package | Yes |
| A new directive or header behind an opt-in flag | Yes |
| A changed default value | No |
| A changed preset value or emitted header value | No |
| A removed or renamed public member | No (`RS0037` is a build error) |

The mechanism behind the last row is the **public API baseline**: `PublicAPI.Shipped.txt` in each package lists the surface
that has shipped, `PublicAPI.Unshipped.txt` lists what a release is about to add, and the analyzer makes removing a shipped
symbol a compile error rather than a review finding. A change to a public member is therefore a deliberate, reviewed event
— and a bug in the baseline was a release blocker.

Documented output counts as API: the headers a preset emits, the default values in Chapter 5, the instrument names of
Chapter 12. That is why a preset can grow but not relax.

## Two target frameworks, one surface

The runtime packages ship `lib/net10.0` and `lib/net11.0` with the same public API. A .NET 11 application resolves the build
compiled against .NET 11 (C# 15, the .NET 11 JIT); a .NET 10 application keeps the `net10.0` build, whose dependency floors
are the ones that shipped in 1.8.1. `SafeWebCore.Analyzers` stays `netstandard2.0` because the compiler host loads it, and
the compiler host is not always the runtime you target.

## How the promise is proven

| Gate | What it proves |
| --- | --- |
| `dotnet test` on both target frameworks (380 test runs) | The same behaviour on .NET 10 and .NET 11 |
| `TreatWarningsAsErrors` | No degraded diagnostic ships silently |
| `RS0037` on the public API baseline | No shipped symbol disappeared |
| SonarCloud all-A with zero open issues | The library is not the weakest link in a security review (Chapter 20 mentions the gate; the triage page has the queries) |
| The mutation-testing baseline | The tests fail when the behaviour changes, not only when the code is removed |

> **The compatibility promise is a product decision, not a technical one.** A security middleware is upgraded in place,
> inside applications whose authors are not reading this manual; a silent behaviour change is an incident they did not
> schedule.
