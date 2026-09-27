using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SafeWebCore.Abstractions;

namespace SafeWebCore.Infrastructure;

/// <summary>
/// Dispatches security events to all registered <see cref="ISecurityEventSink"/> implementations.
/// This is additive and has no effect if no sinks are registered.
/// </summary>
/// <remarks>
/// <para>
/// The sink sequence is materialized once, in the constructor, and every sink is invoked inside its
/// own <c>try</c>. One broken sink therefore cannot end delivery for the sinks registered after it,
/// and its failure cannot surface as an unobserved task exception — which matters because the
/// security call sites discard the returned task. A swallowed failure is counted in
/// <see cref="SafeWebCoreMetrics.SecurityEventSinkFailures"/>.
/// </para>
/// <para>
/// Sinks are invoked in registration order, which is deterministic, but no sink may depend on that
/// order or assume that another sink already ran.
/// </para>
/// </remarks>
public sealed class SecurityEventDispatcher
{
    private readonly ISecurityEventSink[] _sinks;
    private readonly SafeWebCoreMetrics? _metrics;

    /// <summary>
    /// Creates a new dispatcher that forwards events to all provided sinks.
    /// </summary>
    /// <param name="sinks">The sinks that receive the events.</param>
    public SecurityEventDispatcher(IEnumerable<ISecurityEventSink> sinks)
        : this(sinks, null)
    {
    }

    /// <summary>
    /// Creates a new dispatcher that forwards events to all provided sinks and counts isolated sink failures.
    /// </summary>
    /// <param name="sinks">The sinks that receive the events.</param>
    /// <param name="metrics">Optional metrics instance that counts isolated sink failures.</param>
    public SecurityEventDispatcher(IEnumerable<ISecurityEventSink> sinks, SafeWebCoreMetrics? metrics)
    {
        _sinks = sinks?.ToArray() ?? [];
        _metrics = metrics;
    }

    /// <summary>
    /// Emits a security event to all registered sinks. A sink that throws is isolated: the failure is
    /// counted and delivery continues with the next sink.
    /// </summary>
    /// <param name="securityEvent">The security event to emit.</param>
    /// <param name="cancellationToken">Cancellation token forwarded to every sink.</param>
    public async Task EmitAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default)
    {
        foreach (var sink in _sinks)
        {
            try
            {
                await sink.WriteAsync(securityEvent, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Telemetry must never break the response. Isolate the sink and count the failure.
                _metrics?.SecurityEventSinkFailures.Add(1);
            }
        }
    }
}
