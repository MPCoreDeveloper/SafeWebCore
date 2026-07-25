using Microsoft.CodeAnalysis;

namespace SafeWebCore.Analyzers;

/// <summary>
/// Diagnostic descriptors for SafeWebCore analyzers.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "SafeWebCore";

    /// <summary>
    /// SWC001: Registration methods were called but UseNetSecureHeaders was never invoked.
    /// </summary>
    public static readonly DiagnosticDescriptor RegistrationWithoutMiddleware = new(
        id: "SWC001",
        title: "SafeWebCore middleware is registered but not used",
        messageFormat: "SafeWebCore security headers are registered via '{0}' but 'UseNetSecureHeaders()' is never called in the application pipeline",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Calling AddNetSecureHeaders* registers services, but the middleware must also be added to the request pipeline using UseNetSecureHeaders(). Without it, no security headers will be emitted.",
        helpLinkUri: "https://github.com/MPCoreDeveloper/SafeWebCore/blob/master/docs/getting-started.md",
        customTags: new[] { "CompilationEnd" });

    /// <summary>
    /// SWC002: UseCspReportOnly is set to true (often left permanently, causing no enforcement).
    /// </summary>
    public static readonly DiagnosticDescriptor CspReportOnlyPermanentlyEnabled = new(
        id: "SWC002",
        title: "CSP is configured in report-only mode",
        messageFormat: "CSP is set to report-only mode (UseCspReportOnly = true). This means violations are reported but not blocked. Consider enforcing the policy for production.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Setting UseCspReportOnly to true is useful during rollout, but is frequently left on permanently. This results in CSP never actually protecting the application.",
        helpLinkUri: "https://github.com/MPCoreDeveloper/SafeWebCore/blob/master/docs/advanced-configuration.md#csp-report-only-rollout",
        customTags: new[] { "ReportOnly" });

    /// <summary>
    /// SWC003: 'unsafe-inline' used without a nonce.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsafeInlineWithoutNonce = new(
        id: "SWC003",
        title: "'unsafe-inline' used without nonce",
        messageFormat: "CSP directive contains 'unsafe-inline' without a nonce. This weakens the protection significantly.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Using 'unsafe-inline' without a nonce allows inline scripts/styles to execute without CSP protection.");

    /// <summary>
    /// SWC004: Overly broad CSP source detected (e.g. '*', bare https:, unsafe-eval).
    /// </summary>
    public static readonly DiagnosticDescriptor BroadCspSource = new(
        id: "SWC004",
        title: "Overly broad CSP source detected",
        messageFormat: "Broad or permissive CSP source detected: '{0}'. This may significantly reduce the effectiveness of your policy.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Sources such as '*', 'https:', or 'unsafe-eval' are very permissive and often not recommended in strict policies.");
}
