using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SafeWebCore.Manual;

/// <summary>
/// Converts the subset of Markdown the chapters use into HTML: ATX headings with unique anchors, fenced code with a
/// language label, pipe tables, single-line blockquotes, flat lists, inline code, bold, italic, links and rules.
/// </summary>
internal static class MarkdownConverter
{
    /// <summary>Converts one chapter.</summary>
    /// <param name="markdown">The chapter source.</param>
    /// <param name="usedAnchors">Anchors already used in the book; this call adds the ones it creates.</param>
    /// <returns>The HTML body and the headings it contains.</returns>
    public static Chapter Convert(string markdown, ISet<string> usedAnchors)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(usedAnchors);

        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var html = new StringBuilder();
        var headings = new List<Heading>();
        var paragraph = new List<string>();
        var index = 0;

        while (index < lines.Length)
        {
            var line = lines[index];

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                FlushParagraph(html, paragraph);
                index = AppendCode(html, lines, index);
                continue;
            }

            if (line.StartsWith('#'))
            {
                FlushParagraph(html, paragraph);
                AppendHeading(html, headings, usedAnchors, line);
                index++;
                continue;
            }

            if (line.StartsWith('|'))
            {
                FlushParagraph(html, paragraph);
                index = AppendTable(html, lines, index);
                continue;
            }

            if (line.StartsWith("> ", StringComparison.Ordinal) || line == ">")
            {
                FlushParagraph(html, paragraph);
                index = AppendQuote(html, lines, index);
                continue;
            }

            if (line is "---" or "***" or "___")
            {
                FlushParagraph(html, paragraph);
                html.Append("<hr />\n");
                index++;
                continue;
            }

            if (IsListItem(line))
            {
                FlushParagraph(html, paragraph);
                index = AppendList(html, lines, index);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph(html, paragraph);
                index++;
                continue;
            }

            paragraph.Add(line.TrimEnd());
            index++;
        }

        FlushParagraph(html, paragraph);
        return new Chapter(html.ToString(), headings);
    }

    private static void AppendHeading(StringBuilder html, List<Heading> headings, ISet<string> usedAnchors, string line)
    {
        var level = 0;
        while (level < line.Length && line[level] == '#')
        {
            level++;
        }

        level = Math.Clamp(level, 1, 4);
        var title = line[level..].Trim();
        var anchor = UniqueAnchor(title, usedAnchors);

        headings.Add(new Heading(level, title, anchor));
        html.Append("<h").Append(level.ToString(CultureInfo.InvariantCulture)).Append(" id=\"").Append(anchor).Append("\">")
            .Append(Inline(title)).Append("</h").Append(level.ToString(CultureInfo.InvariantCulture)).Append(">\n");
    }

    private static int AppendCode(StringBuilder html, string[] lines, int index)
    {
        var language = lines[index][3..].Trim();
        var code = new StringBuilder();
        index++;

        while (index < lines.Length && !lines[index].StartsWith("```", StringComparison.Ordinal))
        {
            code.Append(lines[index]).Append('\n');
            index++;
        }

        var escaped = Escape(code.ToString());
        html.Append("<pre>");
        if (!string.IsNullOrEmpty(language))
        {
            html.Append("<span class=\"lang\">").Append(Escape(language)).Append("</span>");
        }

        html.Append("<code>").Append(SyntaxHighlighter.Highlight(escaped, language)).Append("</code></pre>\n");
        return index + 1;
    }

    private static int AppendTable(StringBuilder html, string[] lines, int index)
    {
        var rows = new List<string>();

        while (index < lines.Length && lines[index].StartsWith('|'))
        {
            rows.Add(lines[index]);
            index++;
        }

        if (rows.Count < 2)
        {
            html.Append("<p>").Append(Inline(string.Join(' ', rows))).Append("</p>\n");
            return index;
        }

        html.Append("<table>\n<thead>\n<tr>");
        foreach (var cell in Cells(rows[0]))
        {
            html.Append("<th>").Append(Inline(cell)).Append("</th>");
        }

        html.Append("</tr>\n</thead>\n<tbody>\n");

        for (var row = 2; row < rows.Count; row++)
        {
            html.Append("<tr>");
            foreach (var cell in Cells(rows[row]))
            {
                html.Append("<td>").Append(Inline(cell)).Append("</td>");
            }

            html.Append("</tr>\n");
        }

        html.Append("</tbody>\n</table>\n");
        return index;
    }

    private static int AppendQuote(StringBuilder html, string[] lines, int index)
    {
        var content = new List<string>();

        while (index < lines.Length && (lines[index].StartsWith("> ", StringComparison.Ordinal) || lines[index] == ">"))
        {
            content.Add(lines[index].TrimStart('>').Trim());
            index++;
        }

        html.Append("<blockquote>\n<p>").Append(Inline(string.Join(' ', content))).Append("</p>\n</blockquote>\n");
        return index;
    }

    private static int AppendList(StringBuilder html, string[] lines, int index)
    {
        var trimmed = lines[index].TrimStart();
        var ordered = IsOrderedItem(trimmed);
        var tag = ordered ? "ol" : "ul";

        html.Append('<').Append(tag).Append(">\n");

        while (index < lines.Length && IsListItem(lines[index]))
        {
            var item = lines[index].Trim();
            var text = IsOrderedItem(item)
                ? item[(item.IndexOf(". ", StringComparison.Ordinal) + 2)..]
                : item[2..];

            html.Append("<li>").Append(Inline(text.Trim())).Append("</li>\n");
            index++;
        }

        html.Append("</").Append(tag).Append(">\n");
        return index;
    }

    private static bool IsListItem(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("- ", StringComparison.Ordinal)
            || trimmed.StartsWith("* ", StringComparison.Ordinal)
            || IsOrderedItem(trimmed);
    }

    private static bool IsOrderedItem(string trimmed)
    {
        var dot = trimmed.IndexOf(". ", StringComparison.Ordinal);
        return dot > 0 && trimmed[..dot].All(char.IsAsciiDigit);
    }

    private static void FlushParagraph(StringBuilder html, List<string> paragraph)
    {
        if (paragraph.Count == 0)
        {
            return;
        }

        html.Append("<p>").Append(Inline(string.Join(' ', paragraph))).Append("</p>\n");
        paragraph.Clear();
    }

    private static IEnumerable<string> Cells(string row) =>
        row.Trim().Trim('|').Split('|').Select(static cell => cell.Trim());

    private static string UniqueAnchor(string title, ISet<string> usedAnchors)
    {
        var slug = new StringBuilder();
        foreach (var character in title.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                slug.Append(character);
            }
            else if (character is ' ' or '-' or '_')
            {
                slug.Append('-');
            }
        }

        var candidate = slug.ToString().Trim('-');
        if (candidate.Length == 0)
        {
            candidate = "section";
        }

        var unique = candidate;
        var suffix = 2;
        while (!usedAnchors.Add(unique))
        {
            unique = candidate + "-" + suffix.ToString(CultureInfo.InvariantCulture);
            suffix++;
        }

        return unique;
    }

    private static string Inline(string text)
    {
        var escaped = Escape(text);
        var result = CodeSpan().Replace(escaped, static match => "<code>" + match.Groups["code"].Value + "</code>");
        result = Link().Replace(result, static match =>
            "<a href=\"" + match.Groups["url"].Value + "\">" + match.Groups["text"].Value + "</a>");
        result = Bold().Replace(result, static match => "<strong>" + match.Groups["b"].Value + "</strong>");
        return Italic().Replace(result, static match => "<em>" + match.Groups["i"].Value + "</em>");
    }

    private static string Escape(string text) => text
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);

    private static Regex CodeSpan() => new(@"`(?<code>[^`]+)`", RegexOptions.None, TimeSpan.FromSeconds(2));

    private static Regex Link() =>
        new(@"\[(?<text>[^\]]+)\]\((?<url>[^)]+)\)", RegexOptions.None, TimeSpan.FromSeconds(2));

    private static Regex Bold() => new(@"\*\*(?<b>[^*]+)\*\*", RegexOptions.None, TimeSpan.FromSeconds(2));

    private static Regex Italic() => new(@"(?<!\*)\*(?<i>[^*]+)\*(?!\*)", RegexOptions.None, TimeSpan.FromSeconds(2));
}
