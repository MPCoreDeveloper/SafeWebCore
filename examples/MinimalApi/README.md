# SafeWebCore – Minimal API Example

Demonstrates the fastest way to add an **A+ security header** profile to an ASP.NET Core Minimal API application.

## What this example shows

| Feature | Where |
|---------|-------|
| `AddNetSecureHeadersStrictAPlus()` one-liner | `Program.cs` |
| CSP directive customisation with `with { ... }` | `Program.cs` |
| `UseNetSecureHeaders()` middleware registration | `Program.cs` |
| `UseCspReport()` violation reporting endpoint | `Program.cs` |
| `HttpContext.GetCspNonce()` extension method | `Program.cs` `MapGet("/")` |
| Inline `nonce` on `<script>` / `<style>` | `Program.cs` HTML response |
| `SkipNetSecureHeaders()` for health probes | `Program.cs` `MapGet("/health")` |

## Running the example

```bash
cd examples/MinimalApi
dotnet run
```

Then open `http://localhost:5000` in your browser and inspect the response headers in DevTools → Network.

## Verifying headers

You can check the full header set with `curl`:

```bash
curl -sI http://localhost:5000 | grep -Ei "content-security|strict-transport|x-frame|permissions|referrer"
```

Expected output includes:

```
Content-Security-Policy: default-src 'none'; script-src 'nonce-<...>' ...
Strict-Transport-Security: max-age=63072000; includeSubDomains; preload
X-Frame-Options: DENY
Permissions-Policy: camera=(), microphone=(), geolocation=(), ...
Referrer-Policy: no-referrer
```

## CSP Report-Only mode

Switch to report-only mode during development to test a stricter policy before enforcing it:

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.UseCspReportOnly = true;
});
```
