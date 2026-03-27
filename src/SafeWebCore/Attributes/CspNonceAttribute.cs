using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SafeWebCore.Attributes;

/// <summary>
/// Action filter that injects the CSP nonce into ViewData for use in Razor views.
/// Apply to controllers or actions that need nonce access in their views.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix — this is a valid filter attribute
public sealed class CspNonceAttribute : ActionFilterAttribute
#pragma warning restore CA1711
{
    /// <inheritdoc />
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Result is ViewResult viewResult)
        {
            var nonce = context.HttpContext.Items[NetSecureHeaders.CspNonceKey] as string;
            if (!string.IsNullOrEmpty(nonce))
            {
                viewResult.ViewData["CspNonce"] = nonce;
            }
        }

        base.OnActionExecuted(context);
    }
}
