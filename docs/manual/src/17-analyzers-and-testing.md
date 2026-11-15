# 17. Analyzers and the Testing Package

Two small packages exist so that a mistake is caught before a scanner, a user or an incident finds it: `SafeWebCore.Analyzers`
looks at the code that configures the middleware, and `SafeWebCore.Testing` makes the headers assertable in a unit test.

## SafeWebCore.Analyzers

The analyzer assembly targets `netstandard2.0` — it is loaded into the compiler, not into your application — and reports
four rules:

| Rule | Severity | Fires when | Fix |
| --- | --- | --- | --- |
| `SWC001` | Warning | A registration method (`AddNetSecureHeaders…`) is called and `UseNetSecureHeaders()` never is | Add the middleware, or remove the registration |
| `SWC002` | Warning | `UseCspReportOnly` is set to `true` without an environment check | Enforce in production, or make the environment decision explicit |
| `SWC003` | Warning | A CSP source contains `'unsafe-inline'` and no nonce | Use `'nonce-{nonce}'` and the Tag Helpers (Chapter 7) |
| `SWC004` | Warning | A CSP source is overly broad (`*`, a bare `https:`, `'unsafe-eval'`, `data:` in `script-src`) | Name the sources the site actually needs |

`SWC001` is the rule that pays for the package. Registration without the middleware is the classic ASP.NET Core mistake:
the services resolve, the application starts, and no header is ever written.

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus();   // SWC001: registration, but UseNetSecureHeaders() is missing

var app = builder.Build();
app.MapGet("/", () => "Hello");
```

The rules are `Warning`, not `Error`, because a codebase may legitimately stage a configuration. In a repository that sets
`TreatWarningsAsErrors` they are errors anyway, which is the intended outcome for an application.

## SafeWebCore.Testing

Header tests are repetitive and easy to write badly — asserting that one header exists, forgetting the other twelve. The
testing package turns the important combinations into one call each:

| Helper | Asserts |
| --- | --- |
| `AssertHasSecurityHeaders()` | The full set the library emits by default is present |
| `AssertHeaderExists(name)` | A specific header is present |
| `AssertHasCspEnforceMode()` | CSP is sent as `Content-Security-Policy` |
| `AssertHasCspReportOnlyMode()` | CSP is sent as `Content-Security-Policy-Report-Only` |
| `AssertHasNonceInCsp()` | The policy carries a `'nonce-…'` source |
| `AssertHasNoNonceInCsp()` | The policy does not |

`TestHostBootstrapExtensions.CreateSafeWebCoreTestHost(...)` builds a host with the middleware wired, optionally with your
own services or options:

```csharp
using SafeWebCore.Testing;

[Fact]
public async Task Api_DoesNotGetBrowserHeaders()
{
    using var host = TestHostBootstrapExtensions.CreateSafeWebCoreTestHost(options =>
    {
        options.ApplyPreset(SecurePresets.Api());
    });
    using var client = host.GetTestClient();

    var response = await client.GetAsync("/api/orders");

    response.AssertHasSecurityHeaders();
    response.AssertHasCspEnforceMode();
    response.AssertHasNoNonceInCsp();
}
```

The assertions live in `SafeWebCore.Testing` because a consumer's test suite should not have to know which twelve headers
the library emits today; the helper does, and it changes when the library does. The package depends on `xunit.v3.assert`,
so it is for **xUnit v3** suites — the same runner this repository uses.

> **Preview status.** `SafeWebCore.Testing` and `SafeWebCore.Analyzers` ship as `1.0.0-preview.N`. They are additive and
> supported, but their API may still change with a preview bump; pin a version and read the release notes when you move.
