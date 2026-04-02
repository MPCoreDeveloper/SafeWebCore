namespace SafeWebCore.Builder;

/// <summary>
/// Fluent builder for composing typed <c>Referrer-Policy</c> header values.
/// </summary>
public sealed class ReferrerPolicyBuilder
{
    private string _value = "strict-origin-when-cross-origin";

    /// <summary>Uses <c>no-referrer</c>.</summary>
    public ReferrerPolicyBuilder NoReferrer() { _value = "no-referrer"; return this; }

    /// <summary>Uses <c>no-referrer-when-downgrade</c>.</summary>
    public ReferrerPolicyBuilder NoReferrerWhenDowngrade() { _value = "no-referrer-when-downgrade"; return this; }

    /// <summary>Uses <c>origin</c>.</summary>
    public ReferrerPolicyBuilder Origin() { _value = "origin"; return this; }

    /// <summary>Uses <c>origin-when-cross-origin</c>.</summary>
    public ReferrerPolicyBuilder OriginWhenCrossOrigin() { _value = "origin-when-cross-origin"; return this; }

    /// <summary>Uses <c>same-origin</c>.</summary>
    public ReferrerPolicyBuilder SameOrigin() { _value = "same-origin"; return this; }

    /// <summary>Uses <c>strict-origin</c>.</summary>
    public ReferrerPolicyBuilder StrictOrigin() { _value = "strict-origin"; return this; }

    /// <summary>Uses <c>strict-origin-when-cross-origin</c>.</summary>
    public ReferrerPolicyBuilder StrictOriginWhenCrossOrigin() { _value = "strict-origin-when-cross-origin"; return this; }

    /// <summary>Uses <c>unsafe-url</c>.</summary>
    public ReferrerPolicyBuilder UnsafeUrl() { _value = "unsafe-url"; return this; }

    /// <summary>
    /// Builds the final <c>Referrer-Policy</c> header value.
    /// </summary>
    /// <returns>The configured referrer policy value.</returns>
    public string Build() => _value;
}
