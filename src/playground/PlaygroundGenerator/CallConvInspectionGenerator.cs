using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PlaygroundGenerator;

[Generator]
public class CallConvInspectionGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor InfoDescriptor = new(
        id: "CC001",
        title: "Calling Conventions Inspection",
        messageFormat: "Inspected function pointer calling conventions: count={0}, types=[{1}], DisplayString='{2}'",
        category: "Inspection",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fieldDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is FieldDeclarationSyntax,
                transform: static (ctx, _) => (FieldDeclarationSyntax)ctx.Node)
            .Where(static f => f is not null);

        var compilationAndFields = context.CompilationProvider.Combine(fieldDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndFields, static (spc, source) =>
        {
            var (compilation, fields) = source;

            foreach (var fieldSyntax in fields)
            {
                var model = compilation.GetSemanticModel(fieldSyntax.SyntaxTree);
                foreach (var variable in fieldSyntax.Declaration.Variables)
                {
                    if (model.GetDeclaredSymbol(variable) is not IFieldSymbol fieldSymbol)
                        continue;

                    if (fieldSymbol.Type is IFunctionPointerTypeSymbol fnPtr)
                    {
                        var types = fnPtr.Signature.UnmanagedCallingConventionTypes;
                        var typeNames = string.Join(", ", types.Select(t => t.Name));
                        var display = fnPtr.ToDisplayString();

                        var diagnostic = Diagnostic.Create(
                            InfoDescriptor,
                            fieldSyntax.GetLocation(),
                            types.Length,
                            typeNames,
                            display);

                        spc.ReportDiagnostic(diagnostic);
                    }
                }
            }
        });
    }
}
