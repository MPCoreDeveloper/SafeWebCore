using System.Globalization;
using System.Text;

namespace SafeWebCore.Manual;

/// <summary>A heading of the manual, in document order.</summary>
/// <param name="Level">The heading level, 1 to 4.</param>
/// <param name="Title">The heading text, without the markup.</param>
/// <param name="Anchor">The unique id of the heading in the assembled document.</param>
internal sealed record Heading(int Level, string Title, string Anchor);

/// <summary>The HTML body of one chapter and the headings it contains.</summary>
/// <param name="Html">The converted chapter.</param>
/// <param name="Headings">The headings, in the order they appear.</param>
internal sealed record Chapter(string Html, IReadOnlyList<Heading> Headings);

/// <summary>The values the cover states about the book.</summary>
/// <param name="Version">The version the manual describes.</param>
/// <param name="Product">The product name.</param>
/// <param name="Title">The document title, as metadata and the browser tab see it.</param>
internal sealed record ManualIdentity(string Version, string Product, string Title);

/// <summary>
/// Assembles the cover, the contents and the chapters into the one HTML document Chromium prints.
/// </summary>
internal static class ManualHtml
{
    private const string Description =
        "The complete SafeWebCore manual: security headers for ASP.NET Core - presets, Content Security Policy, "
        + "nonces, path policies, reporting, diagnostics, telemetry, fraud detection and JWT bearer hardening.";

    /// <summary>Builds the printable document.</summary>
    /// <param name="chapters">Every chapter, in reading order.</param>
    /// <param name="identity">The values the cover states.</param>
    /// <param name="stylesheetPath">The path of the print stylesheet.</param>
    /// <param name="logoPath">The path of the cover logo, or null when there is none.</param>
    /// <returns>The complete HTML document.</returns>
    public static string Assemble(
        IReadOnlyList<Chapter> chapters,
        ManualIdentity identity,
        string stylesheetPath,
        string? logoPath)
    {
        ArgumentNullException.ThrowIfNull(chapters);
        ArgumentNullException.ThrowIfNull(identity);

        var headings = new List<Heading>();
        var html = new StringBuilder();

        html.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\" />\n");
        html.Append("<title>").Append(Escape(identity.Title)).Append("</title>\n");
        html.Append("<meta name=\"author\" content=\"MPCoreDeveloper\" />\n");
        html.Append("<meta name=\"description\" content=\"").Append(Escape(Description)).Append("\" />\n");
        html.Append("<link rel=\"stylesheet\" href=\"").Append(FileUri(stylesheetPath)).Append("\" />\n");
        html.Append("</head>\n<body>\n");
        html.Append(Cover(identity, logoPath));

        foreach (var chapter in chapters)
        {
            headings.AddRange(chapter.Headings);
        }

        html.Append(Contents(headings));
        html.Append("<main class=\"manual-body\">\n");

        foreach (var chapter in chapters)
        {
            html.Append(chapter.Html).Append('\n');
        }

        html.Append("</main>\n</body>\n</html>\n");
        return html.ToString();
    }

    private static string Cover(ManualIdentity identity, string? logoPath)
    {
        var logo = logoPath is not null && File.Exists(logoPath)
            ? "<img class=\"logo\" src=\"" + FileUri(logoPath) + "\" alt=\"SafeWebCore\" />\n  "
            : string.Empty;

        return
            "<div class=\"cover\">\n" +
            "  " + logo + "<h1 class=\"title\">The " + Escape(identity.Product) + "<br />Manual</h1>\n" +
            "  <div class=\"rule\"></div>\n" +
            "  <div class=\"version\">Version " + Escape(identity.Version) + " &middot; .NET 10 and .NET 11</div>\n" +
            "  <p class=\"subtitle\">Security headers for ASP.NET Core that grade A+ and can be explained: presets you can\n" +
            "  read, a nonce-based Content Security Policy, path policies, report-only rollouts and the diagnostics that\n" +
            "  prove it.</p>\n" +
            "  <ul class=\"promise\">\n" +
            "    <li>The whole public API, from <strong>AddNetSecureHeadersStrictAPlus</strong> to <strong>GetCspNonce</strong></li>\n" +
            "    <li>Presets, the options model, CSP, nonces, Tag Helpers, path policies and endpoint overrides</li>\n" +
            "    <li>Report-only rollout, reporting sinks, the diagnostics endpoint, events and metrics</li>\n" +
            "    <li>Fraud detection, JWT bearer hardening, analyzers, the testing package and a cookbook</li>\n" +
            "  </ul>\n" +
            "  <div class=\"imprint\">\n" +
            "    SafeWebCore is published as <strong>SafeWebCore</strong>, with separate\n" +
            "    <strong>SafeWebCore.FraudDetection</strong>, <strong>SafeWebCore.JwtBearer</strong>,\n" +
            "    <strong>SafeWebCore.Analyzers</strong> and <strong>SafeWebCore.Testing</strong> packages on nuget.org.<br />\n" +
            "    This manual describes version " + Escape(identity.Version) + " exactly.\n" +
            "  </div>\n</div>\n\n";
    }

    private static string Contents(IReadOnlyList<Heading> headings)
    {
        var html = new StringBuilder();
        html.Append("<div class=\"toc-page\">\n<h1 id=\"contents\">Contents</h1>\n");

        foreach (var heading in headings)
        {
            if (heading.Level > 2)
            {
                continue;
            }

            html.Append("<div class=\"toc-entry lvl").Append(heading.Level.ToString(CultureInfo.InvariantCulture)).Append("\">")
                .Append("<span class=\"title\"><a href=\"#").Append(Escape(heading.Anchor)).Append("\">")
                .Append(Escape(heading.Title)).Append("</a></span>")
                .Append("<span class=\"dots\"></span></div>\n");
        }

        html.Append("<p class=\"toc-note\">Every chapter starts on a new page. The chapters are the files in ")
            .Append("<code>docs/manual/src</code>; Chapter 20 describes how they become this document.</p>\n</div>\n");

        return html.ToString();
    }

    private static string FileUri(string path) =>
        new Uri(Path.GetFullPath(path), UriKind.Absolute).AbsoluteUri;

    private static string Escape(string text) => text
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);
}
