# SafeWebCore – MVC Example

Demonstrates SafeWebCore in a full ASP.NET Core MVC application with Razor Views, typed policy builders, path-based policies, and automatic nonce injection via TagHelpers.

## What this example shows

| Feature | Where |
|---------|-------|
| `AddNetSecureHeadersMvcPreset()` | `Program.cs` |
| `ReferrerPolicyBuilder` typed builder | `Program.cs` |
| `PermissionsPolicyBuilder` typed builder | `Program.cs` |
| `CrossOriginPolicyBuilder` typed builder | `Program.cs` |
| Path-based policy override (`/public` → report-only CSP) | `Program.cs` |
| `[CspNonce]` attribute on a controller | `Controllers/HomeController.cs` |
| `@addTagHelper *, SafeWebCore` | `Views/_ViewImports.cshtml` |
| Auto-nonce on `<script>` / `<style>` via TagHelpers | `Views/Home/Index.cshtml`, `Views/Shared/_Layout.cshtml` |
| Explicit nonce via `ViewData["CspNonce"]` | `Views/Home/Index.cshtml` |

## Running the example

```bash
cd examples/MvcApp
dotnet run
```

Open `http://localhost:5000` and inspect the response headers in DevTools → Network.

## TagHelper nonce injection

After adding `@addTagHelper *, SafeWebCore` to `_ViewImports.cshtml`, every `<script>` and `<style>` tag in Razor views automatically receives a `nonce="…"` attribute — no manual plumbing required.

```html
<!-- In any Razor view — nonce is added automatically -->
<script>
    console.log('Allowed by CSP!');
</script>

<style>
    body { font-family: sans-serif; }
</style>
```

## Path policies

The `/public` route prefix uses a separate policy with `UseCspReportOnly = true`. All other routes use the strict enforced CSP from the MVC preset. This lets you roll out tighter policies gradually.

## Typed builders

Use the fluent builders for non-CSP headers instead of raw strings:

```csharp
opts.PermissionsPolicyValue = new PermissionsPolicyBuilder()
    .Disable(PermissionsFeature.Camera)
    .AllowSelf(PermissionsFeature.Geolocation)
    .Build();
```
