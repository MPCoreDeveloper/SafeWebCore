using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SafeWebCore.FraudDetection.Abstractions;
using SafeWebCore.FraudDetection.Extensions;
using SafeWebCore.FraudDetection.Infrastructure;
using SafeWebCore.FraudDetection.Models;
using SafeWebCore.FraudDetection.Options;

namespace SafeWebCore.FraudDetection.Tests;

/// <summary>
/// Tests for the opt-in WebhookFraudEventSink and AddFraudWebhookSink helper.
/// Uses a recording HttpMessageHandler to avoid real network calls.
/// </summary>
public sealed class WebhookFraudEventSinkTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public string? LastContentType { get; private set; }

        public HttpStatusCode ResponseStatus { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? body = null;

            if (request.Content is not null)
            {
                // Capture the declared content type BEFORE consuming the stream
                LastContentType = request.Content.Headers.ContentType?.MediaType;
                body = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            if (body is not null)
            {
                clone.Content = new StringContent(body, Encoding.UTF8, LastContentType ?? "application/json");
            }

            Requests.Add(clone);

            return new HttpResponseMessage(ResponseStatus);
        }
    }

    [Fact]
    public void AddFraudWebhookSinkRegistersSink()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();

        services.AddSafeWebCoreFraudDetection((Action<FraudDetectionOptions>)(_ => { }));
        services.AddFraudWebhookSink("https://example.com/webhook", "TestWebhook");

        var provider = services.BuildServiceProvider();

        var sinks = provider.GetServices<IFraudEventSink>().ToList();
        Assert.Contains(sinks, s => s is WebhookFraudEventSink);
    }

    [Fact]
    public async Task WebhookSinkPostsJsonOnEvent()
    {
        var handler = new RecordingHandler();
        var httpClientFactory = new TestHttpClientFactory(handler, "TestWebhook");

        var sink = new WebhookFraudEventSink(httpClientFactory, "TestWebhook", "https://hooks.example.com/fraud");

        var report = new FraudReport
        {
            SuspicionScore = 87,
            Verdict = FraudVerdict.RegionImpersonation,
            RecommendedAction = RecommendedAction.BlockRequest,
            TenantId = "tenant-42"
        };

        var fraudEvent = new FraudEvent
        {
            Report = report,
            Timestamp = DateTimeOffset.UtcNow
        };

        sink.OnFraudEvent(fraudEvent);

        // Allow async fire-and-forget to complete
        await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.Single(handler.Requests);
        var req = handler.Requests[0];

        Assert.Equal("https://hooks.example.com/fraud", req.RequestUri!.ToString());
        Assert.Equal("application/json", handler.LastContentType);

        var body = await (req.Content?.ReadAsStringAsync(TestContext.Current.CancellationToken) ?? Task.FromResult(string.Empty));

        var received = JsonSerializer.Deserialize<FraudEvent>(body, JsonOptions);
        Assert.NotNull(received);
        Assert.NotNull(received.Report);
        Assert.Equal(87, received.Report.SuspicionScore);
        Assert.Equal(FraudVerdict.RegionImpersonation, received.Report.Verdict);
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly RecordingHandler _handler;
        private readonly string _name;

        public TestHttpClientFactory(RecordingHandler handler, string name)
        {
            _handler = handler;
            _name = name;
        }

        public HttpClient CreateClient(string name)
        {
            if (!string.Equals(name, _name, StringComparison.Ordinal))
                throw new InvalidOperationException($"Unexpected client name {name}");

            return new HttpClient(_handler, disposeHandler: false);
        }
    }
}
