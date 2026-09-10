using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using SafeWebCore.JwtBearer;

namespace SafeWebCore.JwtBearer.Tests;

public sealed class JwtAuthorityValidationGuardTests
{
    [Fact]
    public void IsPermanentDetectsHttp4xxOnInnerException()
    {
        var inner = new IOException("IDX20807: Unable to retrieve document from: 'metadata'.");
        inner.Data[HttpDocumentRetriever.StatusCode] = HttpStatusCode.BadRequest;
        var outer = new InvalidOperationException("IDX20803: Unable to obtain configuration.", inner);

        Assert.True(JwtAuthorityValidationGuard.IsPermanent(outer));
    }

    [Fact]
    public void IsPermanentIgnoresTransientStatusCodes()
    {
        var inner = new IOException("IDX20807: Unable to retrieve document from: 'metadata'.");
        inner.Data[HttpDocumentRetriever.StatusCode] = HttpStatusCode.ServiceUnavailable;
        var outer = new InvalidOperationException("IDX20803: Unable to obtain configuration.", inner);

        Assert.False(JwtAuthorityValidationGuard.IsPermanent(outer));
    }

    [Fact]
    public void IsPermanentReturnsFalseWithoutStatusCode()
    {
        var ex = new InvalidOperationException("IDX10803: Unable to obtain configuration.", new IOException("network down"));

        Assert.False(JwtAuthorityValidationGuard.IsPermanent(ex));
    }

    [Fact]
    public async Task StartAsyncPermanentFailureWithFailFastThrows()
    {
        var guard = CreateGuard(PermanentFailureManager(), failFast: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => guard.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsyncPermanentFailureWithoutFailFastCompletes()
    {
        var guard = CreateGuard(PermanentFailureManager(), failFast: false);

        await guard.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsyncSuccessCompletes()
    {
        var guard = CreateGuard(SuccessfulManager(), failFast: true);

        await guard.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsyncEmptySigningKeysWithFailFastThrows()
    {
        var guard = CreateGuard(EmptySigningKeysManager(), failFast: true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => guard.StartAsync(CancellationToken.None));

        Assert.Contains("no signing keys", exception.Message);
    }

    [Fact]
    public async Task StartAsyncEmptySigningKeysWithoutFailFastLogsError()
    {
        var records = new List<(LogLevel Level, string Message)>();
        var guard = CreateGuard(EmptySigningKeysManager(), failFast: false, records: records);

        await guard.StartAsync(CancellationToken.None);

        var record = Assert.Single(records, record => record.Level == LogLevel.Error);
        Assert.Contains("no signing keys", record.Message);
    }

    [Fact]
    public async Task StartAsyncEmptySigningKeysAllowedByRequireSigningKeysFalseWarns()
    {
        var records = new List<(LogLevel Level, string Message)>();
        var guard = CreateGuard(EmptySigningKeysManager(), failFast: true, records: records, requireSigningKeys: false);

        await guard.StartAsync(CancellationToken.None);

        var record = Assert.Single(records, record => record.Level == LogLevel.Warning);
        Assert.Contains("no signing keys", record.Message);
    }

    [Fact]
    public async Task PeriodicValidationRunsRepeatedly()
    {
        var manager = CountingSuccessfulManager();
        var guard = CreateGuard(manager, failFast: true, periodicInterval: TimeSpan.FromMilliseconds(120));

        await guard.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(400), TestContext.Current.CancellationToken);
        await guard.StopAsync(TestContext.Current.CancellationToken);

        Assert.True(manager.GetCallCount() >= 2, "The authority should have been re-validated at least once.");
    }

    [Fact]
    public async Task PeriodicValidationFailureDoesNotCrash()
    {
        var manager = CountingTransientFailureManager();
        var guard = CreateGuard(manager, failFast: false, periodicInterval: TimeSpan.FromMilliseconds(100));

        await guard.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken);
        await guard.StopAsync(TestContext.Current.CancellationToken);

        Assert.True(manager.GetCallCount() >= 2, "The periodic re-validation should have attempted another fetch.");
    }

    private static JwtAuthorityValidationGuard CreateGuard(
        IConfigurationManager<OpenIdConnectConfiguration> manager,
        bool failFast,
        List<(LogLevel Level, string Message)>? records = null,
        bool requireSigningKeys = true,
        TimeSpan? periodicInterval = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (records is not null)
        {
            services.AddSingleton<ILogger<JwtAuthorityValidationGuard>>(
                new RecordingLogger<JwtAuthorityValidationGuard>(records));
        }

        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
        {
            o.ConfigurationManager = manager;
            o.TokenValidationParameters.ValidateAudience = false;
            o.TokenValidationParameters.ValidateIssuer = false;
        });

        services.Configure<JwtAuthorityValidationOptions>(o =>
        {
            o.FailFast = failFast;
            o.RequireSigningKeys = requireSigningKeys;
            o.PeriodicValidationInterval = periodicInterval;
        });

        services.AddHostedService<JwtAuthorityValidationGuard>();

        using var provider = services.BuildServiceProvider();
        return provider.GetServices<IHostedService>().OfType<JwtAuthorityValidationGuard>().Single();
    }

    private static FakeManager SuccessfulManager()
        => new(() => Task.FromResult(new OpenIdConnectConfiguration
        {
            SigningKeys = { new SymmetricSecurityKey(new byte[32]) },
        }));

    private static FakeManager EmptySigningKeysManager()
        => new(() => Task.FromResult(new OpenIdConnectConfiguration()));

    private static FakeManager PermanentFailureManager()
    {
        var inner = new IOException("IDX20807: Unable to retrieve document from: 'metadata'.");
        inner.Data[HttpDocumentRetriever.StatusCode] = HttpStatusCode.BadRequest;
        var outer = new InvalidOperationException("IDX20803: Unable to obtain configuration.", inner);
        return new FakeManager(() => Task.FromException<OpenIdConnectConfiguration>(outer));
    }

    private static CountingManager CountingSuccessfulManager()
        => new(() => Task.FromResult(new OpenIdConnectConfiguration
        {
            SigningKeys = { new SymmetricSecurityKey(new byte[32]) },
        }));

    private static CountingManager CountingTransientFailureManager()
        => new(() => Task.FromException<OpenIdConnectConfiguration>(new TimeoutException("IDX20807: timed out")));

    private sealed class FakeManager(Func<Task<OpenIdConnectConfiguration>> get) : IConfigurationManager<OpenIdConnectConfiguration>
    {
        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel) => get();

        public void RequestRefresh()
        {
        }
    }

    private sealed class CountingManager(Func<Task<OpenIdConnectConfiguration>> get) : IConfigurationManager<OpenIdConnectConfiguration>
    {
        private int _calls;

        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
        {
            Interlocked.Increment(ref _calls);
            return get();
        }

        public int GetCallCount() => Volatile.Read(ref _calls);

        public void RequestRefresh()
        {
        }
    }

    private sealed class RecordingLogger<T>(List<(LogLevel Level, string Message)> records) : ILogger<T>
        where T : class
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            records.Add((logLevel, formatter(state, exception)));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}