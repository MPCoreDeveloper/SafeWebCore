using Microsoft.AspNetCore.Mvc;
using SafeWebCore.Attributes;

namespace SafeWebCore.Examples.MvcApp.Controllers;

/// <summary>
/// Home controller demonstrating the [CspNonce] attribute and ViewData nonce injection.
/// </summary>
[CspNonce]
public sealed class HomeController : Controller
{
    /// <summary>
    /// Renders the main page. The [CspNonce] attribute on the controller
    /// automatically stores the per-request nonce in ViewData["CspNonce"].
    /// The Razor view uses it on &lt;script&gt; and &lt;style&gt; tags, or you can
    /// let the SafeWebCore TagHelpers inject it automatically.
    /// </summary>
    public IActionResult Index() => View();

    /// <summary>
    /// A public-facing page under /public — matched by the path policy
    /// configured in Program.cs, which uses CSP report-only mode.
    /// </summary>
    [Route("/public/page")]
    public IActionResult PublicPage() => View("Index");
}
