using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using SafeWebCore.JwtBearer;

namespace SafeWebCore.JwtBearer.Tests;

public sealed class LoggingConfigurationManagerTests
{
    [Fact]
    public async Task GetBaseConfigurationAsyncLogsPermanentFailureAtErrorAndThrows()
    {
        var records = new List<(LogLevel Level, string Message)>();
        var inner = new FakeBaseManager(_ => Task.FromException<BaseConfiguration>(PermanentException()));
        var manager = new LoggingConfigurationManager(inner, new RecordingLogger(records));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.GetBaseConfigurationAsync(CancellationToken.None));

        Assert.Contains("IDX20803", ex.Message);
        var record = Assert.Single(records);
        Assert.Equal(LogLevel.Error, record.Level);
        Assert.Contains(inner.MetadataAddress, record.Message);
    }

    [Fact]
    public async Task GetBaseConfigurationAsyncLogsTransientFailureAtWarningAndThrows()
    {
        var records = new List<(LogLevel Level, string Message)>();
        var inner = new FakeBaseManager(
            _ => Task.FromException<BaseConfiguration>(new TimeoutException("IDX20807: timed out")));
        var manager = new LoggingConfigurationManager(inner, new RecordingLogger(records));

        await Assert.ThrowsAsync<TimeoutException>(() => manager.GetBaseConfigurationAsync(CancellationToken.None));

        var record = Assert.Single(records);
        Assert.Equal(LogLevel.Warning, record.Level);
    }

    [Fact]
    public async Task GetConfigurationAsyncReturnsTheRetrievedConfiguration()
    {
        var inner = new FakeBaseManager(
            _ => Task.FromResult<BaseConfiguration>(new OpenIdConnectConfiguration { Issuer = "https://issuer.example.com" }));
        var manager = new LoggingConfigurationManager(inner, NullLogger.Instance);

        var configuration = await manager.GetConfigurationAsync(CancellationToken.None);

        Assert.Equal("https://issuer.example.com", configuration.Issuer);
    }

    [Fact]
    public void RequestRefreshDelegatesToInnerManager()
    {
        var inner = new FakeBaseManager(
            _ => Task.FromResult<BaseConfiguration>(new OpenIdConnectConfiguration()));
        var manager = new LoggingConfigurationManager(inner, NullLogger.Instance);

        manager.RequestRefresh();

        Assert.True(inner.RefreshRequested);
    }

    [Fact]
    public void MetadataAddressIsForwardedFromInnerManager()
    {
        var inner = new FakeBaseManager(
            _ => Task.FromResult<BaseConfiguration>(new OpenIdConnectConfiguration()));
        var manager = new LoggingConfigurationManager(inner, NullLogger.Instance);

        Assert.Equal(inner.MetadataAddress, manager.MetadataAddress);
    }

    [Fact]
    public async Task FailuresAreRateLimitedToRefreshInterval()
    {
        var records = new List<(LogLevel Level, string Message)>();
        var inner = new FakeBaseManager(_ => Task.FromException<BaseConfiguration>(PermanentException()));
        var manager = new LoggingConfigurationManager(inner, new RecordingLogger(records));

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.GetBaseConfigurationAsync(CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.GetBaseConfigurationAsync(CancellationToken.None));

        Assert.Single(records);
    }

    private static InvalidOperationException PermanentException()
    {
        var inner = new IOException("IDX20807: Unable to retrieve document from: 'metadata'.");
        inner.Data[HttpDocumentRetriever.StatusCode] = HttpStatusCode.BadRequest;
        return new InvalidOperationException("IDX20803: Unable to obtain configuration.", inner);
    }

    private sealed class FakeBaseManager : BaseConfigurationManager
    {
        private readonly Func<CancellationToken, Task<BaseConfiguration>> _get;

        public FakeBaseManager(Func<CancellationToken, Task<BaseConfiguration>> get)
        {
            _get = get;
            MetadataAddress = "https://login.example.com/tenant/v2.0";
        }

        public bool RefreshRequested { get; private set; }

        public override Task<BaseConfiguration> GetBaseConfigurationAsync(CancellationToken cancel) => _get(cancel);

        public override void RequestRefresh() => RefreshRequested = true;
    }

    private sealed class RecordingLogger(List<(LogLevel Level, string Message)> records) : ILogger
    {
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            records.Add((logLevel, formatter(state, exception)));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}