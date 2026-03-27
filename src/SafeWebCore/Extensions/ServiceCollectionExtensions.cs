using Microsoft.Extensions.DependencyInjection;
using SafeWebCore.Infrastructure;
using SafeWebCore.Middleware;
using SafeWebCore.Options;

namespace SafeWebCore.Extensions;

/// <summary>
/// Extension methods for configuring NetSecureHeaders services.
/// </summary>
public static class ServiceCollectionExtensions
{
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
