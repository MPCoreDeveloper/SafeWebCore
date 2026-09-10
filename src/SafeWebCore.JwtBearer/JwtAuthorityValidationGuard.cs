using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace SafeWebCore.JwtBearer;

/// <summary>
/// Startup guard that eagerly loads the OpenID Connect metadata for the JWT bearer authority,
/// so a misconfigured or unreachable authority is detected at startup instead of surfacing as
/// silent 401s at runtime. Permanent errors (HTTP 4xx from the metadata endpoint, or a discovery
/// document without signing keys) are logged at Error level and can fail fast; transient errors
/// (5xx, timeout, DNS) only log a Warning and let the application start. Optionally re-validates
/// the authority on a schedule while the application runs and runs deterministic, network-free
/// configuration checks (see <see cref="JwtAuthorityValidationOptions.EnforceStaticConfigurationChecks"/>).
/// </summary>
public sealed partial class JwtAuthorityValidationGuard : IHostedService, IDisposable
{
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly JwtBearerOptions _jwtOptions;
    private readonly ILogger<JwtAuthorityValidationGuard> _logger;
    private readonly string _scheme;
    private readonly bool _failFast;
    private readonly bool _enforceStaticConfigurationChecks;
    private readonly bool _requireSigningKeys;
    private readonly TimeSpan? _periodicInterval;
    private CancellationTokenSource? _periodicCancellation;
    private Task? _periodicTask;

    /// <summary>
    /// Initializes a new <see cref="JwtAuthorityValidationGuard"/>.
    /// </summary>
    /// <param name="jwtOptions">Monitor for the JWT bearer options of the validated scheme.</param>
    /// <param name="options">Guard options.</param>
    /// <param name="logger">Logger used to report validation results.</param>
    /// <param name="hardeningOptions">Optional hardening rules used by the static configuration checks.</param>
    /// <exception cref="InvalidOperationException">Thrown when the scheme has no OpenID Connect configuration manager.</exception>
    public JwtAuthorityValidationGuard(
        IOptionsMonitor<JwtBearerOptions> jwtOptions,
        IOptions<JwtAuthorityValidationOptions> options,
        ILogger<JwtAuthorityValidationGuard> logger,
        IOptions<JwtBearerHardeningOptions>? hardeningOptions = null)
    {
        ArgumentNullException.ThrowIfNull(jwtOptions);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _scheme = options.Value.Scheme ?? JwtBearerDefaults.AuthenticationScheme;
        _jwtOptions = jwtOptions.Get(_scheme);
        _configurationManager = _jwtOptions.ConfigurationManager
            ?? throw new InvalidOperationException(
                $"The JWT bearer scheme '{_scheme}' has no ConfigurationManager. Configure Authority or MetadataAddress first.");
        _failFast = options.Value.FailFast;
        _enforceStaticConfigurationChecks = options.Value.EnforceStaticConfigurationChecks;
        _requireSigningKeys = options.Value.RequireSigningKeys;
        _periodicInterval = options.Value.PeriodicValidationInterval;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ValidateAuthorityAsync(_failFast, cancellationToken);

        if (_periodicInterval is { } interval && interval > TimeSpan.Zero)
        {
            _periodicCancellation = new CancellationTokenSource();
            _periodicTask = RunPeriodicValidationAsync(interval, _periodicCancellation.Token);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_periodicCancellation is null)
        {
            return;
        }

        await _periodicCancellation.CancelAsync();
        if (_periodicTask is not null)
        {
            try
            {
                await _periodicTask.WaitAsync(CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }

        _periodicCancellation.Dispose();
        _periodicCancellation = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _periodicCancellation?.Dispose();
        _periodicCancellation = null;
    }

    private async Task ValidateAuthorityAsync(bool failFast, CancellationToken cancellationToken)
    {
        if (_enforceStaticConfigurationChecks)
        {
            RunStaticConfigurationChecks(cancellationToken);
        }

        OpenIdConnectConfiguration configuration;
        try
        {
            configuration = await _configurationManager.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ReportMetadataFailure(ex, failFast);
            return;
        }

        ReportConfigurationResult(configuration, failFast);
    }

    private void ReportConfigurationResult(OpenIdConnectConfiguration configuration, bool failFast)
    {
        if (configuration.SigningKeys.Count == 0)
        {
            if (_requireSigningKeys)
            {
                LogNoSigningKeys(_logger, _scheme, AuthorityAddress); // NOSONAR: implementation generated by the LoggerMessage source generator
                if (failFast)
                {
                    throw new InvalidOperationException(
                        $"The OpenID Connect metadata for scheme '{_scheme}' from '{AuthorityAddress}' contains no signing keys; every token will fail validation. Use RequireSigningKeys = false to allow this.");
                }
            }
            else
            {
                LogNoSigningKeysWarning(_logger, _scheme, AuthorityAddress); // NOSONAR: implementation generated by the LoggerMessage source generator
            }

            return;
        }

        LogMetadataLoaded(_logger, _scheme, AuthorityAddress); // NOSONAR: implementation generated by the LoggerMessage source generator
    }

    private void ReportMetadataFailure(Exception ex, bool failFast)
    {
        if (IsPermanent(ex))
        {
            LogPermanentFailure(_logger, _scheme, AuthorityAddress, ex); // NOSONAR: implementation generated by the LoggerMessage source generator
            if (failFast)
            {
                throw new InvalidOperationException(
                    $"The JWT authority for scheme '{_scheme}' is misconfigured (HTTP 4xx from '{AuthorityAddress}'). Fix the Authority/MetadataAddress before starting.",
                    ex);
            }
        }
        else
        {
            LogTransientFailure(_logger, _scheme, AuthorityAddress, ex); // NOSONAR: implementation generated by the LoggerMessage source generator
        }
    }

    private void RunStaticConfigurationChecks(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var issues = JwtBearerConfigurationValidator.FindIssues(_jwtOptions);
        if (issues.Count == 0)
        {
            return;
        }

        var message = string.Join("; ", issues);
        LogConfigurationInvalid(_logger, _scheme, message); // NOSONAR: implementation generated by the LoggerMessage source generator
        if (_failFast)
        {
            throw new InvalidOperationException($"The JWT bearer configuration for scheme '{_scheme}' is invalid: {message}");
        }
    }

    private async Task RunPeriodicValidationAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    await ValidateAuthorityAsync(failFast: false, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    LogPeriodicValidationFailed(_logger, _scheme, ex); // NOSONAR: implementation generated by the LoggerMessage source generator
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private string AuthorityAddress => _jwtOptions.Authority ?? _jwtOptions.MetadataAddress ?? "(no authority configured)";

    /// <summary>
    /// Determines whether an authority metadata failure is permanent (HTTP 4xx) by walking the
    /// exception chain for the HTTP status recorded by the IdentityModel document retriever
    /// (<c>HttpDocumentRetriever.StatusCode</c>, value an <see cref="HttpStatusCode"/>).
    /// </summary>
    internal static bool IsPermanent(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            if (current.Data[HttpDocumentRetriever.StatusCode] is HttpStatusCode status
                && (int)status is >= 400 and < 500)
            {
                return true;
            }
        }

        return false;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "JWT authority metadata loaded successfully for scheme '{Scheme}' from '{MetadataAddress}'.")]
    static partial void LogMetadataLoaded(ILogger logger, string scheme, string metadataAddress); // NOSONAR: implementation generated by the LoggerMessage source generator

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "JWT authority metadata could not be loaded for scheme '{Scheme}' from '{MetadataAddress}' (permanent configuration error, HTTP 4xx).")]
    static partial void LogPermanentFailure(ILogger logger, string scheme, string metadataAddress, Exception exception); // NOSONAR: implementation generated by the LoggerMessage source generator

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "JWT authority metadata could not be loaded for scheme '{Scheme}' from '{MetadataAddress}' (transient or unclassified). The app will start; requests fail closed until the IdP is reachable.")]
    static partial void LogTransientFailure(ILogger logger, string scheme, string metadataAddress, Exception exception); // NOSONAR: implementation generated by the LoggerMessage source generator

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "The JWT bearer configuration for scheme '{Scheme}' is invalid: {ValidationMessage}")]
    static partial void LogConfigurationInvalid(ILogger logger, string scheme, string validationMessage); // NOSONAR: implementation generated by the LoggerMessage source generator

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "The OpenID Connect metadata for scheme '{Scheme}' from '{MetadataAddress}' contains no signing keys; every token will fail validation.")]
    static partial void LogNoSigningKeys(ILogger logger, string scheme, string metadataAddress); // NOSONAR: implementation generated by the LoggerMessage source generator

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "The OpenID Connect metadata for scheme '{Scheme}' from '{MetadataAddress}' contains no signing keys.")]
    static partial void LogNoSigningKeysWarning(ILogger logger, string scheme, string metadataAddress); // NOSONAR: implementation generated by the LoggerMessage source generator

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Periodic JWT authority validation failed for scheme '{Scheme}'.")]
    static partial void LogPeriodicValidationFailed(ILogger logger, string scheme, Exception exception); // NOSONAR: implementation generated by the LoggerMessage source generator
}