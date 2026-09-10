using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SafeWebCore.JwtBearer.Extensions;

namespace SafeWebCore.JwtBearer.Tests;

/// <summary>
/// Recreates Stephan van Rooij's exact reproduction from dotnet/aspnetcore#67991
/// (the misspelled authority <c>https://login.microsoftonline.com/organisations/v2.0</c>) and
/// proves both the faulty behavior (app starts, silent 401s) and the SafeWebCore fix.
/// The real network call is replaced by a deterministic HTTP stub so the tests never leave the machine.
/// </summary>
public sealed class StephanReproIntegrationTests
{
    private const string FaultyAuthority = "https://login.microsoftonline.com/organisations/v2.0";
    private const string CorrectAuthority = "https://login.microsoftonline.com/organizations/v2.0";

    private const string FakeOpenIdConfiguration = """
        {
            "issuer": "https://login.microsoftonline.com/organizations/v2.0",
            "jwks_uri": "https://login.microsoftonline.com/organizations/v2.0/discovery/v2.0/keys",
            "keys": [
                {
                    "kty": "RSA",
                    "use": "sig",
                    "kid": "test-key",
                    "e": "AQAB",
                    "n": "0vx7agoebGcQSuuPiLJXZptN9nndrQmbXEps2aiAFbWhM78LhWx4cbbfAAtVT86zwu1RK7aPFFxuhDR1L6tSoc_BJECPebWKRXjBZCiFV4n3oknjhMstn64tZ_2W-5JsGY4Hc5n9yBXArwl93lqt7_RN5w6Cf0h4QyQ5v-65YGjQR0_FDW2QvzqY368QQMicAtaSqzs8KJZgnYb9c7d0zgdAZHzu6qMQvRL5hajrn1n91CbOpbISD08qNLyrdkt-bFTWhAI4vMQFh6WeZu0fM4lFd2NcRwr3XPksINHaQ-G_xBniIqbw0Ls1jF44-csFCur-kEgU8awapJzKnqDKgw"
                }
            ]
        }
        """;

    [Fact]
    public async Task FaultyAuthorityWithoutGuardStartsAnd401sSilently()
    {
        var records = new List<string>();
        await using var app = BuildReproApp(addGuard: false, failFast: false, records);

        await app.StartAsync(TestContext.Current.CancellationToken); // app starts normally - exactly what Stephan reported

        var client = app.GetTestServer().CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/weatherforecast");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateFaultyToken());

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain(records, record => record.Contains("JwtBearer", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(records, record => record.Contains("Failed to validate the token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FaultyAuthorityWithFailFastGuardDoesNotStart()
    {
        var records = new List<string>();
        await using var app = BuildReproApp(addGuard: true, failFast: true, records);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => app.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("JWT authority", exception.Message);
        Assert.Contains(FaultyAuthority, exception.Message);
    }

    [Fact]
    public async Task FaultyAuthorityWithLoudGuardStartsWithErrorLog()
    {
        var records = new List<string>();
        await using var app = BuildReproApp(addGuard: true, failFast: false, records);

        await app.StartAsync(TestContext.Current.CancellationToken);

        Assert.Contains(records, record => record.Contains("permanent configuration error", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(records, record => record.Contains(FaultyAuthority, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CorrectAuthorityWithFailFastGuardStartsClean()
    {
        var records = new List<string>();
        await using var app = BuildReproApp(addGuard: true, failFast: true, records, authority: CorrectAuthority, faulty: false);

        await app.StartAsync(TestContext.Current.CancellationToken);

        Assert.Contains(records, record => record.Contains("metadata loaded successfully", StringComparison.OrdinalIgnoreCase));
    }

    private static WebApplication BuildReproApp(
        bool addGuard,
        bool failFast,
        List<string> records,
        string? authority = null,
        bool faulty = true)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning); // default template filter
        builder.Logging.AddProvider(new RecordingLoggerProvider(records));

        var handler = faulty
            ? new StubHttpMessageHandler(HttpStatusCode.BadRequest, "The tenant is invalid.")
            : new StubHttpMessageHandler(HttpStatusCode.OK, FakeOpenIdConfiguration);

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority ?? FaultyAuthority;
                options.BackchannelHttpHandler = handler;
                options.RequireHttpsMetadata = true;

                // Stephan's exact TokenValidationParameters from dotnet/aspnetcore#67991
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidateIssuer = false;
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(15);
                options.TokenValidationParameters.RequireSignedTokens = true;
                options.TokenValidationParameters.RequireExpirationTime = true;
                options.TokenValidationParameters.ValidAudience = "api://safe-web-core-repro";
            });
        builder.Services.AddAuthorization();

        if (addGuard)
        {
            builder.Services.AddJwtBearerAuthorityValidation(o => o.FailFast = failFast);
        }

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/weatherforecast", () => Results.Ok(new { forecast = "sunny" })).RequireAuthorization();
        return app;
    }

    private static string CreateFaultyToken()
    {
        var header = Base64Url(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\"}"));
        var payload = Base64Url(Encoding.UTF8.GetBytes("{\"sub\":\"stefan-token\"}"));
        return $"{header}.{payload}.not-a-real-signature";
    }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');


    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content),
            });
    }

    private sealed class RecordingLoggerProvider(List<string> records) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new RecordingLogger(records, categoryName);

        public void Dispose()
        {
        }
    }

    private sealed class RecordingLogger(List<string> records, string category) : ILogger
    {
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            records.Add($"[{category}] {logLevel}: {formatter(state, exception)}");
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}