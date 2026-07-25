using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SafeWebCore.Abstractions;

namespace SafeWebCore.Infrastructure;

/// <summary>
/// Dispatches security events to all registered <see cref="ISecurityEventSink"/> implementations.
/// This is additive and has no effect if no sinks are registered.
/// </summary>
public sealed class SecurityEventDispatcher
{
    private readonly IEnumerable<ISecurityEventSink> _sinks;

    /// <summary>
    /// Creates a new dispatcher that forwards events to all provided sinks.
    /// </summary>
    public SecurityEventDispatcher(IEnumerable<ISecurityEventSink> sinks)
    {
        _sinks = sinks;
    }

    /// <summary>
    /// Emits a security event to all registered sinks.
    /// </summary>
    public async Task EmitAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        foreach (var sink in _sinks)
        {
            await sink.WriteAsync(securityEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
