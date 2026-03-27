using SafeWebCore;
using Xunit;

namespace SafeWebCore.Tests;

/// <summary>
/// Tests for NonceService.
/// </summary>
public class NonceServiceTests
{
    [Fact]
    public void GenerateNonceReturnsNonEmptyString()
    {
        // Arrange
        var service = new NonceService();

        // Act
        var nonce = service.GenerateNonce();

        // Assert
        Assert.NotNull(nonce);
        Assert.NotEmpty(nonce);
    }

    [Fact]
    public void GenerateNonceReturnsValidBase64()
    {
        // Arrange
        var service = new NonceService();

        // Act
        var nonce = service.GenerateNonce();

        // Assert
        Assert.True(IsValidBase64(nonce), "Nonce should be valid base64");
    }

    [Fact]
    public void GenerateNonceReturnsCorrectLength()
    {
        // Arrange
        var service = new NonceService();

        // Act
        var nonce = service.GenerateNonce();

        // Assert
        // 32 bytes base64 encoded: (32 * 4/3) = 42.666, rounded up to 44 with padding
        Assert.Equal(44, nonce.Length);
    }

    [Fact]
    public void GenerateNonceReturnsUniqueValues()
    {
        // Arrange
        var service = new NonceService();

        // Act
        var nonce1 = service.GenerateNonce();
        var nonce2 = service.GenerateNonce();

        // Assert
        Assert.NotEqual(nonce1, nonce2);
    }

    private static bool IsValidBase64(string value)
    {
        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
