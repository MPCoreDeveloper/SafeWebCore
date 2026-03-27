using SafeWebCore.Options;
using SafeWebCore.Presets;
using Xunit;

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
}
