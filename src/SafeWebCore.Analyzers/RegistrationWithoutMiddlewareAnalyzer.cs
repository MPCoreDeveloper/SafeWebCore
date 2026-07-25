using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SafeWebCore.Analyzers;

/// <summary>
/// Analyzer that reports when SafeWebCore registration methods are used
/// but <c>UseNetSecureHeaders()</c> is never called in the application pipeline.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RegistrationWithoutMiddlewareAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> RegistrationMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "AddNetSecureHeaders",
        "AddNetSecureHeadersStrictAPlus",
        "AddNetSecureHeadersFromConfiguration",
        "AddNetSecureHeadersApiPreset",
        "AddNetSecureHeadersMvcPreset",
        "AddNetSecureHeadersBlazorPreset",
        "AddNetSecureHeadersBlazorWebSocketPreset",
        "AddNetSecureHeadersSpaReverseProxyPreset",
        "AddNetSecureHeadersSwagger",
        "AddNetSecureHeadersReverseProxyPreset",
        "AddNetSecureHeadersForEnvironment",
        "AddNetSecureHeadersStrictAPlusForEnvironment");

    private const string UseNetSecureHeadersMethodName = "UseNetSecureHeaders";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.RegistrationWithoutMiddleware);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Thread-safe collection because EnableConcurrentExecution is on.
            var registrations = new ConcurrentBag<InvocationExpressionSyntax>();
            int useNetSecureHeadersFound = 0; // 0 = false, 1 = true (Interlocked)

            compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
            {
                var invocation = (InvocationExpressionSyntax)syntaxContext.Node;
                var methodName = GetInvokedMethodName(invocation);

                if (RegistrationMethodNames.Contains(methodName))
                {
                    registrations.Add(invocation);
                }

                if (string.Equals(methodName, UseNetSecureHeadersMethodName, StringComparison.Ordinal))
                {
                    Interlocked.Exchange(ref useNetSecureHeadersFound, 1);
                }
            }, SyntaxKind.InvocationExpression);

            compilationContext.RegisterCompilationEndAction(endContext =>
            {
                if (!registrations.IsEmpty && useNetSecureHeadersFound == 0)
                {
                    foreach (var registration in registrations)
                    {
                        var methodName = GetInvokedMethodName(registration);
                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.RegistrationWithoutMiddleware,
                            registration.GetLocation(),
                            methodName);

                        endContext.ReportDiagnostic(diagnostic);
                    }
                }
            });
        });
    }

    private static string GetInvokedMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => string.Empty
        };
    }
}
