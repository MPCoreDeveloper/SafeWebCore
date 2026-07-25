# Recipe: Blazor with Nonce-based CSP

Blazor (especially Blazor Server and Blazor WebAssembly with server-side prerendering) has specific CSP requirements.

## Recommended Configuration

```csharp
builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    opts.Csp = opts.Csp with
    {
        // Blazor needs these for WebSockets, blazor.server.js, etc.
        ConnectSrc = "'self' wss:",
        ScriptSrc = "'nonce-{nonce}' 'strict-dynamic' 'self'",
        StyleSrc = "'nonce-{nonce}' 'self'",
        ImgSrc = "'self' data:",
        FontSrc = "'self'",
        WorkerSrc = "'self' blob:"
    };
});
```

## Important Notes for Blazor

- Blazor Server uses SignalR / WebSockets → allow `wss:`
- Blazor often injects inline styles/scripts during initial render
- For Blazor WebAssembly, you may need to allow the WASM loader and `blob:`

## Using Nonces in Blazor Components

```razor
@inject Microsoft.AspNetCore.Http.IHttpContextAccessor HttpContextAccessor

<script nonce="@HttpContextAccessor.HttpContext?.GetCspNonce()">
    // your script
</script>
```

Or use the TagHelper if you registered it.

## Alternative for Simpler Projects

Use the environment-aware helper during development:

```csharp
builder.Services.AddNetSecureHeadersForEnvironment(builder.Environment, opts =>
{
    // Relax only what Blazor actually needs
});
```
