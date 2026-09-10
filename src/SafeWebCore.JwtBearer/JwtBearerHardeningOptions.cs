namespace SafeWebCore.JwtBearer;

/// <summary>
/// Optional hardening rules applied by <c>AddJwtBearerHardening</c> / <c>AddSafeWebCoreJwtBearer</c>.
/// Every rule is opt-in or safe by default and can only ever increase strictness; existing
/// (stronger) settings on the <c>JwtBearerOptions</c> are never weakened.
/// </summary>
public sealed class JwtBearerHardeningOptions
{
    /// <summary>
    /// Requires tokens to be signed. When true, <see cref="Microsoft.IdentityModel.Tokens.TokenValidationParameters.RequireSignedTokens"/>
    /// is enforced so <c>alg: none</c> tokens are rejected.
    /// </summary>
    public bool RequireSignedTokens { get; set; } = true;

    /// <summary>
    /// Requires a token to carry an <c>exp</c> claim.
    /// </summary>
    public bool RequireExpirationTime { get; set; } = true;

    /// <summary>
    /// Enforces lifetime validation (<c>nbf</c> and <c>exp</c> ranges).
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Requires the JWT <c>typ</c> header to be present and to match one of
    /// <see cref="AllowedTokenTypes"/>.
    /// </summary>
    public bool RequireTokenType { get; set; } = true;

    /// <summary>
    /// The allowed JWT <c>typ</c> header values when <see cref="RequireTokenType"/> is true.
    /// </summary>
    public IList<string> AllowedTokenTypes { get; } = new List<string> { "JWT", "at+jwt" };

    /// <summary>
    /// Requires the token to carry a unique <c>jti</c> (JWT ID) claim.
    /// </summary>
    public bool RequireJwtId { get; set; }

    /// <summary>
    /// Requires the token to carry an <c>nbf</c> (not before) claim.
    /// </summary>
    public bool RequireNotBefore { get; set; }

    /// <summary>
    /// Requires the token to carry an <c>iat</c> (issued at) claim.
    /// </summary>
    public bool RequireIssuedAt { get; set; }

    /// <summary>
    /// The maximum allowed <see cref="Microsoft.IdentityModel.Tokens.TokenValidationParameters.ClockSkew"/>. The tighter of the
    /// existing clock skew and this value is applied.
    /// </summary>
    public TimeSpan MaximumClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When set, rejects tokens whose valid lifetime (<c>exp - nbf</c>) exceeds this duration.
    /// A common limit for access tokens is one hour.
    /// </summary>
    public TimeSpan? MaximumTokenLifetime { get; set; }

    /// <summary>
    /// When non-empty, restricts signature validation to the listed JWS algorithms
    /// (for example <c>RS256</c>, <c>RS384</c>, <c>RS512</c>, <c>ES256</c>, <c>ES384</c>,
    /// <c>ES512</c>, <c>PS256</c>, <c>PS384</c>, <c>PS512</c>).
    /// </summary>
    public IList<string> AllowedAlgorithms { get; } = new List<string>();

    /// <summary>
    /// Enforces issuer validation. Add the expected issuer(s) to <see cref="ValidIssuers"/>.
    /// Multi-tenant applications either leave this off (and use a converter) or list every
    /// tenant issuer explicitly.
    /// </summary>
    public bool ValidateIssuer { get; set; }

    /// <summary>
    /// The allowed issuer(s) used when <see cref="ValidateIssuer"/> is true. These are appended
    /// to any values already configured on the token validation parameters.
    /// </summary>
    public IList<string> ValidIssuers { get; } = new List<string>();

    /// <summary>
    /// Enforces audience validation. Add the expected audience(s) to <see cref="ValidAudiences"/>.
    /// </summary>
    public bool ValidateAudience { get; set; }

    /// <summary>
    /// The allowed audience(s) used when <see cref="ValidateAudience"/> is true. These are appended
    /// to any values already configured on the token validation parameters.
    /// </summary>
    public IList<string> ValidAudiences { get; } = new List<string>();

    /// <summary>
    /// When true (default), the OpenID Connect metadata <see cref="Microsoft.IdentityModel.Protocols.IConfigurationManager{T}"/>
    /// is wrapped with a logging decorator so metadata retrieval failures surf at Error
    /// (HTTP 4xx) or Warning (transient) level during runtime as well as at startup; without it
    /// the framework only logs such failures at Information level.
    /// </summary>
    public bool EnableRuntimeMetadataLogging { get; set; } = true;
}
