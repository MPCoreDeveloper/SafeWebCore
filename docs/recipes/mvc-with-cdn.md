# Recipe: MVC Application with CDN

This recipe shows a common real-world scenario: an ASP.NET Core MVC application that loads scripts and styles from a CDN while maintaining strong security headers.

## Recommended Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNetSecureHeadersStrictAPlus(opts =>
{
    // Allow your CDN
    opts.Csp = opts.Csp with
    {
        ImgSrc = "'self' https: data:",
        ScriptSrc = "'nonce-{nonce}' 'strict-dynamic' 'self' https://cdn.example.com",
        StyleSrc = "'nonce-{nonce}' 'self' https://cdn.example.com",
        FontSrc = "'self' https://cdn.example.com",
        ConnectSrc = "'self' https://api.example.com"
    };
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseNetSecureHeaders();

app.MapControllerRoute(...);

app.Run();
```

## Key Points

- Use `strict-dynamic` + nonce for scripts
- Explicitly allow the CDN domains you actually use
- Keep `img-src` reasonably open if you use external images
- Consider using environment-aware helpers during development

## Verify

Use the diagnostics endpoint or run your headers through securityheaders.com and Google CSP Evaluator.
