# 6. Content Security Policy

The policy is the part of the library that has to be designed rather than switched on. `CspOptions` is a record with one
init-only string per directive, a `{nonce}` placeholder, and a `Build()` that turns the non-empty values into the header
value.

## The policy object

```csharp
public sealed record CspOptions
{
    public string DefaultSrc { get; init; } = "none";
    public string ScriptSrc { get; init; } = "'nonce-{nonce}' 'strict-dynamic' https:";
    public string StyleSrc { get; init; } = "'nonce-{nonce}'";
    public string ImgSrc { get; init; } = "'self' https: data:";
    public string ObjectSrc { get; init; } = "none";
    public string BaseUri { get; init; } = "none";
    public string FormAction { get; init; } = "'self'";
    public string FrameAncestors { get; init; } = "none";
    public bool EnableUpgradeInsecureRequests { get; init; } = true;
    // …one property per directive, empty string means "not emitted"
}
```

`Build()` is where the value is assembled: a directive with an empty value is skipped entirely, `'none'` stays `'none'`,
and `upgrade-insecure-requests` is appended as a valueless directive when enabled. The result is cached per policy
instance by the middleware — the header is not re-concatenated on every request, only the nonce is substituted.

| Default | Value | Why |
| --- | --- | --- |
| `DefaultSrc` | `'none'` | Deny by default; every allowed source is named explicitly |
| `ScriptSrc` | `'nonce-{nonce}' 'strict-dynamic' https:` | A nonced script may load further scripts; no inline string injection |
| `StyleSrc` | `'nonce-{nonce}'` | Styles need the nonce too, which is why the Tag Helper exists |
| `ImgSrc` | `'self' https: data:` | The combination that almost every site needs |
| `ObjectSrc`, `BaseUri`, `FrameAncestors` | `'none'` | The three most common injection amplifiers |
| `FormAction` | `'self'` | Forms may only post back to you |

## Building a policy

`CspBuilder` is a fluent wrapper over record `with` expressions. Each call returns a new builder holding the updated
record, and `Build()` returns the record.

```csharp
var csp = new CspBuilder()
    .DefaultSrc("'none'")
    .ScriptSrc("'nonce-{nonce}' 'strict-dynamic' https:")
    .StyleSrc("'nonce-{nonce}'")
    .ImgSrc("'self' https: data: https://cdn.example.com")
    .FontSrc("'self' https://fonts.example.com")
    .ConnectSrc("'self' https://api.example.com")
    .FrameAncestors("'none'")
    .UpgradeInsecureRequests()
    .Build();
```

Assign it where the policy belongs:

```csharp
builder.Services.AddNetSecureHeaders(options => options.Csp = csp);
```

Or as part of a preset-and-adjust block (Chapter 4). Because `CspOptions` is a record, this is also a value you can
compare in a test:

```csharp
Assert.Equal("'none'", csp.DefaultSrc);
Assert.Contains("'strict-dynamic'", csp.ScriptSrc, StringComparison.Ordinal);
```

## `{nonce}` is a placeholder, not a template language

The only substitution the library performs is `{nonce}` → the request's nonce. Everything else is literal. That keeps
the value predictable and makes the policy safe to log at startup (the placeholder is not secret). It also means:

- A policy without `{nonce}` is legal — it simply has no nonce, and the Tag Helpers have nothing to inject.
- A policy that relies on `{nonce}` **must** be enforced or reported as a browser would see it; if you strip the
  directive, scripts stop loading. The diagnostics endpoint (Chapter 11) shows the policy with the placeholder intact.
- Two occurrences of `{nonce}` (for example `script-src` and `style-src`) are both substituted. That is how the default
  policy authorises its own styles.

## Report-only and reporting

`UseCspReportOnly = true` sends the same policy in `Content-Security-Policy-Report-Only` instead, which browsers evaluate
but do not enforce. Chapter 10 covers the rollout, `MapSafeWebCoreDiagnostics` shows both modes, and
`WithCspMode(CspEndpointMode.ReportOnly)` (Chapter 9) overrides the mode per endpoint.

Directive-level reporting is available through `ReportUri` (legacy, per-directive URLs) and `ReportTo` (the Reporting API
group name that matches a `ReportingEndpoints` entry):

```csharp
options.ReportingEndpoints =
[
    new ReportingEndpointOptions { Group = "default", Url = "https://reports.example.com/csp" }
];
options.Csp = new CspBuilder().ReportTo("default").Build();
```

## Trusted Types and fenced frames

Two CSP Level 4 features are exposed as plain directives, because they are still rolling out:

```csharp
options.Csp = new CspBuilder()
    .RequireTrustedTypesFor("'script'")
    .TrustedTypes("default policy")
    .FencedFrameSrc("'none'")
    .Build();
```

They are off by default. Turning `require-trusted-types-for 'script'` on is a code change in the application — every
`innerHTML` assignment must go through a Trusted Types policy — and the analyzer rule `SWC004` (Chapter 17) reports a
policy that names a broad source next to them.
