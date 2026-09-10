using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using SafeWebCore.JwtBearer;

namespace SafeWebCore.JwtBearer.Tests;

public sealed class JwtBearerHardeningTests
{
    [Fact]
    public void ApplyEnforcesSignedTokensLifetimeAndExpirationByDefault()
    {
        var options = new JwtBearerOptions();
        var hardening = new JwtBearerHardeningOptions();

        JwtBearerHardeningApplication.Apply(options, hardening);

        Assert.True(options.TokenValidationParameters.RequireSignedTokens);
        Assert.True(options.TokenValidationParameters.RequireExpirationTime);
        Assert.True(options.TokenValidationParameters.ValidateLifetime);
    }

    [Fact]
    public void ApplyKeepsTighterClockSkew()
    {
        var options = new JwtBearerOptions();
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(2);
        var hardening = new JwtBearerHardeningOptions { MaximumClockSkew = TimeSpan.FromMinutes(5) };

        JwtBearerHardeningApplication.Apply(options, hardening);

        Assert.Equal(TimeSpan.FromMinutes(2), options.TokenValidationParameters.ClockSkew);
    }

    [Fact]
    public void ApplyRestrictsAlgorithmsAndMergesAudiences()
    {
        var options = new JwtBearerOptions();
        options.TokenValidationParameters.ValidAudiences = new[] { "spa" };
        var hardening = new JwtBearerHardeningOptions { ValidateAudience = true };
        hardening.ValidAudiences.Add("api");
        hardening.AllowedAlgorithms.Add("RS256");

        JwtBearerHardeningApplication.Apply(options, hardening);

        Assert.True(options.TokenValidationParameters.ValidateAudience);
        Assert.True(options.TokenValidationParameters.RequireAudience);
        Assert.Contains("api", options.TokenValidationParameters.ValidAudiences!);
        Assert.Contains("spa", options.TokenValidationParameters.ValidAudiences!);
        Assert.Equal("RS256", Assert.Single(options.TokenValidationParameters.ValidAlgorithms!));
    }

    [Fact]
    public void ApplyThrowsWhenNoneAlgorithmIsAllowed()
    {
        var options = new JwtBearerOptions();
        var hardening = new JwtBearerHardeningOptions();
        hardening.AllowedAlgorithms.Add("none");

        Assert.Throws<InvalidOperationException>(() => JwtBearerHardeningApplication.Apply(options, hardening));
    }

    [Fact]
    public async Task OnTokenValidatedRejectsDisallowedTokenType()
    {
        var context = CreateContext(BuildToken(typ: "pop"));
        var hardening = new JwtBearerHardeningOptions();

        await JwtBearerHardeningApplication.OnTokenValidatedAsync(context, existing: null, hardening);

        Assert.False(context.Result?.Succeeded);
    }

    [Fact]
    public async Task OnTokenValidatedAcceptsAllowedTokenType()
    {
        var context = CreateContext(BuildToken(typ: "at+jwt"));
        var hardening = new JwtBearerHardeningOptions();

        await JwtBearerHardeningApplication.OnTokenValidatedAsync(context, existing: null, hardening);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnTokenValidatedRejectsMissingJwtId()
    {
        var context = CreateContext(BuildToken(typ: "JWT", jti: null));
        var hardening = new JwtBearerHardeningOptions { RequireJwtId = true, RequireTokenType = false };

        await JwtBearerHardeningApplication.OnTokenValidatedAsync(context, existing: null, hardening);

        Assert.False(context.Result?.Succeeded);
    }

    [Fact]
    public async Task OnTokenValidatedRejectsExcessiveLifetime()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var context = CreateContext(BuildToken(typ: "JWT", notBefore: now - 120, expiresAt: now + 7200));
        var hardening = new JwtBearerHardeningOptions { MaximumTokenLifetime = TimeSpan.FromHours(1), RequireTokenType = false };

        await JwtBearerHardeningApplication.OnTokenValidatedAsync(context, existing: null, hardening);

        Assert.False(context.Result?.Succeeded);
    }

    [Fact]
    public async Task OnTokenValidatedStopsWhenExistingHandlerFailed()
    {
        var context = CreateContext(BuildToken(typ: "JWT"));
        var hardening = new JwtBearerHardeningOptions();
        static Task Existing(TokenValidatedContext ctx)
        {
            ctx.Fail("existing failure");
            return Task.CompletedTask;
        }

        await JwtBearerHardeningApplication.OnTokenValidatedAsync(context, Existing, hardening);

        Assert.False(context.Result?.Succeeded);
        Assert.StartsWith("existing failure", context.Result?.Failure?.Message);
    }

    private static TokenValidatedContext CreateContext(JsonWebToken token)
    {
        var scheme = new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
        return new TokenValidatedContext(new DefaultHttpContext(), scheme, new JwtBearerOptions())
        {
            SecurityToken = token,
        };
    }

    private static JsonWebToken BuildToken(string typ, string? jti = "jti-123", long? notBefore = null, long? expiresAt = null)
    {
        var encoder = Base64UrlEncoder();
        var header = encoder(Encoding.UTF8.GetBytes($"{{\"alg\":\"none\",\"typ\":\"{typ}\"}}"));
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var parts = new List<string>
        {
            $"\"aud\":\"api\"",
            $"\"exp\":{expiresAt ?? now + 3600}",
            $"\"iat\":{now - 120}",
        };

        if (notBefore is not null)
        {
            parts.Add($"\"nbf\":{notBefore}");
        }

        if (jti is not null)
        {
            parts.Add($"\"jti\":\"{jti}\"");
        }

        var payload = encoder(Encoding.UTF8.GetBytes("{" + string.Join(",", parts) + "}"));
        return new JsonWebToken($"{header}.{payload}.sig");
    }

    private static Func<byte[], string> Base64UrlEncoder()
        => bytes => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}