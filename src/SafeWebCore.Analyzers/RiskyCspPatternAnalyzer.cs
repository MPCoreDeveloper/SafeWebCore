using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SafeWebCore.Analyzers;

/// <summary>
/// Analyzer that detects risky CSP patterns such as:
/// - 'unsafe-inline' without a nonce
/// - overly broad wildcards (e.g. '*' or 'https:')
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RiskyCspPatternAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.UnsafeInlineWithoutNonce,
            DiagnosticDescriptors.BroadCspSource);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Look for string literals that are assigned to CSP directive properties or passed in fluent builders
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInterpolatedString, SyntaxKind.InterpolatedStringExpression);
    }

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var literal = (LiteralExpressionSyntax)context.Node;
        var text = literal.Token.ValueText;

        CheckForRiskyPatterns(context, literal, text);
    }

    private static void AnalyzeInterpolatedString(SyntaxNodeAnalysisContext context)
    {
        var interpolated = (InterpolatedStringExpressionSyntax)context.Node;
        var text = interpolated.ToString();

        CheckForRiskyPatterns(context, interpolated, text);
    }

    private static void CheckForRiskyPatterns(SyntaxNodeAnalysisContext context, SyntaxNode node, string value)
    {
        // Check for unsafe-inline without nonce context
        if (value.Contains("'unsafe-inline'", StringComparison.OrdinalIgnoreCase) &&
            !value.Contains("nonce-", StringComparison.OrdinalIgnoreCase))
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.UnsafeInlineWithoutNonce,
                node.GetLocation());

            context.ReportDiagnostic(diagnostic);
        }

        // Broad wildcard detection
        if (ContainsBroadWildcard(value))
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.BroadCspSource,
                node.GetLocation(),
                value);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool ContainsBroadWildcard(string value)
    {
        // Very broad patterns that are usually dangerous in strict policies
        return value.Contains("*", StringComparison.Ordinal) ||
               value.Contains("'unsafe-eval'", StringComparison.OrdinalIgnoreCase) ||
               (value.Contains("https:", StringComparison.OrdinalIgnoreCase) && 
                !value.Contains("https://", StringComparison.OrdinalIgnoreCase)); // bare https: is very permissive
    }
}
