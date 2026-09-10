namespace SafeWebCore.JwtBearer;

/// <summary>
/// Options for the <see cref="JwtAuthorityValidationGuard"/>.
/// </summary>
public sealed class JwtAuthorityValidationOptions
{
    /// <summary>
    /// The authentication scheme to validate. Defaults to the JWT bearer default scheme ("Bearer").
    /// </summary>
    public string? Scheme { get; set; }

    /// <summary>
    /// When true, a permanent authority misconfiguration (HTTP 4xx from the metadata endpoint,
    /// e.g. a typo such as "organisations") throws at startup, failing the application fast.
    /// When false, the failure is logged at Error level and the application continues to start
    /// (requests still fail closed with 401 until the authority is fixed).
    /// </summary>
    public bool FailFast { get; set; }
    /// <summary>
    /// When true (default), the guard also runs deterministic, network-free configuration checks
    /// at startup: the authority/metadata address must be an absolute HTTPS Uri when
    /// <c>RequireHttpsMetadata</c> is set, audience/issuer validation must have a matching
    /// configured value, the algorithm list must not contain <c>none</c>, and the clock skew
    /// must not be negative. Violations are logged at Error level and, when <see cref="FailFast"/>
    /// is set, throw at startup.
    /// </summary>
    public bool EnforceStaticConfigurationChecks { get; set; } = true;

}
