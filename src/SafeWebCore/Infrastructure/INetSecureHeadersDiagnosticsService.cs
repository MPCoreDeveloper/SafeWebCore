using SafeWebCore.Metadata;

namespace SafeWebCore.Infrastructure;

internal interface INetSecureHeadersDiagnosticsService
{
    object CreateSnapshot(string? path = null, CspEndpointMode? endpointCspMode = null);
}
