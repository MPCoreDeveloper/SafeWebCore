namespace SafeWebCore.Options;

/// <summary>
/// Represents a custom response header emitted by SafeWebCore.
/// Use this for first-class support of upcoming or non-standard headers.
/// </summary>
public sealed record AdditionalHeaderOptions
{
    /// <summary>
    /// Header name (for example <c>Document-Policy</c>).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Header value emitted for <see cref="Name"/>.
    /// </summary>
    public required string Value { get; init; }
}
