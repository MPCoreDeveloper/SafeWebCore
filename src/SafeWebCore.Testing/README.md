# SafeWebCore.Testing

Consumer-facing testing helpers for SafeWebCore.

## Features

- Assert common security headers are present with expected values
- Assert CSP mode (enforce vs report-only)
- Assert nonce presence and consistency
- Bootstrap helpers for `WebApplicationFactory` and `TestServer`

## Example usage

```csharp
using SafeWebCore.Testing;
using Xunit;

public class SecurityHeadersTests : IClassFixture<MyWebAppFactory>
{
    private readonly HttpClient _client;

    public SecurityHeadersTests(MyWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ReturnsExpectedSecurityHeaders()
    {
        var response = await _client.GetAsync("/");

        response.AssertHasSecurityHeaders();
        response.AssertHasCspEnforceMode();
        response.AssertHasNonceInCsp();
    }
}
```

## Compatibility

This package is additive and does not affect runtime behavior.
