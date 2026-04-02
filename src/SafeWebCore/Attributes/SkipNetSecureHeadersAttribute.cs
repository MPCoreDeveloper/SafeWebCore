namespace SafeWebCore.Attributes;

/// <summary>
/// Skips SafeWebCore security header emission for the targeted endpoint.
/// Can be used as endpoint metadata and MVC action/controller attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipNetSecureHeadersAttribute : Attribute;
