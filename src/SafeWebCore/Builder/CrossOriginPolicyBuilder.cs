namespace SafeWebCore.Builder;

/// <summary>
/// Fluent builder for typed COEP, COOP, and CORP header values.
/// </summary>
public sealed class CrossOriginPolicyBuilder
{
    private const string SameOrigin = "same-origin";

    private string _coep = "require-corp";
    private string _coop = SameOrigin;
    private string _corp = SameOrigin;

    /// <summary>Sets COEP to <c>require-corp</c>.</summary>
    public CrossOriginPolicyBuilder CoepRequireCorp() { _coep = "require-corp"; return this; }

    /// <summary>Sets COEP to <c>credentialless</c>.</summary>
    public CrossOriginPolicyBuilder CoepCredentialless() { _coep = "credentialless"; return this; }

    /// <summary>Sets COOP to <c>unsafe-none</c>.</summary>
    public CrossOriginPolicyBuilder CoopUnsafeNone() { _coop = "unsafe-none"; return this; }

    /// <summary>Sets COOP to <c>same-origin</c>.</summary>
    public CrossOriginPolicyBuilder CoopSameOrigin() { _coop = SameOrigin; return this; }

    /// <summary>Sets COOP to <c>same-origin-allow-popups</c>.</summary>
    public CrossOriginPolicyBuilder CoopSameOriginAllowPopups() { _coop = "same-origin-allow-popups"; return this; }

    /// <summary>Sets CORP to <c>same-origin</c>.</summary>
    public CrossOriginPolicyBuilder CorpSameOrigin() { _corp = SameOrigin; return this; }

    /// <summary>Sets CORP to <c>same-site</c>.</summary>
    public CrossOriginPolicyBuilder CorpSameSite() { _corp = "same-site"; return this; }

    /// <summary>Sets CORP to <c>cross-origin</c>.</summary>
    public CrossOriginPolicyBuilder CorpCrossOrigin() { _corp = "cross-origin"; return this; }

    /// <summary>
    /// Builds typed cross-origin policy values.
    /// </summary>
    /// <returns>The configured COEP, COOP, and CORP values.</returns>
    public CrossOriginPolicyValues Build() => new(_coep, _coop, _corp);
}
