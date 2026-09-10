using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace SafeWebCore.JwtBearer;

/// <summary>
/// Wraps a <see cref="BaseConfigurationManager"/> so OpenID Connect metadata retrieval failures
/// surface loudly at Error (permanent HTTP 4xx) or Warning (transient) level, instead of the
/// framework default of logging them at Information level where the default log filter hides them.
/// All reads and writes are forwarded to the wrapped manager, preserving its caching and
/// last-known-good semantics. Failures are rate-limited to one log line per refresh interval.
/// </summary>
internal sealed partial class LoggingConfigurationManager : BaseConfigurationManager, IConfigurationManager<OpenIdConnectConfiguration>
{
    private readonly BaseConfigurationManager _inner;
    private readonly ILogger _logger;
    private readonly object _sync = new();
    private DateTimeOffset _lastFailureLogged = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new <see cref="LoggingConfigurationManager"/> that decorates <paramref name="inner"/>.
    /// </summary>
    /// <param name="inner">The configuration manager to decorate.</param>
    /// <param name="logger">The logger used to report metadata retrieval failures.</param>
    public LoggingConfigurationManager(BaseConfigurationManager inner, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _logger = logger;

        // BaseConfigurationManager exposes these as non-virtual properties, so mirror the inner
        // values on this instance to keep reads through base-typed references correct.
        MetadataAddress = inner.MetadataAddress;
        AutomaticRefreshInterval = inner.AutomaticRefreshInterval;
        RefreshInterval = inner.RefreshInterval;
        UseLastKnownGoodConfiguration = inner.UseLastKnownGoodConfiguration;
        LastKnownGoodLifetime = inner.LastKnownGoodLifetime;
        if (inner.LastKnownGoodConfiguration is { } lastKnownGood)
        {
            LastKnownGoodConfiguration = lastKnownGood;
        }
    }

    /// <inheritdoc />
    public override Task<BaseConfiguration> GetBaseConfigurationAsync(CancellationToken cancel)
        => GetBaseConfigurationCoreAsync(cancel);

    private async Task<BaseConfiguration> GetBaseConfigurationCoreAsync(CancellationToken cancel)
    {
        try
        {
            return await _inner.GetBaseConfigurationAsync(cancel).ConfigureAwait(false);
        }
        catch (Exception ex) when (ShouldLogFailure())
        {
            var address = MetadataAddress ?? _inner.MetadataAddress ?? "(unknown)";
            if (JwtAuthorityValidationGuard.IsPermanent(ex))
            {
                LogPermanentFailure(_logger, address, ex);
            }
            else
            {
                LogTransientFailure(_logger, address, ex);
            }

            throw;
        }
    }

    /// <inheritdoc />
    public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancellationToken)
        => GetConfigurationCoreAsync(cancellationToken);

    /// <inheritdoc />
    public Task<OpenIdConnectConfiguration> GetConfigurationAsync()
        => GetConfigurationAsync(CancellationToken.None);

    private async Task<OpenIdConnectConfiguration> GetConfigurationCoreAsync(CancellationToken cancellationToken)
    {
        BaseConfiguration configuration = await GetBaseConfigurationCoreAsync(cancellationToken).ConfigureAwait(false);
        return (OpenIdConnectConfiguration)configuration;
    }

    /// <inheritdoc />
    public override void RequestRefresh() => _inner.RequestRefresh();

    private bool ShouldLogFailure()
    {
        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;
            if (now - _lastFailureLogged < _inner.RefreshInterval)
            {
                return false;
            }

            _lastFailureLogged = now;
            return true;
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "OpenID Connect metadata for '{MetadataAddress}' could not be retrieved (permanent configuration error, HTTP 4xx). Requests fail closed until this is fixed.")]
    static partial void LogPermanentFailure(ILogger logger, string metadataAddress, Exception exception);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "OpenID Connect metadata for '{MetadataAddress}' could not be retrieved (transient failure). Requests keep failing closed until the metadata endpoint is reachable again.")]
    static partial void LogTransientFailure(ILogger logger, string metadataAddress, Exception exception);
}
