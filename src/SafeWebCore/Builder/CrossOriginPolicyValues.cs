namespace SafeWebCore.Builder;

/// <summary>
/// Typed values for cross-origin security headers.
/// </summary>
/// <param name="Coep">Value for <c>Cross-Origin-Embedder-Policy</c>.</param>
/// <param name="Coop">Value for <c>Cross-Origin-Opener-Policy</c>.</param>
/// <param name="Corp">Value for <c>Cross-Origin-Resource-Policy</c>.</param>
public readonly record struct CrossOriginPolicyValues(string Coep, string Coop, string Corp);
