using Microsoft.AspNetCore.Authentication.JwtBearer;
using SafeWebCore.JwtBearer.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// SafeWebCore.JwtBearer demo - reproduction of dotnet/aspnetcore#67991
// reported by Stephan van Rooij (@svrooij)
//     https://github.com/dotnet/aspnetcore/issues/67991
//
// FIXED (default): the SafeWebCore guard eagerly validates the authority metadata
//     at startup. HTTP 4xx from the metadata endpoint (the permanent
//     misconfiguration) stops the application with a clear error, or logs an Error
//     if you prefer to keep the app running so all requests fail closed until the
//     authority is fixed.
//
// BROKEN (dotnet run -- --broken): the guard is skipped. The misspelled authority
//     lets the app start and run normally, every request returns 401 invalid_token,
//     and nothing rises above the default Microsoft.AspNetCore: Warning log level.
// ============================================================================
var brokenMode = args.Contains("--broken");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // The classic typo from issue #67991: "organisations" instead of "organizations".
        // The discovery document for this url returns HTTP 400 (not 404), so the app
        // starts fine but no signing keys are ever loaded.
        options.Authority = "https://login.microsoftonline.com/organisations/v2.0";

        // Block insecure HTTP metadata and enforce HTTPS for token validation.
        options.RequireHttpsMetadata = true;

        // Token validation settings taken verbatim from the issue reproduction.
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidAudience = "api://safe-web-core-demo";
        options.TokenValidationParameters.ValidateIssuer = false; // allow tokens from multiple tenants
        options.TokenValidationParameters.ValidateLifetime = true;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(15);
        options.TokenValidationParameters.RequireSignedTokens = true;
        options.TokenValidationParameters.RequireExpirationTime = true;

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // In the broken setup, set a breakpoint here to see:
                //   IDX10500: Signature validation failed. No security keys were provided.
                return Task.CompletedTask;
            },
            OnChallenge = context => Task.CompletedTask,
        };
    });

if (!brokenMode)
{
    // SafeWebCore.JwtBearer fix
    // ------------------------
    // Eagerly loads the OpenID Connect metadata at startup so a broken authority is
    // never a silent availability incident. FailFast = true makes a permanent
    // misconfiguration (HTTP 4xx) fail the startup, exactly as requested in #67991.
    // Set FailFast = false to keep the app running with a loud Error log instead.
    builder.Services.AddJwtBearerAuthorityValidation(options => options.FailFast = true);
}

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/weatherforecast", () => Results.Ok(new { forecast = "Sunny" }))
    .RequireAuthorization();

await app.RunAsync();