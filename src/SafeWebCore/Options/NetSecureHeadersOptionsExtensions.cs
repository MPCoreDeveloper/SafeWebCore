namespace SafeWebCore.Options;

/// <summary>
/// Extension methods for <see cref="NetSecureHeadersOptions"/>.
/// </summary>
public static class NetSecureHeadersOptionsExtensions
{
    /// <summary>
    /// Adds a path-specific security policy that <b>inherits</b> all values from the
    /// global configuration and only overrides the settings explicitly configured
    /// in the <paramref name="customize"/> action.
    /// </summary>
    /// <param name="options">The global options instance.</param>
    /// <param name="pathPrefix">Request path prefix that activates this policy (for example <c>/api</c> or <c>/admin</c>).</param>
    /// <param name="customize">Action that overrides specific security settings for the matching path.</param>
    /// <returns>The global options instance for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This is the recommended way to configure path policies because it prevents
    /// accidental security header downgrades. Unspecified values inherit from the
    /// global options (for example HSTS, X-Frame-Options, CSP, etc.) instead of
    /// falling back to library defaults.
    /// </para>
    /// <para>
    /// Example — only override X-Frame-Options for <c>/api</c> while inheriting
    /// the global HSTS, CSP, and all other security headers:
    /// </para>
    /// <code>
    /// builder.Services.AddNetSecureHeaders(opts =>
    /// {
    ///     opts.HstsValue = "max-age=63072000; includeSubDomains; preload";
    ///
    ///     opts.PathPolicy("/api", api =>
    ///     {
    ///         api.XFrameOptionsValue = "SAMEORIGIN";
    ///     });
    /// });
    /// </code>
    /// </remarks>
    public static NetSecureHeadersOptions PathPolicy(
        this NetSecureHeadersOptions options,
        string pathPrefix,
        Action<NetSecureHeadersOptions> customize)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(pathPrefix);
        ArgumentNullException.ThrowIfNull(customize);

        // Start from a clone of the global configuration so path policies inherit
        // every setting that is not explicitly overridden below.
        var pathOptions = options.Clone();
        customize(pathOptions);

        options.PathPolicies.Add(new PathPolicyOptions
        {
            PathPrefix = pathPrefix,
            Options = pathOptions
        });

        return options;
    }
}