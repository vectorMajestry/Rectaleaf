using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace Rectaleaf.Tests.Infrastructure;

public sealed class CompilationResult : IDisposable
{
    public CSharpCompilation Compilation { get; }
    public IReadOnlyList<INamedTypeSymbol> RoslynTopLevelTypes { get; }
    public MetadataLoadContext MetadataContext { get; }
    public Assembly ReflectionAssembly { get; }
    public IReadOnlyList<Type> ReflectionTopLevelTypes { get; }

    public CompilationResult(
        CSharpCompilation compilation,
        IReadOnlyList<INamedTypeSymbol> roslynTopLevelTypes,
        MetadataLoadContext metadataContext,
        Assembly reflectionAssembly,
        IReadOnlyList<Type> reflectionTopLevelTypes)
    {
        Compilation = compilation;
        RoslynTopLevelTypes = roslynTopLevelTypes;
        MetadataContext = metadataContext;
        ReflectionAssembly = reflectionAssembly;
        ReflectionTopLevelTypes = reflectionTopLevelTypes;
    }

    public void Dispose()
    {
        MetadataContext.Dispose();
    }
}

public static class CompilationHarness
{
    public static CompilationResult Compile(string sourceCode, string? assemblyName = null)
    {
        assemblyName ??= "CursedCompilation_" + Guid.NewGuid().ToString("N");

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, parseOptions);
        var globalUsingsTree = CSharpSyntaxTree.ParseText(@"
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Runtime.CompilerServices;
global using global::System.Runtime.InteropServices;
", parseOptions);

        var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        var references = trustedAssembliesPaths
            .Select(p => MetadataReference.CreateFromFile(p))
            .Cast<MetadataReference>()
            .ToList();

        var compilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            allowUnsafe: true,
            nullableContextOptions: NullableContextOptions.Enable
        );

        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree, globalUsingsTree },
            references,
            compilationOptions
        );

        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            var errorMsg = string.Join(Environment.NewLine, errors.Select(e => e.ToString()));
            throw new InvalidOperationException($"C# source compilation failed with {errors.Count} error(s):{Environment.NewLine}{errorMsg}");
        }

        // 1. Gather Roslyn top-level named type symbols
        var roslynTypes = new List<INamedTypeSymbol>();
        CollectTopLevelTypes(compilation.Assembly.GlobalNamespace, roslynTypes);

        // 2. Emit IL to memory for Reflection MetadataLoadContext
        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream);
        if (!emitResult.Success)
        {
            var emitErrors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
            throw new InvalidOperationException($"IL Emit failed:{Environment.NewLine}{string.Join(Environment.NewLine, emitErrors)}");
        }

        peStream.Seek(0, SeekOrigin.Begin);

        // 3. Setup MetadataLoadContext
        var resolver = new PathAssemblyResolver(trustedAssembliesPaths);
        var mlc = new MetadataLoadContext(resolver);
        var reflectionAssembly = mlc.LoadFromStream(peStream);
        var reflectionTypes = reflectionAssembly.GetTypes()
            .Where(t => !t.IsNested && !t.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal))
            .ToList();

        return new CompilationResult(compilation, roslynTypes, mlc, reflectionAssembly, reflectionTypes);
    }

    static void CollectTopLevelTypes(INamespaceSymbol ns, List<INamedTypeSymbol> result)
    {
        foreach (var member in ns.GetTypeMembers())
        {
            // Skip compiler-generated implementation types and extension blocks (which are nested in static classes)
            if (!member.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal)
                && member.TypeKind != TypeKind.Extension
                && member.ContainingType is null)
            {
                result.Add(member);
            }
        }

        foreach (var subNs in ns.GetNamespaceMembers())
        {
            CollectTopLevelTypes(subNs, result);
        }
    }
}
