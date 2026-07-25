using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SafeWebCore.Infrastructure;
using SafeWebCore.Metadata;

namespace SafeWebCore.Extensions;

/// <summary>
/// Endpoint mapping extensions for SafeWebCore diagnostics.
/// </summary>
public static class DiagnosticsEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps an opt-in diagnostics endpoint that previews the effective SafeWebCore policy and headers.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern. Default: <c>/safewebcore/diagnostics</c>.</param>
    /// <returns>The endpoint convention builder for further customization.</returns>
    public static IEndpointConventionBuilder MapSafeWebCoreDiagnostics(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/safewebcore/diagnostics")
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.MapGet(pattern, (
            string? path,
            CspEndpointMode? cspMode,
            INetSecureHeadersDiagnosticsService diagnostics) =>
        {
            var snapshot = diagnostics.CreateSnapshot(path, cspMode);
            return Results.Json(snapshot);
        });
    }
}
