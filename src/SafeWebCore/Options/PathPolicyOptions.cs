namespace SafeWebCore.Options;

/// <summary>
/// Represents a path-specific security policy that overrides global options
/// when the current request path starts with <see cref="PathPrefix"/>.
/// </summary>
public sealed class PathPolicyOptions
{
    /// <summary>
    /// Request path prefix that activates this policy (for example <c>/api</c> or <c>/admin</c>).
    /// </summary>
    public required string PathPrefix { get; init; }

    /// <summary>
    /// Security header options applied to matching paths.
    /// </summary>
    public required NetSecureHeadersOptions Options { get; init; }
}
