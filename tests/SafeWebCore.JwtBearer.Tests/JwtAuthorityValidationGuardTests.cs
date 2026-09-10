using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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

    private static JwtAuthorityValidationGuard CreateGuard(FakeManager manager, bool failFast)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
        {
            o.ConfigurationManager = manager;
            o.TokenValidationParameters.ValidateAudience = false;
            o.TokenValidationParameters.ValidateIssuer = false;
        });
        services.Configure<JwtAuthorityValidationOptions>(o => o.FailFast = failFast);
        services.AddHostedService<JwtAuthorityValidationGuard>();

        using var provider = services.BuildServiceProvider();
        return provider.GetServices<IHostedService>().OfType<JwtAuthorityValidationGuard>().Single();
    }

    private static FakeManager SuccessfulManager()
        => new(() => Task.FromResult(new OpenIdConnectConfiguration()));

    private static FakeManager PermanentFailureManager()
    {
        var inner = new IOException("IDX20807: Unable to retrieve document from: 'metadata'.");
        inner.Data[HttpDocumentRetriever.StatusCode] = HttpStatusCode.BadRequest;
        var outer = new InvalidOperationException("IDX20803: Unable to obtain configuration.", inner);
        return new FakeManager(() => Task.FromException<OpenIdConnectConfiguration>(outer));
    }

    private sealed class FakeManager(Func<Task<OpenIdConnectConfiguration>> get) : IConfigurationManager<OpenIdConnectConfiguration>
    {
        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancellationToken) => get();

        public void RequestRefresh()
        {
        }
    }
}