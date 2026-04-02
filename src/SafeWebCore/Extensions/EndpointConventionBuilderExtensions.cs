using Microsoft.AspNetCore.Builder;
using SafeWebCore.Attributes;
using SafeWebCore.Metadata;

namespace SafeWebCore.Extensions;

/// <summary>
/// Endpoint convention extensions for endpoint-level SafeWebCore overrides.
/// </summary>
public static class EndpointConventionBuilderExtensions
{
    /// <summary>
    /// Skips SafeWebCore security header emission for the configured endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static TBuilder SkipNetSecureHeaders<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithMetadata(new SkipNetSecureHeadersAttribute());
        return builder;
    }

    /// <summary>
    /// Overrides CSP emission mode for the configured endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder type.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <param name="mode">Endpoint CSP mode override.</param>
    /// <returns>The same builder for chaining.</returns>
    public static TBuilder WithCspMode<TBuilder>(this TBuilder builder, CspEndpointMode mode)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithMetadata(new CspModeAttribute(mode));
        return builder;
    }
}
