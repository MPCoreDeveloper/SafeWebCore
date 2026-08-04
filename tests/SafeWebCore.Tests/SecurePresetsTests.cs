using SafeWebCore.Options;
using SafeWebCore.Presets;

namespace SafeWebCore.Tests;

/// <summary>
/// Tests for <see cref="SecurePresets.StrictAPlus"/>.
/// </summary>
public sealed class SecurePresetsTests
{
    private readonly NetSecureHeadersOptions _options = SecurePresets.StrictAPlus();

    // ── Transport security ─────────────────────────────────────────────────

    [Fact]
    public void StrictAPlusHstsEnabledWithTwoYearMaxAge()
    {
        // Assert
        Assert.True(_options.EnableHsts);
        Assert.Contains("max-age=63072000", _options.HstsValue);
        Assert.Contains("includeSubDomains", _options.HstsValue);
        Assert.Contains("preload", _options.HstsValue);
    }

    // ── Framing protection ─────────────────────────────────────────────────

    [Fact]
    public void StrictAPlusXFrameOptionsSetToDeny()
    {
        // Assert
        Assert.True(_options.EnableXFrameOptions);
        Assert.Equal("DENY", _options.XFrameOptionsValue);
    }

    // ── MIME-type sniffing ─────────────────────────────────────────────────

    [Fact]
    public void StrictAPlusXContentTypeOptionsSetToNoSniff()
    {
        // Assert
        Assert.True(_options.EnableXContentTypeOptions);
        Assert.Equal("nosniff", _options.XContentTypeOptionsValue);
    }

    // ── Referrer ───────────────────────────────────────────────────────────

    [Fact]
    public void StrictAPlusReferrerPolicySetToNoReferrer()
    {
        // Assert
        Assert.True(_options.EnableReferrerPolicy);
        Assert.Equal("no-referrer", _options.ReferrerPolicyValue);
    }

    // ── Permissions ────────────────────────────────────────────────────────

    [Fact]
    public void StrictAPlusPermissionsPolicyDeniesAllFeatures()
    {
        // Assert
        Assert.True(_options.EnablePermissionsPolicy);
        Assert.Contains("camera=()", _options.PermissionsPolicyValue);
        Assert.Contains("microphone=()", _options.PermissionsPolicyValue);
        Assert.Contains("geolocation=()", _options.PermissionsPolicyValue);
        Assert.Contains("payment=()", _options.PermissionsPolicyValue);
        Assert.Contains("usb=()", _options.PermissionsPolicyValue);
        Assert.Contains("fullscreen=()", _options.PermissionsPolicyValue);
    }

    [Fact]
    public void StrictAPlusPermissionsPolicyIncludesModernTokens()
    {
        // Modern valid tokens (Chromium-recognised, no scanner warnings).
        // Note: identity-credentials-get, otp-credentials, publickey-credentials-create, window-management
        // were removed because security scanners flag them as invalid directives.
        Assert.Contains("clipboard-read=()", _options.PermissionsPolicyValue);
        Assert.Contains("clipboard-write=()", _options.PermissionsPolicyValue);
        Assert.Contains("local-fonts=()", _options.PermissionsPolicyValue);
    }

    [Fact]
    public void StrictAPlusPermissionsPolicyExcludesStaleTokens()
    {
        // These tokens were removed because they are either stale or cause
        // "invalid directive" warnings from security scanners (securityheaders.com etc.).
        Assert.DoesNotContain("ambient-light-sensor", _options.PermissionsPolicyValue);
        Assert.DoesNotContain("battery", _options.PermissionsPolicyValue);
        Assert.DoesNotContain("cross-origin-isolated", _options.PermissionsPolicyValue);
        Assert.DoesNotContain("document-domain", _options.PermissionsPolicyValue);
        Assert.DoesNotContain("execution-while-not-rendered", _options.PermissionsPolicyValue);
        Assert.DoesNotContain("execution-while-out-of-viewport", _options.PermissionsPolicyValue);
        Assert.DoesNotContain("navigation-override", _options.PermissionsPolicyValue);
        Assert.DoesNotContain("sync-xhr", _options.PermissionsPolicyValue);

        // Invalid per scanner — explicitly excluded to pass securityheaders.com checks
        Assert.DoesNotContain("identity-credentials-get", _options.PermissionsPolicyValue);
        Assert.DoesNotContain("otp-credentials", _options.PermissionsPolicyValue);
        Assert.DoesNotContain("publickey-credentials-create", _options.PermissionsPolicyValue);
        Assert.DoesNotContain("window-management", _options.PermissionsPolicyValue);
    }

    // ── Cross-Origin isolation ─────────────────────────────────────────────

    [Fact]
    public void StrictAPlusCrossOriginPoliciesAllEnabled()
    {
        // Assert
        Assert.True(_options.EnableCoep);
        Assert.Equal("require-corp", _options.CoepValue);
        Assert.True(_options.EnableCoop);
        Assert.Equal("same-origin", _options.CoopValue);
        Assert.True(_options.EnableCorp);
        Assert.Equal("same-origin", _options.CorpValue);
    }

    // ── DNS / Cross-domain ─────────────────────────────────────────────────

    [Fact]
    public void StrictAPlusDnsAndCrossDomainDisabled()
    {
        // Assert
        Assert.True(_options.EnableXDnsPrefetchControl);
        Assert.Equal("off", _options.XDnsPrefetchControlValue);
        Assert.True(_options.EnableXPermittedCrossDomainPolicies);
        Assert.Equal("none", _options.XPermittedCrossDomainPoliciesValue);
    }

    // ── Server header ──────────────────────────────────────────────────────

    [Fact]
    public void StrictAPlusServerHeaderRemoved()
    {
        // Assert
        Assert.True(_options.RemoveServerHeader);
    }

    [Fact]
    public void StrictAPlusXPoweredByHeaderRemoved()
    {
        // X-Powered-By removal is enabled in Strict A+ (and inherited by other presets).
        // Default in NetSecureHeadersOptions is false for backward compatibility.
        Assert.True(_options.RemoveXPoweredBy);
    }

    // ── CSP ────────────────────────────────────────────────────────────────

    [Fact]
    public void StrictAPlusCspDefaultSrcIsNone()
    {
        // Assert
        Assert.True(_options.EnableCsp);
        Assert.Equal("'none'", _options.Csp.DefaultSrc);
    }

    [Fact]
    public void StrictAPlusCspScriptSrcUsesNonceAndStrictDynamic()
    {
        // Assert
        Assert.Contains("'nonce-{nonce}'", _options.Csp.ScriptSrc);
        Assert.Contains("'strict-dynamic'", _options.Csp.ScriptSrc);
        Assert.DoesNotContain("'unsafe-inline'", _options.Csp.ScriptSrc);
        Assert.DoesNotContain("'unsafe-eval'", _options.Csp.ScriptSrc);
    }

    [Fact]
    public void StrictAPlusCspStyleSrcUsesNonceOnly()
    {
        // Assert
        Assert.Equal("'nonce-{nonce}'", _options.Csp.StyleSrc);
        Assert.DoesNotContain("'unsafe-inline'", _options.Csp.StyleSrc);
    }

    [Fact]
    public void StrictAPlusCspObjectSrcIsNone()
    {
        // Assert
        Assert.Equal("'none'", _options.Csp.ObjectSrc);
    }

    [Fact]
    public void StrictAPlusCspBaseUriIsNone()
    {
        // Assert
        Assert.Equal("'none'", _options.Csp.BaseUri);
    }

    [Fact]
    public void StrictAPlusCspFrameAncestorsIsNone()
    {
        // Assert
        Assert.Equal("'none'", _options.Csp.FrameAncestors);
    }

    [Fact]
    public void StrictAPlusCspTrustedTypesEnabled()
    {
        // Assert
        Assert.Equal("'script'", _options.Csp.RequireTrustedTypesFor);
        Assert.Equal("'none'", _options.Csp.TrustedTypes);
    }

    [Fact]
    public void StrictAPlusCspUpgradeInsecureRequestsEnabled()
    {
        // Assert
        Assert.True(_options.Csp.EnableUpgradeInsecureRequests);
    }

    [Fact]
    public void StrictAPlusCspBuildProducesValidHeaderValue()
    {
        // Act
        var csp = _options.Csp.Build();

        // Assert — must contain critical directives
        Assert.Contains("default-src 'none'", csp);
        Assert.Contains("script-src 'nonce-{nonce}' 'strict-dynamic'", csp);
        Assert.Contains("style-src 'nonce-{nonce}'", csp);
        Assert.Contains("object-src 'none'", csp);
        Assert.Contains("base-uri 'none'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("form-action 'self'", csp);
        Assert.Contains("upgrade-insecure-requests", csp);
        Assert.Contains("require-trusted-types-for 'script'", csp);
        Assert.Contains("trusted-types 'none'", csp);

        // Must NOT contain unsafe directives
        Assert.DoesNotContain("'unsafe-inline'", csp);
        Assert.DoesNotContain("'unsafe-eval'", csp);
    }

    // ── Preset-specific tests ───────────────────────────────────────────────

    [Fact]
    public void ApiPresetDisablesCspByDefault()
    {
        // Arrange
        var options = SecurePresets.Api();

        // Assert
        Assert.False(options.EnableCsp);
    }

    [Fact]
    public void ApiMinimalPresetDisablesBrowserDocumentHeaders()
    {
        // Arrange
        var options = SecurePresets.ApiMinimal();

        // Assert
        Assert.False(options.EnableCsp);
        Assert.False(options.EnableXFrameOptions);
        Assert.False(options.EnablePermissionsPolicy);
        Assert.False(options.EnableCoep);
        Assert.False(options.EnableCoop);
        Assert.False(options.EnableCorp);
        Assert.False(options.EnableXDnsPrefetchControl);
        Assert.False(options.EnableXPermittedCrossDomainPolicies);
    }

    [Fact]
    public void ApiMinimalPresetKeepsApiRelevantHardening()
    {
        // Arrange
        var options = SecurePresets.ApiMinimal();

        // Assert
        Assert.True(options.EnableHsts);
        Assert.True(options.EnableXContentTypeOptions);
        Assert.True(options.EnableReferrerPolicy);
        Assert.True(options.RemoveServerHeader);
        Assert.True(options.RemoveXPoweredBy);
    }

    [Fact]
    public void ApiPathPresetUsesDefaultApiPrefix()
    {
        // Arrange
        var policy = SecurePresets.ApiPath();

        // Assert
        Assert.Equal("/api", policy.PathPrefix);
        Assert.False(policy.Options.EnableCsp);
        Assert.False(policy.Options.EnableXFrameOptions);
    }

    [Fact]
    public void MvcPresetUsesBalancedReferrerPolicy()
    {
        // Arrange
        var options = SecurePresets.Mvc();

        // Assert
        Assert.Equal("strict-origin-when-cross-origin", options.ReferrerPolicyValue);
    }

    [Fact]
    public void MvcPresetAllowsHttpsImages()
    {
        // Arrange
        var options = SecurePresets.Mvc();

        // Assert
        Assert.Contains("https:", options.Csp.ImgSrc);
    }

    [Fact]
    public void BlazorPresetAllowsBlobWorkers()
    {
        // Arrange
        var options = SecurePresets.Blazor();

        // Assert
        Assert.Contains("blob:", options.Csp.WorkerSrc);
    }

    [Fact]
    public void BlazorPresetAllowsWebSocketConnections()
    {
        // Arrange
        var options = SecurePresets.Blazor();

        // Assert
        Assert.Contains("wss:", options.Csp.ConnectSrc);
    }

    [Fact]
    public void SpaReverseProxyPresetAllowsHttpsAndWebSocketConnections()
    {
        // Arrange
        var options = SecurePresets.SpaReverseProxy();

        // Assert
        Assert.Contains("https:", options.Csp.ConnectSrc);
        Assert.Contains("wss:", options.Csp.ConnectSrc);
    }

    [Fact]
    public void SpaReverseProxyPresetAllowsBlobImages()
    {
        // Arrange
        var options = SecurePresets.SpaReverseProxy();

        // Assert
        Assert.Contains("blob:", options.Csp.ImgSrc);
    }

    [Fact]
    public void SwaggerPresetAllowsUnsafeInlineForStylesAndCdn()
    {
        // Arrange
        var options = SecurePresets.Swagger();

        // Assert
        Assert.Contains("'unsafe-inline'", options.Csp.StyleSrc);
        Assert.Contains("https://cdn.jsdelivr.net", options.Csp.ScriptSrc);
        Assert.Contains("https://cdn.jsdelivr.net", options.Csp.StyleSrc);
        Assert.Equal("strict-origin-when-cross-origin", options.ReferrerPolicyValue);
    }

    [Fact]
    public void SwaggerPresetKeepsStrongBaseHeaders()
    {
        // Arrange
        var options = SecurePresets.Swagger();

        // Assert
        Assert.True(options.EnableHsts);
        Assert.True(options.EnableXContentTypeOptions);
        Assert.True(options.RemoveServerHeader);
    }

    [Fact]
    public void ReverseProxyPresetAllowsHttpsAndWebSocket()
    {
        // Arrange
        var options = SecurePresets.ReverseProxy();

        // Assert
        Assert.Contains("https:", options.Csp.ConnectSrc);
        Assert.Contains("wss:", options.Csp.ConnectSrc);
        Assert.Equal("strict-origin-when-cross-origin", options.ReferrerPolicyValue);
    }

    [Fact]
    public void BlazorWebSocketPresetExplicitlyAllowsWsAndWss()
    {
        // Arrange
        var options = SecurePresets.BlazorWebSocket();

        // Assert
        Assert.Contains("wss:", options.Csp.ConnectSrc);
        Assert.Contains("ws:", options.Csp.ConnectSrc);
    }

    // ── OWASP API preset ──────────────────────────────────────────────────

    [Fact]
    public void OwaspApiPresetKeepsTransportSecurityAndSniffingProtection()
    {
        // Arrange
        var options = SecurePresets.OwaspApi();

        // Assert
        Assert.True(options.EnableHsts);
        Assert.Contains("max-age=63072000", options.HstsValue);
        Assert.True(options.EnableXContentTypeOptions);
        Assert.Equal("nosniff", options.XContentTypeOptionsValue);
        Assert.True(options.EnableReferrerPolicy);
        Assert.Equal("no-referrer", options.ReferrerPolicyValue);
    }

    [Fact]
    public void OwaspApiPresetHidesServerIdentity()
    {
        // Arrange
        var options = SecurePresets.OwaspApi();

        // Assert
        Assert.True(options.RemoveServerHeader);
        Assert.True(options.RemoveXPoweredBy);
    }

    [Fact]
    public void OwaspApiPresetDisablesBrowserDocumentHeaders()
    {
        // Arrange
        var options = SecurePresets.OwaspApi();

        // Assert
        Assert.False(options.EnableCsp);
        Assert.False(options.EnableXFrameOptions);
        Assert.False(options.EnablePermissionsPolicy);
        Assert.False(options.EnableCoep);
        Assert.False(options.EnableCoop);
        Assert.False(options.EnableCorp);
    }

    [Fact]
    public void OwaspApiPresetEnablesCrossDomainPolicyAndDnsPrefetchControl()
    {
        // Arrange
        var options = SecurePresets.OwaspApi();

        // Assert
        Assert.True(options.EnableXPermittedCrossDomainPolicies);
        Assert.Equal("none", options.XPermittedCrossDomainPoliciesValue);
        Assert.True(options.EnableXDnsPrefetchControl);
        Assert.Equal("off", options.XDnsPrefetchControlValue);
    }

    [Fact]
    public void OwaspApiPathPresetUsesDefaultApiPrefix()
    {
        // Arrange
        var policy = SecurePresets.OwaspApiPath();

        // Assert
        Assert.Equal("/api", policy.PathPrefix);
        Assert.True(policy.Options.EnableHsts);
        Assert.True(policy.Options.EnableXContentTypeOptions);
        Assert.False(policy.Options.EnableCsp);
    }

    // ── NSwag preset ──────────────────────────────────────────────────────

    [Fact]
    public void NSwagPresetAllowsUnpkgCdnAndNonceBasedCsp()
    {
        // Arrange
        var options = SecurePresets.NSwag();

        // Assert
        Assert.Contains("https://unpkg.com", options.Csp.ScriptSrc);
        Assert.Contains("https://unpkg.com", options.Csp.StyleSrc);
        Assert.Contains("'nonce-{nonce}'", options.Csp.ScriptSrc);
        Assert.Contains("'strict-dynamic'", options.Csp.ScriptSrc);
        Assert.DoesNotContain("'unsafe-inline'", options.Csp.ScriptSrc);
    }

    [Fact]
    public void NSwagPresetUsesStrictReferrerPolicy()
    {
        // Arrange
        var options = SecurePresets.NSwag();

        // Assert
        Assert.Equal("strict-origin-when-cross-origin", options.ReferrerPolicyValue);
    }

    [Fact]
    public void NSwagPresetKeepsStrongBaseHeaders()
    {
        // Arrange
        var options = SecurePresets.NSwag();

        // Assert
        Assert.True(options.EnableHsts);
        Assert.True(options.EnableXContentTypeOptions);
        Assert.True(options.RemoveServerHeader);
        Assert.True(options.RemoveXPoweredBy);
        Assert.True(options.EnableXFrameOptions);
        Assert.Equal("DENY", options.XFrameOptionsValue);
    }
}
