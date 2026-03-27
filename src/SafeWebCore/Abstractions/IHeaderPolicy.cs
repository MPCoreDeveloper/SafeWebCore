using Microsoft.AspNetCore.Http;

namespace SafeWebCore.Abstractions;

/// <summary>
/// Interface for custom header policies.
/// </summary>
public interface IHeaderPolicy
{
    /// <summary>
    /// Applies the header policy to the response.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    void Apply(HttpResponse response);
}
