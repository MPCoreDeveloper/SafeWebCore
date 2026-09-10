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
/// silent 401s at runtime. Permanent errors (HTTP 4xx from the metadata endpoint) are logged at
/// Error level and can fail fast; transient errors (5xx, timeout, DNS) only log a Warning and
/// let the application start. Optionally runs deterministic, network-free configuration checks
/// first (see <see cref="JwtAuthorityValidationOptions.EnforceStaticConfigurationChecks"/>).
/// </summary>
public sealed partial class JwtAuthorityValidationGuard : IHostedService
{
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly JwtBearerOptions _jwtOptions;
    private readonly string _scheme;
    private readonly bool _failFast;
    private readonly bool _enforceStaticConfigurationChecks;
    private readonly ILogger<JwtAuthorityValidationGuard> _logger;

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
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_enforceStaticConfigurationChecks)
        {
            RunStaticConfigurationChecks(cancellationToken);
        }

        try
        {
            await _configurationManager.GetConfigurationAsync(cancellationToken);
            LogMetadataLoaded(_logger, _scheme, AuthorityAddress);
        }
        catch (Exception ex)
        {
            if (IsPermanent(ex))
            {
                LogPermanentFailure(_logger, _scheme, AuthorityAddress, ex);
                if (_failFast)
                {
                    throw new InvalidOperationException(
                        $"The JWT authority for scheme '{_scheme}' is misconfigured (HTTP 4xx from '{AuthorityAddress}'). Fix the Authority/MetadataAddress before starting.",
                        ex);
                }
            }
            else
            {
                LogTransientFailure(_logger, _scheme, AuthorityAddress, ex);
            }
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void RunStaticConfigurationChecks(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var issues = JwtBearerConfigurationValidator.FindIssues(_jwtOptions);
        if (issues.Count == 0)
        {
            return;
        }

        var message = string.Join("; ", issues);
        LogConfigurationInvalid(_logger, _scheme, message);
        if (_failFast)
        {
            throw new InvalidOperationException($"The JWT bearer configuration for scheme '{_scheme}' is invalid: {message}");
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
    static partial void LogMetadataLoaded(ILogger logger, string scheme, string metadataAddress);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "JWT authority metadata could not be loaded for scheme '{Scheme}' from '{MetadataAddress}' (permanent configuration error, HTTP 4xx).")]
    static partial void LogPermanentFailure(ILogger logger, string scheme, string metadataAddress, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "JWT authority metadata could not be loaded for scheme '{Scheme}' from '{MetadataAddress}' (transient or unclassified). The app will start; requests fail closed until the IdP is reachable.")]
    static partial void LogTransientFailure(ILogger logger, string scheme, string metadataAddress, Exception exception);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "The JWT bearer configuration for scheme '{Scheme}' is invalid: {ValidationMessage}")]
    static partial void LogConfigurationInvalid(ILogger logger, string scheme, string validationMessage);
}