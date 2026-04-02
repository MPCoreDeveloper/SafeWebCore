using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using SafeWebCore.TagHelpers;

namespace SafeWebCore.Tests;

/// <summary>
/// Tests for CSP nonce TagHelpers.
/// </summary>
public sealed class CspNonceTagHelpersTests
{
    [Fact]
    public void ScriptTagHelperAddsNonceWhenMissing()
    {
        // Arrange
        var helper = new CspScriptNonceTagHelper(CreateHttpContextAccessorWithNonce("nonce-script"));
        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("script");

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("nonce-script", output.Attributes["nonce"]?.Value as string);
    }

    [Fact]
    public void ScriptTagHelperDoesNotOverrideExistingNonce()
    {
        // Arrange
        var helper = new CspScriptNonceTagHelper(CreateHttpContextAccessorWithNonce("nonce-script"));
        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("script");
        output.Attributes.SetAttribute("nonce", "existing");

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("existing", output.Attributes["nonce"]?.Value as string);
    }

    [Fact]
    public void StyleTagHelperAddsNonceWhenMissing()
    {
        // Arrange
        var helper = new CspStyleNonceTagHelper(CreateHttpContextAccessorWithNonce("nonce-style"));
        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("style");

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Equal("nonce-style", output.Attributes["nonce"]?.Value as string);
    }

    [Fact]
    public void StyleTagHelperSkipsNonceWhenUnavailable()
    {
        // Arrange
        var helper = new CspStyleNonceTagHelper(new HttpContextAccessor());
        var context = CreateTagHelperContext();
        var output = CreateTagHelperOutput("style");

        // Act
        helper.Process(context, output);

        // Assert
        Assert.Null(output.Attributes["nonce"]);
    }

    private static HttpContextAccessor CreateHttpContextAccessorWithNonce(string nonce)
    {
        var context = new DefaultHttpContext();
        context.Items[NetSecureHeaders.CspNonceKey] = nonce;
        return new HttpContextAccessor { HttpContext = context };
    }

    private static TagHelperContext CreateTagHelperContext()
        => new(
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object?>(),
            uniqueId: Guid.NewGuid().ToString("N"));

    private static TagHelperOutput CreateTagHelperOutput(string tagName)
        => new(
            tagName,
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: static (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
}
