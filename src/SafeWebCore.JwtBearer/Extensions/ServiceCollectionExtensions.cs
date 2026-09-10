using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SafeWebCore.JwtBearer.Extensions;

/// <summary>
/// Extension methods for registering the JWT authority validation guard and optional hardening.
/// </summary>
public static class JwtAuthorityValidationExtensions
{
    /// <summary>
    /// Registers a startup <see cref="JwtAuthorityValidationGuard"/> that eagerly loads the OpenID
    /// Connect metadata for the JWT bearer authority and fails loud (or fails fast) when the authority
    /// is misconfigured, so a typo'd authority is never a silent availability incident.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration of <see cref="JwtAuthorityValidationOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddJwtBearerAuthorityValidation(
        this IServiceCollection services,
        Action<JwtAuthorityValidationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure(configure ?? (_ => { }));
        if (!services.Any(descriptor => descriptor.ImplementationType == typeof(JwtAuthorityValidationGuard)))
        {
            services.AddSingleton<JwtAuthorityValidationGuard>();
            services.AddHostedService(provider => provider.GetRequiredService<JwtAuthorityValidationGuard>());
        }

        return services;
    }

    /// <summary>
    /// Registers the optional JWT hardening rules plus the startup authority validation guard with a
    /// secure default (a broken authority fails the application fast). Call this after <c>AddJwtBearer</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="scheme">The JWT bearer scheme to harden (defaults to "Bearer").</param>
    /// <param name="configure">Optional configuration of <see cref="JwtBearerHardeningOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddJwtBearerHardening(
        this IServiceCollection services,
        string? scheme = null,
        Action<JwtBearerHardeningOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var targetScheme = scheme ?? JwtBearerDefaults.AuthenticationScheme;
        var hardening = new JwtBearerHardeningOptions();
        configure?.Invoke(hardening);

        services.AddSingleton<IOptions<JwtBearerHardeningOptions>>(Options.Create(hardening));
        services.Configure<JwtBearerOptions>(targetScheme, options => JwtBearerHardeningApplication.Apply(options, hardening));
        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerLoggingConfigurationPostConfigure>();

        // Secure default: a broken authority must stop the application. The last registered
        // JwtAuthorityValidationOptions always wins, so callers can opt out explicitly.
        services.AddJwtBearerAuthorityValidation(options => options.FailFast = true);

        return services;
    }

    /// <summary>
    /// One-line registration that adds <c>AddJwtBearer</c>, the optional hardening rules and the
    /// startup authority validation guard (fail fast by default) for the "Bearer" scheme.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureJwt">Configuration of <see cref="JwtBearerOptions"/> (authority, issuer, audience, ...).</param>
    /// <param name="configureHardening">Optional configuration of <see cref="JwtBearerHardeningOptions"/>.</param>
    /// <param name="configureGuard">Optional configuration of <see cref="JwtAuthorityValidationOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSafeWebCoreJwtBearer(
        this IServiceCollection services,
        Action<JwtBearerOptions> configureJwt,
        Action<JwtBearerHardeningOptions>? configureHardening = null,
        Action<JwtAuthorityValidationOptions>? configureGuard = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureJwt);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(configureJwt);
        services.AddJwtBearerHardening(null, configureHardening);
        services.Configure(configureGuard ?? (_ => { }));

        return services;
    }
}