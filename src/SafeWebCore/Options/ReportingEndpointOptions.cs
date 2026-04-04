namespace SafeWebCore.Options;

/// <summary>
/// Represents a single Reporting API endpoint group entry used in the
/// <c>Reporting-Endpoints</c> response header.
/// </summary>
public sealed record ReportingEndpointOptions
{
    /// <summary>
    /// Reporting endpoint group name referenced by directives such as
    /// <c>report-to</c> in Content Security Policy.
    /// </summary>
    public required string Group { get; init; }

    /// <summary>
    /// Absolute endpoint URL that receives browser reports.
    /// </summary>
    public required string Url { get; init; }
}
