using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SafeWebCore.TagHelpers;

/// <summary>
/// Automatically adds the per-request CSP nonce to <c>&lt;script&gt;</c> elements
/// when the <c>nonce</c> attribute is not explicitly set.
/// </summary>
[HtmlTargetElement("script")]
public sealed class CspScriptNonceTagHelper(IHttpContextAccessor httpContextAccessor) : TagHelper
{
    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        if (output.Attributes.ContainsName("nonce"))
            return;

        var nonce = httpContextAccessor.HttpContext?.Items[NetSecureHeaders.CspNonceKey] as string;
        if (!string.IsNullOrWhiteSpace(nonce))
        {
            output.Attributes.SetAttribute("nonce", nonce);
        }
    }
}
