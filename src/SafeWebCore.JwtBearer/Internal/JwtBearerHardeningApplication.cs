using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SafeWebCore.JwtBearer;

/// <summary>
/// Applies <see cref="JwtBearerHardeningOptions"/> to the effective <see cref="JwtBearerOptions"/>.
/// Every rule can only increase strictness; tighter settings already present on the token
/// validation parameters are never weakened.
/// </summary>
internal static class JwtBearerHardeningApplication
{
    private const string TypHeader = "typ";

    /// <summary>
    /// Applies the hardening rules to a <see cref="JwtBearerOptions"/> instance.
    /// </summary>
    /// <param name="options">The effective JWT bearer options.</param>
    /// <param name="hardening">The hardening rules to enforce.</param>
    /// <exception cref="InvalidOperationException">When <c>none</c> is present in <see cref="JwtBearerHardeningOptions.AllowedAlgorithms"/>.</exception>
    public static void Apply(JwtBearerOptions options, JwtBearerHardeningOptions hardening)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(hardening);

        ApplyDefaultValidationRules(options, hardening);
        ApplyAlgorithmRestrictions(options, hardening);
        ApplyIssuerAndAudience(options, hardening);

        var existing = options.Events.OnTokenValidated;
        options.Events.OnTokenValidated = context => OnTokenValidatedAsync(context, existing, hardening);
    }

    private static void ApplyDefaultValidationRules(JwtBearerOptions options, JwtBearerHardeningOptions hardening)
    {
        var parameters = options.TokenValidationParameters;

        if (hardening.RequireSignedTokens)
        {
            parameters.RequireSignedTokens = true;
        }

        if (hardening.RequireExpirationTime)
        {
            parameters.RequireExpirationTime = true;
        }

        if (hardening.ValidateLifetime)
        {
            parameters.ValidateLifetime = true;
        }

        if (hardening.MaximumClockSkew > TimeSpan.Zero)
        {
            parameters.ClockSkew = parameters.ClockSkew <= TimeSpan.Zero
                ? hardening.MaximumClockSkew
                : TimeSpan.FromTicks(Math.Min(parameters.ClockSkew.Ticks, hardening.MaximumClockSkew.Ticks));
        }
    }

    private static void ApplyAlgorithmRestrictions(JwtBearerOptions options, JwtBearerHardeningOptions hardening)
    {
        if (hardening.AllowedAlgorithms.Count == 0)
        {
            return;
        }

        if (hardening.AllowedAlgorithms.Any(algorithm => string.Equals(algorithm, "none", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "The algorithm 'none' must not be allowed in JwtBearerHardeningOptions.AllowedAlgorithms.");
        }

        options.TokenValidationParameters.ValidAlgorithms = hardening.AllowedAlgorithms
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void ApplyIssuerAndAudience(JwtBearerOptions options, JwtBearerHardeningOptions hardening)
    {
        var parameters = options.TokenValidationParameters;

        if (hardening.ValidateIssuer)
        {
            parameters.ValidateIssuer = true;
            if (hardening.ValidIssuers.Count > 0)
            {
                parameters.ValidIssuers = Merge(parameters.ValidIssuers, hardening.ValidIssuers);
            }
        }

        if (hardening.ValidateAudience)
        {
            parameters.ValidateAudience = true;
            parameters.RequireAudience = true;
            if (hardening.ValidAudiences.Count > 0)
            {
                parameters.ValidAudiences = Merge(parameters.ValidAudiences, hardening.ValidAudiences);
            }
        }
    }

    /// <summary>
    /// Chains the claim-based hardening checks (token type, <c>jti</c>, <c>nbf</c>, <c>iat</c>,
    /// maximum lifetime) onto the existing <c>OnTokenValidated</c> handler.
    /// </summary>
    /// <param name="context">The token validated context.</param>
    /// <param name="existing">The previously configured handler, if any.</param>
    /// <param name="hardening">The hardening rules to enforce.</param>
    public static async Task OnTokenValidatedAsync(
        TokenValidatedContext context,
        Func<TokenValidatedContext, Task>? existing,
        JwtBearerHardeningOptions hardening)
    {
        if (existing is not null)
        {
            await existing(context);
            if (context.Result?.Succeeded == false)
            {
                return;
            }
        }

        var token = context.SecurityToken;
        if (token is null)
        {
            return;
        }

        var failure = FindConstraintViolation(token, hardening);
        if (failure is not null)
        {
            context.Fail(failure);
        }
    }

    private static string? FindConstraintViolation(SecurityToken token, JwtBearerHardeningOptions hardening)
    {
        if (hardening.RequireTokenType)
        {
            var type = GetHeaderValue(token, TypHeader);
            if (string.IsNullOrWhiteSpace(type)
                || !hardening.AllowedTokenTypes.Any(candidate => string.Equals(candidate, type, StringComparison.OrdinalIgnoreCase)))
            {
                return $"The JWT typ header '{type}' is not allowed by SafeWebCore.JwtBearer hardening.";
            }
        }

        if (hardening.RequireJwtId && string.IsNullOrWhiteSpace(token.Id))
        {
            return "The JWT does not contain a jti (JWT ID) claim required by SafeWebCore.JwtBearer hardening.";
        }

        if (hardening.RequireNotBefore && token.ValidFrom == default)
        {
            return "The JWT does not contain an nbf (not-before) claim required by SafeWebCore.JwtBearer hardening.";
        }

        if (hardening.RequireIssuedAt && GetIssuedAt(token) == default)
        {
            return "The JWT does not contain an iat (issued-at) claim required by SafeWebCore.JwtBearer hardening.";
        }

        if (hardening.MaximumTokenLifetime is { } maximum
            && maximum > TimeSpan.Zero
            && token.ValidFrom != default
            && token.ValidTo != default
            && token.ValidTo - token.ValidFrom > maximum)
        {
            return $"The JWT lifetime ({token.ValidTo - token.ValidFrom}) exceeds the maximum of {maximum} configured by SafeWebCore.JwtBearer hardening.";
        }

        return null;
    }

    private static string[] Merge(IEnumerable<string>? existing, IList<string> additions)
        => existing is null
            ? additions.Distinct(StringComparer.Ordinal).ToArray()
            : existing.Concat(additions).Distinct(StringComparer.Ordinal).ToArray();

    private static string? GetHeaderValue(SecurityToken token, string key)
    {
        switch (token)
        {
            case JwtSecurityToken jwt:
                return jwt.Header.TryGetValue(key, out var value) ? value as string : null;
            case JsonWebToken json:
                return json.TryGetHeaderValue<object>(key, out var jsonValue) ? jsonValue as string : null;
            default:
                return null;
        }
    }

    private static DateTime GetIssuedAt(SecurityToken token)
        => token switch
        {
            JsonWebToken json => json.IssuedAt,
            JwtSecurityToken jwt => jwt.IssuedAt,
            _ => default,
        };
}