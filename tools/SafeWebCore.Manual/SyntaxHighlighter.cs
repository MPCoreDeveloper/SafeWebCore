using System.Text.RegularExpressions;

namespace SafeWebCore.Manual;

/// <summary>
/// Colours a fenced code block into class names — <c>k</c> keyword, <c>s</c> string, <c>c</c> comment, <c>m</c> number,
/// <c>t</c> type or command. The classes are what <c>manual.css</c> styles, so the same book can be restyled without
/// touching this file.
/// </summary>
internal static partial class SyntaxHighlighter
{
    private const string SpanOpen = "<span class=\"";
    private const string SpanClose = "</span>";
    private const string CommentPattern = @"(?<c>//[^\n]*)";
    private const string StringPattern = @"(?<s>&quot;(?:[^&]|&(?!quot;))*?&quot;)";
    private const string NumberPattern = @"(?<m>\b\d+(?:\.\d+)?\b)";

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

    private static readonly string[] PowerShellKeywords =
    [
        "param", "if", "else", "foreach", "return", "function", "Set-Content", "Get-Content", "dotnet"
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
        var pattern = Join(CommentPattern, StringPattern, KeywordPattern(CSharpKeywords), NumberPattern);
        return Replace(code, pattern, static (match, builder, fallback) =>
            Append(builder, ClassOf(match, "c", "s", "k"), match.Value, fallback), "m");
    }

    private static string HighlightShell(string code)
    {
        var pattern = Join(CommentPattern, StringPattern, KeywordPattern(ShellKeywords));
        return Replace(code, pattern, static (match, builder, fallback) =>
            Append(builder, ClassOf(match, "c", "s"), match.Value, fallback), "k");
    }

    private static string HighlightPowerShell(string code)
    {
        var pattern = Join(CommentPattern, StringPattern, @"(?<v>\$[A-Za-z_][A-Za-z0-9_]*)", KeywordPattern(PowerShellKeywords));
        return Replace(code, pattern, static (match, builder, fallback) =>
            Append(builder, match.Groups["v"].Success ? "t" : ClassOf(match, "c", "s"), match.Value, fallback), "k");
    }

    private static string HighlightJson(string code)
    {
        var pattern = Join(StringPattern, NumberPattern, @"(?<k>\b(?:true|false|null)\b)");
        return Replace(code, pattern, static (match, builder, fallback) =>
            Append(builder, ClassOf(match, "s", "k"), match.Value, fallback), "m");
    }

    private static void Append(System.Text.StringBuilder builder, string cssClass, string value, string fallback)
    {
        builder.Append(SpanOpen).Append(cssClass.Length == 0 ? fallback : cssClass)
            .Append("\">").Append(value).Append(SpanClose);
    }

    private static string ClassOf(Match match, params string[] groups)
    {
        foreach (var group in groups)
        {
            if (match.Groups[group].Success)
            {
                return group;
            }
        }

        return string.Empty;
    }

    private static string Join(params string[] patterns) => string.Join('|', patterns);

    private static string Replace(
        string code,
        string pattern,
        Action<Match, System.Text.StringBuilder, string> append,
        string fallback)
    {
        var builder = new System.Text.StringBuilder(code.Length + 64);
        var position = 0;

        foreach (Match match in new Regex(pattern, RegexOptions.Multiline, TimeSpan.FromSeconds(2)).Matches(code))
        {
            builder.Append(code, position, match.Index - position);
            append(match, builder, fallback);
            position = match.Index + match.Length;
        }

        builder.Append(code, position, code.Length - position);
        return builder.ToString();
    }

    private static string KeywordPattern(IEnumerable<string> keywords) =>
        @"(?<k>\b(?:" + string.Join('|', keywords.OrderByDescending(static word => word.Length)) + @")\b)";
}
