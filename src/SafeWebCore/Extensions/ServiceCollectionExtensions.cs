using Microsoft.Extensions.DependencyInjection;
using SafeWebCore.Infrastructure;
using SafeWebCore.Middleware;
using SafeWebCore.Options;
using SafeWebCore.Presets;

namespace SafeWebCore.Extensions;

/// <summary>
/// Extension methods for configuring NetSecureHeaders services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds NetSecureHeaders services with the <b>Strict A+</b> preset.
    /// This is the fastest way to achieve an A+ rating on securityheaders.com.
    /// <para>
    /// Optionally pass a <paramref name="customize"/> action to relax specific settings.
    /// CSP directives accept multiple origins separated by spaces, e.g.
    /// <c>"'self' https://cdn1.example.com https://cdn2.example.com"</c>.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="customize">Optional action to override individual preset values.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Strict A+ with no changes:
    /// builder.Services.AddNetSecureHeadersStrictAPlus();
    ///
    /// // Allow images from multiple CDNs:
    /// builder.Services.AddNetSecureHeadersStrictAPlus(opts =&gt;
    ///     opts.Csp = opts.Csp with { ImgSrc = "'self' https://cdn1.example.com https://cdn2.example.com" });
    ///
    /// // Relax multiple directives at once:
    /// builder.Services.AddNetSecureHeadersStrictAPlus(opts =&gt;
    ///     opts.Csp = opts.Csp with
    ///     {
    ///         ImgSrc = "'self' https://img.cdn.com https://avatars.cdn.com data:",
    ///         ConnectSrc = "'self' https://api.example.com wss://ws.example.com",
    ///         FontSrc = "'self' https://fonts.gstatic.com https://cdn.example.com"
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddNetSecureHeadersStrictAPlus(
        this IServiceCollection services,
        Action<NetSecureHeadersOptions>? customize = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddNetSecureHeaders(services, opts =>
        {
            opts.ApplyPreset(SecurePresets.StrictAPlus());

            // Allow the caller to override specific values
            customize?.Invoke(opts);
        });
    }

    /// <summary>
    /// Adds NetSecureHeaders services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An action to configure the options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetSecureHeaders(
        this IServiceCollection services,
        Action<NetSecureHeadersOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        services.AddSingleton<INonceService, NonceService>();
        services.AddTransient<NetSecureHeadersMiddleware>();
        services.AddTransient<CspReportMiddleware>();
        services.AddHttpContextAccessor();

        return services;
    }
}
