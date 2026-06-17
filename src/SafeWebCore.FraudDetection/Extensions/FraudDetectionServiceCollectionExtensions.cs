using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Detection;
using SafeWebCore.FraudDetection.Infrastructure;
using SafeWebCore.FraudDetection.Options;

namespace SafeWebCore.FraudDetection.Extensions;

/// <summary>
/// Extension methods for registering SafeWebCore fraud-detection services.
/// </summary>
public static class FraudDetectionServiceCollectionExtensions
{
    /// <summary>
    /// Registers fraud detection using <see cref="FraudDetectionOptions"/> defaults.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSafeWebCoreFraudDetection(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<FraudDetectionOptions>().ValidateOnStart();
        return AddFraudDetectionCore(services);
    }

    /// <summary>
    /// Registers fraud detection with application configuration binding.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="sectionName">Configuration section path.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSafeWebCoreFraudDetection(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = FraudDetectionOptions.DefaultSectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services
            .AddOptions<FraudDetectionOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateOnStart();

        return AddFraudDetectionCore(services);
    }

    /// <summary>
    /// Registers fraud detection and configures <see cref="FraudDetectionOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration delegate.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSafeWebCoreFraudDetection(
        this IServiceCollection services,
        Action<FraudDetectionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services
            .AddOptions<FraudDetectionOptions>()
            .Configure(configure)
            .ValidateOnStart();

        return AddFraudDetectionCore(services);
    }

    /// <summary>
    /// Backward-compatible registration overload for legacy Western-only options.
    /// 
    /// For new projects or multi-region scenarios, prefer configuring
    /// <see cref="FraudDetectionOptions.GeoCulturalConsistency"/> and
    /// <see cref="FraudDetectionOptions.EnableGeoCulturalConsistency"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional Western detector customization.</param>
    /// <returns>The service collection for chaining.</returns>
    [Obsolete("Use AddSafeWebCoreFraudDetection(Action<FraudDetectionOptions>) and set EnableGeoCulturalConsistency + GeoCulturalConsistency for new scenarios. This overload remains fully supported.")]
    public static IServiceCollection AddSafeWebCoreFraudDetection(
        this IServiceCollection services,
        Action<WesternDetectorOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddOptions<FraudDetectionOptions>()
            .Configure(options =>
            {
                options.EnableWesternImpersonation = true;
                options.EnablePenTestDetection = false;
                configure?.Invoke(options.WesternImpersonation);
            })
            .ValidateOnStart();

        return AddFraudDetectionCore(services);
    }

    /// <summary>
    /// Backward-compatible named registration overload for legacy Western-only options.
    /// 
    /// For new multi-region scenarios, configure via <see cref="FraudDetectionOptions"/>
    /// and <see cref="GeoCulturalConsistencyOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">Options name.</param>
    /// <param name="configure">Configuration delegate.</param>
    /// <returns>The service collection for chaining.</returns>
    #pragma warning disable CS0618
    public static IServiceCollection AddSafeWebCoreFraudDetection(
            this IServiceCollection services,
            string name,
            Action<WesternDetectorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<WesternDetectorOptions>(name).Configure(configure);
        return services;
    }
    #pragma warning restore CS0618

    /// <summary>
    /// Registers a pen-test authorization notification consumer.
    /// </summary>
    /// <typeparam name="TConsumer">Consumer implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPenTestAuthorizationNotificationConsumer<TConsumer>(this IServiceCollection services)
        where TConsumer : class, IPenTestAuthorizationNotificationConsumer
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPenTestAuthorizationNotificationConsumer, TConsumer>());
        return services;
    }

    /// <summary>
    /// Registers an injectable mail client and wires it as an authorization notification consumer.
    /// </summary>
    /// <typeparam name="TMailClient">Mail client implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPenTestAuthorizationNotificationMailClient<TMailClient>(this IServiceCollection services)
        where TMailClient : class, IPenTestAuthorizationNotificationMailClient
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IPenTestAuthorizationNotificationMailClient, TMailClient>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPenTestAuthorizationNotificationConsumer, MailClientPenTestAuthorizationNotificationConsumer>());
        return services;
    }

    private static IServiceCollection AddFraudDetectionCore(IServiceCollection services)
    {
        services.TryAddSingleton<IPenTestAuthorizationNotificationSender, DispatchingPenTestAuthorizationNotificationSender>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPenTestAuthorizationNotificationConsumer, LoggingPenTestAuthorizationNotificationSender>());
        services.TryAddSingleton<IFraudDetectionOptionsResolver, FraudDetectionOptionsResolver>();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<FraudDetectionOptions>, FraudDetectionOptionsValidator>());

        // Register the appropriate detector implementation based on configuration.
        // Neutral multi-region detector is preferred when EnableGeoCulturalConsistency is configured.
        // Legacy Western detector remains available for full backward compatibility.
        services.TryAddSingleton<IFraudDetector>(sp =>
        {
            var resolver = sp.GetRequiredService<IFraudDetectionOptionsResolver>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GeoCulturalConsistencyDetector>>();
            var sender = sp.GetRequiredService<IPenTestAuthorizationNotificationSender>();
            var geoIp = sp.GetService<IGeoIpService>();
            var timeProvider = sp.GetRequiredService<TimeProvider>();

            // Peek at the options to decide which detector implementation to use.
            // This keeps the decision at startup without per-request cost.
            var currentOptions = resolver.GetCurrent(null);

            if (currentOptions.EnableGeoCulturalConsistency)
            {
                return new GeoCulturalConsistencyDetector(
                    resolver,
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GeoCulturalConsistencyDetector>>(),
                    sender,
                    geoIp,
                    timeProvider);
            }

            // Legacy path (Western-centric naming) for backward compatibility.
#pragma warning disable CS0618
            return new WesternImpersonationDetector(
                resolver,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<WesternImpersonationDetector>>(),
                sender,
                geoIp,
                timeProvider);
#pragma warning restore CS0618
        });

        return services;
    }
}
