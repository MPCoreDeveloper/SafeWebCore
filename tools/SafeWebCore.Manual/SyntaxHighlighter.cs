using System.Text.RegularExpressions;

namespace SafeWebCore.Manual;

/// <summary>
/// Colours a fenced code block into class names — <c>k</c> keyword, <c>s</c> string, <c>c</c> comment, <c>m</c> number,
/// <c>t</c> type or command, <c>p</c> punctuation. The classes are what <c>manual.css</c> styles, so the same book can be
/// restyled without touching this file.
/// </summary>
internal static partial class SyntaxHighlighter
{
    private static readonly string[] CSharpKeywords =
    [
        "abstract", "as", "async", "await", "base", "bool", "break", "case", "catch", "class", "const", "continue",
        "default", "delegate", "do", "double", "else", "enum", "false", "finally", "for", "foreach", "get", "if",
        "implicit", "in", "init", "int", "interface", "internal", "is", "long", "nameof", "namespace", "new", "null",
        "object", "override", "params", "private", "protected", "public", "readonly", "record", "required", "return",
        "sealed", "set", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "using",
        "var", "virtual", "void", "when", "where", "while"
    ];

    private static readonly string[] ShellKeywords =
    [
        "dotnet", "curl", "git", "npm", "cd", "export", "echo", "set", "jq"
    ];

    /// <summary>Highlights <paramref name="code"/> for <paramref name="language"/>.</summary>
    /// <param name="code">The escaped code text.</param>
    /// <param name="language">The language of the fence; unknown languages are returned unchanged.</param>
    /// <returns>HTML with the tokens wrapped in class names.</returns>
    public static string Highlight(string code, string language) => language.ToLowerInvariant() switch
    {
        "csharp" or "cs" or "c#" => HighlightCSharp(code),
        "bash" or "shell" or "sh" => HighlightShell(code),
        "powershell" or "ps1" => HighlightPowerShell(code),
        "json" => HighlightJson(code),
        _ => code
    };

    private static string HighlightCSharp(string code)
    {
        var pattern = CommentPattern() + "|" + StringPattern() + "|" + KeywordPattern(CSharpKeywords) + "|" + NumberPattern();
        return Replace(code, pattern, static (match, builder) =>
        {
            var group = match.Groups;
            var cls = group["c"].Success ? "c" : group["s"].Success ? "s" : group["k"].Success ? "k" : "m";
            builder.Append("<span class=\"").Append(cls).Append("\">").Append(match.Value).Append("</span>");
        });
    }

    private static string HighlightShell(string code)
    {
        var pattern = CommentPattern() + "|" + StringPattern() + "|" + KeywordPattern(ShellKeywords);
        return Replace(code, pattern, static (match, builder) =>
        {
            var cls = match.Groups["c"].Success ? "c" : match.Groups["s"].Success ? "s" : "k";
            builder.Append("<span class=\"").Append(cls).Append("\">").Append(match.Value).Append("</span>");
        });
    }

    private static string HighlightPowerShell(string code)
    {
        var pattern = CommentPattern() + "|" + StringPattern() + @"|(?<v>\$[A-Za-z_][A-Za-z0-9_]*)|" + KeywordPattern(["param", "if", "else", "foreach", "return", "function", "Set-Content", "Get-Content", "dotnet"]);
        return Replace(code, pattern, static (match, builder) =>
        {
            if (match.Groups["v"].Success)
            {
                builder.Append("<span class=\"t\">").Append(match.Value).Append("</span>");
                return;
            }

            var cls = match.Groups["c"].Success ? "c" : match.Groups["s"].Success ? "s" : "k";
            builder.Append("<span class=\"").Append(cls).Append("\">").Append(match.Value).Append("</span>");
        });
    }

    private static string HighlightJson(string code)
    {
        var pattern = StringPattern() + "|" + NumberPattern() + @"|(?<k>\b(?:true|false|null)\b)";
        return Replace(code, pattern, static (match, builder) =>
        {
            var cls = match.Groups["s"].Success ? "s" : match.Groups["k"].Success ? "k" : "m";
            builder.Append("<span class=\"").Append(cls).Append("\">").Append(match.Value).Append("</span>");
        });
    }

    private static string Replace(string code, string pattern, Action<Match, System.Text.StringBuilder> append)
    {
        var builder = new System.Text.StringBuilder(code.Length + 64);
        var position = 0;

        foreach (Match match in new Regex(pattern, RegexOptions.Multiline, TimeSpan.FromSeconds(2)).Matches(code))
        {
            builder.Append(code, position, match.Index - position);
            append(match, builder);
            position = match.Index + match.Length;
        }

        builder.Append(code, position, code.Length - position);
        return builder.ToString();
    }

    private static string CommentPattern() => @"(?<c>//[^\n]*)";

    private static string StringPattern() => @"(?<s>&quot;(?:[^&]|&(?!quot;))*?&quot;)";

    private static string NumberPattern() => @"(?<m>\b\d+(?:\.\d+)?\b)";

    private static string KeywordPattern(IEnumerable<string> keywords) =>
        @"(?<k>\b(?:" + string.Join('|', keywords.OrderByDescending(static word => word.Length)) + @")\b)";
}
