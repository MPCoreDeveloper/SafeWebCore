namespace SafeWebCore.Models;

/// <summary>
/// Represents a parsed CSP violation report payload.
/// </summary>
public sealed record CspViolationReport
{
    /// <summary>Document URI where the violation occurred.</summary>
    public string? DocumentUri { get; init; }

    /// <summary>Referrer value associated with the violating request.</summary>
    public string? Referrer { get; init; }

    /// <summary>Directive that was violated.</summary>
    public string? ViolatedDirective { get; init; }

    /// <summary>Effective directive applied by the browser.</summary>
    public string? EffectiveDirective { get; init; }

    /// <summary>Original policy as evaluated by the user agent.</summary>
    public string? OriginalPolicy { get; init; }

    /// <summary>URI of the blocked resource.</summary>
    public string? BlockedUri { get; init; }

    /// <summary>Enforcement disposition, such as <c>enforce</c> or <c>report</c>.</summary>
    public string? Disposition { get; init; }

    /// <summary>HTTP status code of the protected resource response.</summary>
    public int? StatusCode { get; init; }

    /// <summary>Source file associated with the violation.</summary>
    public string? SourceFile { get; init; }

    /// <summary>Line number where the violation originated.</summary>
    public int? LineNumber { get; init; }

    /// <summary>Column number where the violation originated.</summary>
    public int? ColumnNumber { get; init; }

    /// <summary>Code sample sent by the browser when available.</summary>
    public string? Sample { get; init; }
}
