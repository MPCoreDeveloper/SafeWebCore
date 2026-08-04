using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SafeWebCore.Abstractions;
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

        services
            .AddOptions<NetSecureHeadersOptions>()
            .Configure(configure)
            .ValidateOnStart();

        return AddNetSecureHeadersCore(services);
    }

    /// <summary>
    /// Adds NetSecureHeaders services by binding <see cref="NetSecureHeadersOptions"/> from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionName">The configuration section name. Default: <c>NetSecureHeaders</c>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddNetSecureHeadersFromConfiguration(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddNetSecureHeadersFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "NetSecureHeaders")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        return AddNetSecureHeadersFromConfiguration(services, configuration.GetSection(sectionName));
    }

    /// <summary>
    /// Adds NetSecureHeaders services by binding <see cref="NetSecureHeadersOptions"/> from the specified configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="section">The configuration section to bind.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetSecureHeadersFromConfiguration(
        this IServiceCollection services,
        IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(section);

        services
            .AddOptions<NetSecureHeadersOptions>()
            .Bind(section)
            .ValidateOnStart();

        return AddNetSecureHeadersCore(services);
    }

    /// <summary>
    /// Adds NetSecureHeaders services with environment-aware rollout defaults.
    /// In non-production environments, CSP defaults to report-only mode unless the caller overrides it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="environment">The current host environment.</param>
    /// <param name="configure">Optional action to customize the options after environment defaults are applied.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetSecureHeadersForEnvironment(
        this IServiceCollection services,
        IHostEnvironment environment,
        Action<NetSecureHeadersOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);

        return AddNetSecureHeaders(services, opts =>
        {
            ApplyEnvironmentRolloutDefaults(opts, environment);
            configure?.Invoke(opts);
        });
    }

    /// <summary>
    /// Adds NetSecureHeaders services with the <b>Strict A+</b> preset and environment-aware rollout defaults.
    /// In non-production environments, CSP defaults to report-only mode unless the caller overrides it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="environment">The current host environment.</param>
    /// <param name="customize">Optional action to override individual preset values after environment defaults are applied.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetSecureHeadersStrictAPlusForEnvironment(
        this IServiceCollection services,
        IHostEnvironment environment,
        Action<NetSecureHeadersOptions>? customize = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);

        return AddNetSecureHeaders(services, opts =>
        {
            opts.ApplyPreset(SecurePresets.StrictAPlus());
            ApplyEnvironmentRolloutDefaults(opts, environment);
            customize?.Invoke(opts);
        });
    }

    /// <summary>
    /// Adds NetSecureHeaders services with the API preset.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="customize">Optional action to override individual preset values.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetSecureHeadersApiPreset(
        this IServiceCollection services,
        Action<NetSecureHeadersOptions>? customize = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddNetSecureHeaders(services, opts =>
        {
            opts.ApplyPreset(SecurePresets.Api());
            customize?.Invoke(opts);
        });
    }

    /// <summary>
    /// Adds NetSecureHeaders services with the MVC preset.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="customize">Optional action to override individual preset values.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetSecureHeadersMvcPreset(
        this IServiceCollection services,
        Action<NetSecureHeadersOptions>? customize = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddNetSecureHeaders(services, opts =>
        {
            opts.ApplyPreset(SecurePresets.Mvc());
            customize?.Invoke(opts);
        });
    }

    /// <summary>
    /// Adds NetSecureHeaders services with the Blazor preset.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="customize">Optional action to override individual preset values.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetSecureHeadersBlazorPreset(
        this IServiceCollection services,
        Action<NetSecureHeadersOptions>? customize = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddNetSecureHeaders(services, opts =>
        {
            opts.ApplyPreset(SecurePresets.Blazor());
            customize?.Invoke(opts);
        });
    }

    /// <summary>
    /// Adds NetSecureHeaders services with a Blazor preset optimized for heavy WebSocket / SignalR usage.
    /// </summary>
    public static IServiceCollection AddNetSecureHeadersBlazorWebSocketPreset(
        this IServiceCollection services,
        Action<NetSecureHeadersOptions>? customize = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddNetSecureHeaders(services, opts =>
        {
            opts.ApplyPreset(SecurePresets.BlazorWebSocket());
            customize?.Invoke(opts);
        });
    }

    /// <summary>
    /// Adds NetSecureHeaders services with the SPA reverse-proxy preset.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="customize">Optional action to override individual preset values.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddNetSecureHeadersSpaReverseProxyPreset(
        this IServiceCollection services,
        Action<NetSecureHeadersOptions>? customize = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddNetSecureHeaders(services, opts =>
        {
            opts.ApplyPreset(SecurePresets.SpaReverseProxy());
            customize?.Invoke(opts);
        });
    }

    /// <summary>
    /// Adds NetSecureHeaders services with a preset suitable for Swagger / OpenAPI UI.
    /// </summary>
    public static IServiceCollection AddNetSecureHeadersSwagger(
        this IServiceCollection services,
        Action<NetSecureHeadersOptions>? customize = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddNetSecureHeaders(services, opts =>
        {
            opts.ApplyPreset(SecurePresets.Swagger());
            customize?.Invoke(opts);
        });
    }

    /// <summary>
    /// Adds NetSecureHeaders services with a preset aligned with the
    /// <b>OWASP API Security Top 10</b> recommended response-header hardening
    /// for API endpoints.
    /// <para>
    /// This preset keeps strong transport security, content-type sniffing
    /// protection, referrer control, and server identity hiding while disabling
    /// browser-document headers that are irrelevant for JSON responses.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="customize">Optional action to override individual preset values.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddNetSecureHeadersOwaspApiPreset(opts =>
    /// {
    ///     opts.ReferrerPolicyValue = "strict-origin-when-cross-origin";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddNetSecureHeadersOwaspApiPreset(
        this IServiceCollection services,
        Action<NetSecureHeadersOptions>? customize = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddNetSecureHeaders(services, opts =>
        {
            opts.ApplyPreset(SecurePresets.OwaspApi());
            customize?.Invoke(opts);
        });
    }

    /// <summary>
    /// Adds NetSecureHeaders services with a preset suitable for applications that
    /// expose the <b>NSwag</b> UI (Rico Sutter's NSwag / NSwagStudio).
    /// <para>
    /// NSwag UI is stricter than classic Swagger UI: assets load from the
    /// official <c>https://unpkg.com/nswag/</c> package and scripts/styles use
    /// nonce-based CSP with <c>'strict-dynamic'</c> — no <c>'unsafe-inline'</c>
    /// required.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="customize">Optional action to override individual preset values.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddNetSecureHeadersNSwagPreset(opts =>
    /// {
    ///     opts.ReferrerPolicyValue = "no-referrer";
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddNetSecureHeadersNSwagPreset(
        this IServiceCollection services,
        Action<NetSecureHeadersOptions>? customize = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddNetSecureHeaders(services, opts =>
        {
            opts.ApplyPreset(SecurePresets.NSwag());
            customize?.Invoke(opts);
        });
    }

    /// <summary>
    /// Adds NetSecureHeaders services with a preset suitable for applications behind a reverse proxy or YARP.
    /// </summary>
    public static IServiceCollection AddNetSecureHeadersReverseProxyPreset(
        this IServiceCollection services,
        Action<NetSecureHeadersOptions>? customize = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddNetSecureHeaders(services, opts =>
        {
            opts.ApplyPreset(SecurePresets.ReverseProxy());
            customize?.Invoke(opts);
        });
    }

    private static IServiceCollection AddNetSecureHeadersCore(IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<NetSecureHeadersOptions>, NetSecureHeadersOptionsValidator>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ICspReportSink, CspLoggingReportSink>());
        services.AddSingleton<INonceService, NonceService>();
        services.AddTransient<NetSecureHeadersMiddleware>();
        services.AddTransient<CspReportMiddleware>();
        services.AddSingleton<INetSecureHeadersDiagnosticsService, NetSecureHeadersDiagnosticsService>();
        services.AddSingleton<SecurityEventDispatcher>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISecurityEventSink, LoggingSecurityEventSink>());
        services.AddSingleton<SafeWebCoreMetrics>();
        services.AddHttpContextAccessor();

        return services;
    }

    /// <summary>
    /// Registers a custom <see cref="ISecurityEventSink"/> for SafeWebCore security events.
    /// This is additive and opt-in.
    /// </summary>
    public static IServiceCollection AddSafeWebCoreSecurityEventSink<T>(this IServiceCollection services)
        where T : class, ISecurityEventSink
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISecurityEventSink, T>());
        return services;
    }

    private static void ApplyEnvironmentRolloutDefaults(NetSecureHeadersOptions options, IHostEnvironment environment)
    {
        if (!environment.IsProduction() && options.EnableCsp)
        {
            options.UseCspReportOnly = true;
        }
    }
}
