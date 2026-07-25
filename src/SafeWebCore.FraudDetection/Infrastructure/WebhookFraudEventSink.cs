using System.Net.Http;
using System.Text;
using System.Text.Json;
using SafeWebCore.FraudDetection.Abstractions;

namespace SafeWebCore.FraudDetection.Infrastructure;

/// <summary>
/// An opt-in <see cref="IFraudEventSink"/> that posts <see cref="FraudEvent"/> payloads
/// as JSON to a configured webhook URL.
/// 
/// This is additive and best-effort: failures are swallowed so that webhook issues
/// never impact fraud detection or application behavior.
/// </summary>
public sealed class WebhookFraudEventSink : IFraudEventSink
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _httpClientName;
    private readonly string _webhookUrl;

    /// <summary>
    /// Creates a new webhook sink.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory used to create clients.</param>
    /// <param name="httpClientName">The named client to use (allows per-webhook configuration).</param>
    /// <param name="webhookUrl">The absolute URL to POST events to.</param>
    public WebhookFraudEventSink(
        IHttpClientFactory httpClientFactory,
        string httpClientName,
        string webhookUrl)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(httpClientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookUrl);

        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Webhook URL must be an absolute URI.", nameof(webhookUrl));
        }

        _httpClientFactory = httpClientFactory;
        _httpClientName = httpClientName;
        _webhookUrl = webhookUrl;
    }

    /// <inheritdoc />
    public void OnFraudEvent(FraudEvent fraudEvent)
    {
        if (fraudEvent is null)
            return;

        // Fire and forget — sinks must never block the caller
        _ = SendAsync(fraudEvent);
    }

    private async Task SendAsync(FraudEvent fraudEvent)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(_httpClientName);

            var json = JsonSerializer.Serialize(fraudEvent, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Best effort: ignore response and any errors
            using var response = await client.PostAsync(_webhookUrl, content).ConfigureAwait(false);
        }
        catch
        {
            // Intentionally swallow all errors — webhook delivery is non-critical
        }
    }
}
