using SafeWebCore.Metadata;

namespace SafeWebCore.Attributes;

/// <summary>
/// Overrides CSP emission mode for the targeted endpoint.
/// Can be used as endpoint metadata and MVC action/controller attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CspModeAttribute(CspEndpointMode mode) : Attribute
{
    /// <summary>
    /// Gets the configured endpoint-level CSP mode.
    /// </summary>
    public CspEndpointMode Mode { get; } = mode;
}
