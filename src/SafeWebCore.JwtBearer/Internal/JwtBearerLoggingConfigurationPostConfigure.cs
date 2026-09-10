using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SafeWebCore.JwtBearer;

/// <summary>
/// Runs after the framework's <c>JwtBearerPostConfigureOptions</c> and wraps the already-created
/// configuration manager with <see cref="LoggingConfigurationManager"/> so metadata retrieval
/// failures surface loudly at runtime. Requires the hardening extension to be registered after
/// <c>AddJwtBearer</c>.
/// </summary>
internal sealed class JwtBearerLoggingConfigurationPostConfigure : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly JwtBearerHardeningOptions _hardening;
    private readonly ILogger<JwtBearerLoggingConfigurationPostConfigure> _logger;

    /// <summary>
    /// Initializes a new <see cref="JwtBearerLoggingConfigurationPostConfigure"/>.
    /// </summary>
    /// <param name="hardening">The hardening options controlling the runtime metadata logging.</param>
    /// <param name="logger">The logger used by the wrapping decorator.</param>
    public JwtBearerLoggingConfigurationPostConfigure(
        IOptions<JwtBearerHardeningOptions> hardening,
        ILogger<JwtBearerLoggingConfigurationPostConfigure> logger)
    {
        _hardening = hardening.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (!_hardening.EnableRuntimeMetadataLogging)
        {
            return;
        }

        if (options.ConfigurationManager is BaseConfigurationManager manager && manager is not LoggingConfigurationManager)
        {
            options.ConfigurationManager = new LoggingConfigurationManager(manager, _logger);
        }
    }
}
