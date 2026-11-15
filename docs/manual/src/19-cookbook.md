# 19. Cookbook

Complete recipes for the situations that come up most often. Each one is a whole configuration, not a fragment.

## A site and an API in one application

The site needs a nonce-based policy; the API needs no browser headers at all.

```csharp
builder.Services.AddNetSecureHeaders(options =>
{
    options.ApplyPreset(SecurePresets.Mvc());

    options.PathPolicies =
    [
        SecurePresets.ApiPath("/api"),
        SecurePresets.ApiPath("/api/files", api => api.EnableCorp = true)
    ];
});

app.UseRouting();
app.UseNetSecureHeaders();
app.MapControllers();
```

## A Blazor Server application with SignalR

The socket needs `connect-src`; everything else stays strict.

```csharp
builder.Services.AddNetSecureHeaders(options =>
{
    options.ApplyPreset(SecurePresets.BlazorWebSocket());
});

app.UseNetSecureHeaders();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
```

## A report-only rollout of a stricter policy

Enforce nothing yet, collect what the browser would block.

```csharp
builder.Services.AddNetSecureHeaders(options =>
{
    options.ApplyPreset(SecurePresets.StrictAPlus());
    options.UseCspReportOnly = true;
    options.ReportingEndpoints =
    [
        new ReportingEndpointOptions { Group = "default", Url = "/csp-report" }
    ];
    options.Csp = new CspBuilder().ReportTo("default").Build();
});

app.UseCspReport();          // POST /csp-report → ICspReportSink
app.UseNetSecureHeaders();
```

Swap `UseCspReportOnly` for an `AddNetSecureHeaders…ForEnvironment(builder.Environment)` call when the decision should
follow the environment rather than a flag (Chapter 10).

## A CDN and an analytics host

Name the sources instead of relaxing the policy.

```csharp
builder.Services.AddNetSecureHeaders(options =>
{
    options.ApplyPreset(SecurePresets.Mvc());

    options.Csp = new CspBuilder()
        .ScriptSrc("'nonce-{nonce}' 'strict-dynamic' https:")
        .StyleSrc("'nonce-{nonce}' https://cdn.example.com")
        .ImgSrc("'self' https://cdn.example.com data:")
        .FontSrc("'self' https://cdn.example.com")
        .ConnectSrc("'self' https://api.example.com https://analytics.example.com")
        .Build();
});
```

## Swagger only in development

The UI's inline script is incompatible with a strict policy, so the policy changes for its path — and only there. There is
no `SwaggerPath` helper: a path policy carries a complete options object, and `SecurePresets.Swagger()` is that object.

```csharp
builder.Services.AddNetSecureHeaders(options =>
{
    options.ApplyPreset(SecurePresets.Mvc());

    if (builder.Environment.IsDevelopment())
    {
        options.PathPolicies =
        [
            new PathPolicyOptions
            {
                PathPrefix = "/swagger",
                Options = SecurePresets.Swagger()
            }
        ];
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}
```

## A health check that nobody grades

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "up" })).SkipNetSecureHeaders();
```

Use it narrowly: a skipped endpoint carries no headers and raises no event (Chapter 9).

## Network Error Logging with a reporting group

```csharp
builder.Services.AddNetSecureHeaders(options =>
{
    options.ApplyPreset(SecurePresets.Mvc());
    options.EnableNel = true;
    options.NelValue = """{"report_to":"default","max_age":2592000}""";
    options.ReportingEndpoints =
    [
        new ReportingEndpointOptions { Group = "default", Url = "https://app.example.com/csp-report" }
    ];
});
```

## A test that proves the shape of the policy

```csharp
using SafeWebCore.Testing;

[Fact]
public async Task Api_IsNotBrowserOriented()
{
    using var host = TestHostBootstrapExtensions.CreateSafeWebCoreTestHost(options =>
        options.ApplyPreset(SecurePresets.Api()));
    using var client = host.GetTestClient();

    var response = await client.GetAsync("/api/orders");

    response.AssertHasSecurityHeaders();
    response.AssertHasNoNonceInCsp();
    response.AssertHeaderExists("X-Frame-Options");
}
```

## Reading the effective policy without a request

```bash
curl -sS "http://localhost:5000/safewebcore/diagnostics?path=/api/orders/42" | jq '.headers'
```

Chapter 11 explains every field the endpoint returns; it is the fastest way to answer "what are we actually sending?".
