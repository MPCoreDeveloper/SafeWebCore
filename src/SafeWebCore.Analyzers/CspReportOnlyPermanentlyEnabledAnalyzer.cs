using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SafeWebCore.Analyzers;

/// <summary>
/// Analyzer that warns when <c>UseCspReportOnly = true</c> is set.
/// This is common during rollout but frequently left on permanently.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CspReportOnlyPermanentlyEnabledAnalyzer : DiagnosticAnalyzer
{
    private const string PropertyName = "UseCspReportOnly";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.CspReportOnlyPermanentlyEnabled);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectInitializer, SyntaxKind.ObjectInitializerExpression);
    }

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;

        if (IsUseCspReportOnlyTrue(assignment.Left, assignment.Right))
        {
            Report(context, assignment.GetLocation());
        }
    }

    private static void AnalyzeObjectInitializer(SyntaxNodeAnalysisContext context)
    {
        var initializer = (InitializerExpressionSyntax)context.Node;

        foreach (var expression in initializer.Expressions)
        {
            if (expression is AssignmentExpressionSyntax assignment &&
                IsUseCspReportOnlyTrue(assignment.Left, assignment.Right))
            {
                Report(context, assignment.GetLocation());
            }
        }
    }

    private static bool IsUseCspReportOnlyTrue(ExpressionSyntax left, ExpressionSyntax right)
    {
        if (left is not IdentifierNameSyntax identifier)
            return false;

        if (!string.Equals(identifier.Identifier.ValueText, PropertyName, StringComparison.Ordinal))
            return false;

        // Check if it's being set to the literal 'true'
        return right is LiteralExpressionSyntax literal &&
               literal.IsKind(SyntaxKind.TrueLiteralExpression);
    }

    private static void Report(SyntaxNodeAnalysisContext context, Location location)
    {
        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.CspReportOnlyPermanentlyEnabled,
            location);

        context.ReportDiagnostic(diagnostic);
    }
}
