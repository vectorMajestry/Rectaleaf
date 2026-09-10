using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace PlaygroundApp;

public static class TestEmit
{
    public static void Test()
    {
    }

    public static void InspectLayout()
    {
        var code = """
            using System.Runtime.InteropServices;
            public struct S1 { public int A; public int B; }
            [StructLayout(LayoutKind.Sequential, Size = 64)]
            public struct S2 { public int A; }
            public class C1 { public int A; }
            public enum E1 : byte { A }
            """;
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
            code,
            new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview));
        var comp = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            "TestLayout",
            new[] { syntaxTree },
            new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        foreach (var name in new[] { "S1", "S2", "C1", "E1" })
        {
            var sym = comp.GetTypeByMetadataName(name)!;
            Console.WriteLine($"\n--- {name} ---");
            foreach (var prop in typeof(Microsoft.CodeAnalysis.INamedTypeSymbol).GetProperties())
            {
                try
                {
                    if (prop.GetIndexParameters().Length == 0)
                    {
                        var val = prop.GetValue(sym);
                        if (prop.Name.Contains("Size", StringComparison.OrdinalIgnoreCase) || prop.Name.Contains("Layout", StringComparison.OrdinalIgnoreCase) || prop.Name.Contains("Kind", StringComparison.OrdinalIgnoreCase))
                            Console.WriteLine($"  {prop.Name}: {val}");
                    }
                }
                catch { }
            }
        }
    }

    public static void InspectCursedCases()
    {
        var code = """
            using System;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            public class Outer<T>
            {
                public class Inner<U>
                {
                    public void Method<V>(T t, U u, V v) { }
                }
            }

            public class CursedGenerics
            {
                public (int, int, int, int, int, int, int, int, int) BigTuple;
                public (int A, int B, int C, int D, int E, int F, int G, int H, int I) BigNamedTuple;

                public unsafe delegate* unmanaged[Cdecl, SuppressGCTransition]<int*, ref int, void> FnPtr;

                public void Mutual<T, U>(T t, U u)
                    where T : IComparable<U>
                    where U : IEnumerable<T>, allows ref struct
                {
                }

                public ref readonly int RefProp => ref s_field;
                private static int s_field = 42;

                public int this[[System.Diagnostics.CodeAnalysis.NotNull] int index, in string key]
                {
                    get => 0;
                    init { }
                }

                public static CursedGenerics operator +(CursedGenerics a, CursedGenerics b) => a;
                public static CursedGenerics operator checked +(CursedGenerics a, CursedGenerics b) => a;
                public static explicit operator int(CursedGenerics a) => 0;
                public static explicit operator checked int(CursedGenerics a) => 0;
                public static bool operator true(CursedGenerics a) => true;
                public static bool operator false(CursedGenerics a) => false;
            }

            public record struct PointStruct(int X, int Y);
            public record class PointClass(int X, int Y);
            """;

        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
            code,
            new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview));

        var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(System.IO.Path.PathSeparator)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(p))
            .Cast<Microsoft.CodeAnalysis.MetadataReference>()
            .ToList();

        var comp = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            "CursedCompilation",
            new[] { syntaxTree },
            trustedAssembliesPaths,
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
                Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                nullableContextOptions: Microsoft.CodeAnalysis.NullableContextOptions.Enable));

        var diags = comp.GetDiagnostics().Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToList();
        if (diags.Count > 0)
        {
            Console.WriteLine("Cursed compilation errors:");
            foreach (var d in diags) Console.WriteLine("  " + d);
            return;
        }

        // 1. Nested generic types
        var outerSym = comp.GetTypeByMetadataName("Outer`1")!;
        var innerSym = outerSym.GetTypeMembers("Inner").First();
        Console.WriteLine($"Outer<T> TypeParameters: {string.Join(", ", outerSym.TypeParameters.Select(p => p.Name))}");
        Console.WriteLine($"Inner<U> TypeParameters: {string.Join(", ", innerSym.TypeParameters.Select(p => p.Name))}");
        Console.WriteLine($"Inner<U> ContainingType: {innerSym.ContainingType?.Name}");

        // 2. Big tuple
        var cursedSym = comp.GetTypeByMetadataName("CursedGenerics")!;
        var bigTupleField = (Microsoft.CodeAnalysis.IFieldSymbol)cursedSym.GetMembers("BigTuple").First();
        var bigTupleType = (Microsoft.CodeAnalysis.INamedTypeSymbol)bigTupleField.Type;
        Console.WriteLine($"BigTuple IsTupleType: {bigTupleType.IsTupleType}");
        Console.WriteLine($"BigTuple TupleElements count: {bigTupleType.TupleElements.Length}");
        Console.WriteLine($"BigTuple Elements: {string.Join(", ", bigTupleType.TupleElements.Select(e => $"{e.Type.Name} {e.Name} (implicit={e.IsImplicitlyDeclared})"))}");

        var bigNamedTupleField = (Microsoft.CodeAnalysis.IFieldSymbol)cursedSym.GetMembers("BigNamedTuple").First();
        var bigNamedTupleType = (Microsoft.CodeAnalysis.INamedTypeSymbol)bigNamedTupleField.Type;
        Console.WriteLine($"BigNamedTuple Elements: {string.Join(", ", bigNamedTupleType.TupleElements.Select(e => $"{e.Type.Name} {e.Name} (implicit={e.IsImplicitlyDeclared})"))}");

        // 3. FnPtr
        var fnPtrField = (Microsoft.CodeAnalysis.IFieldSymbol)cursedSym.GetMembers("FnPtr").First();
        var fnPtr = (Microsoft.CodeAnalysis.IFunctionPointerTypeSymbol)fnPtrField.Type;
        Console.WriteLine($"FnPtr CallingConvention: {fnPtr.Signature.CallingConvention}");
        Console.WriteLine($"FnPtr UnmanagedCallingConventionTypes: {string.Join(", ", fnPtr.Signature.UnmanagedCallingConventionTypes.Select(t => t.Name))}");
        Console.WriteLine($"FnPtr ReturnType: {fnPtr.Signature.ReturnType}");
        Console.WriteLine($"FnPtr Parameters: {string.Join(", ", fnPtr.Signature.Parameters.Select(p => $"{p.RefKind} {p.Type}"))}");

        // 4. Mutual constraints
        var mutualMethod = (Microsoft.CodeAnalysis.IMethodSymbol)cursedSym.GetMembers("Mutual").First();
        foreach (var tp in mutualMethod.TypeParameters)
        {
            Console.WriteLine($"Method TypeParam {tp.Name}: ConstraintTypes=[{string.Join(", ", tp.ConstraintTypes.Select(c => c.ToDisplayString()))}], AllowsRefLike={tp.AllowsRefLikeType}");
        }

        // 5. Operators
        foreach (var op in cursedSym.GetMembers().OfType<Microsoft.CodeAnalysis.IMethodSymbol>().Where(m => m.MethodKind == Microsoft.CodeAnalysis.MethodKind.UserDefinedOperator || m.MethodKind == Microsoft.CodeAnalysis.MethodKind.Conversion))
        {
            Console.WriteLine($"Operator: Name='{op.Name}', MethodKind={op.MethodKind}, ReturnType={op.ReturnType}, Params=[{string.Join(", ", op.Parameters.Select(p => $"{p.Type} {p.Name}"))}]");
        }

        // 6. Record structs vs Record classes constructors
        var pointStruct = comp.GetTypeByMetadataName("PointStruct")!;
        Console.WriteLine($"PointStruct constructors: {pointStruct.Constructors.Length}");
        foreach (var ctor in pointStruct.Constructors)
        {
            Console.WriteLine($"  PointStruct ctor: Params={ctor.Parameters.Length}, Implicit={ctor.IsImplicitlyDeclared}");
        }

        // 7. Fixed buffer & Primary Ctor & Tuple details
        var code2 = """
            public unsafe struct FixedStruct
            {
                public fixed int Buffer[16];
                public fixed byte Bytes[32];
            }

            public class ClassPrimary(int a, string b)
            {
                public ClassPrimary() : this(0, "") { }
            }
            """;
        var tree2 = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
            code2,
            new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview));
        var comp2 = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            "Test2",
            new[] { tree2 },
            trustedAssembliesPaths,
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
                Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true));

        var fixedStruct = comp2.GetTypeByMetadataName("FixedStruct")!;
        foreach (var f in fixedStruct.GetMembers().OfType<Microsoft.CodeAnalysis.IFieldSymbol>())
        {
            Console.WriteLine($"Fixed field: '{f.Name}', IsFixedSizeBuffer={f.IsFixedSizeBuffer}, Type={f.Type}, FixedSize={f.FixedSize}");
            if (f.Type is Microsoft.CodeAnalysis.IPointerTypeSymbol pts)
                Console.WriteLine($"  PointedAtType: {pts.PointedAtType}");
        }

        var classPrim = comp2.GetTypeByMetadataName("ClassPrimary")!;
        foreach (var ctor in classPrim.Constructors)
        {
            // Check properties of ctor
            Console.WriteLine($"ClassPrimary ctor: Params={ctor.Parameters.Length}, Implicit={ctor.IsImplicitlyDeclared}");
            foreach (var p in ctor.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
            {
                if (p.Name.Contains("Primary", StringComparison.OrdinalIgnoreCase))
                    Console.WriteLine($"  Ctor prop {p.Name} = {p.GetValue(ctor)}");
            }
            var syn = ctor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            Console.WriteLine($"  Ctor syntax: {syn?.GetType().Name}");
        }

        // Check bigTupleType underlying representation
        Console.WriteLine($"BigTuple NamedTypeSymbol: Name='{bigTupleType.Name}', MetadataName='{bigTupleType.MetadataName}'");
        Console.WriteLine($"BigTuple TypeArguments: {string.Join(", ", bigTupleType.TypeArguments.Select(t => t.Name))}");
        Console.WriteLine($"BigTuple TypeArguments count: {bigTupleType.TypeArguments.Length}");
        if (bigTupleType.TypeArguments.Length == 8)
        {
            var rest = bigTupleType.TypeArguments[7];
            Console.WriteLine($"  Rest type: {rest.GetType().Name}, Name='{rest.Name}', IsTuple={(rest is Microsoft.CodeAnalysis.INamedTypeSymbol nt && nt.IsTupleType)}");
        }
    }

    public static void InspectDirectInterfaces()
    {
        var code = """
            public interface IParent { }
            public interface IChild : IParent { }
            public class A : IChild { }
            public class B : A { }
            public class C : A, IChild { }
            """;
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
            code,
            new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview));
        var comp = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            "Test",
            new[] { syntaxTree },
            new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var aSym = comp.Assembly.GlobalNamespace.GetTypeMembers().First(t => t.Name == "A");
        var bSym = comp.Assembly.GlobalNamespace.GetTypeMembers().First(t => t.Name == "B");
        var cSym = comp.Assembly.GlobalNamespace.GetTypeMembers().First(t => t.Name == "C");
        Console.WriteLine($"A.Interfaces: {string.Join(", ", aSym.Interfaces.Select(i => i.Name))}");
        Console.WriteLine($"A.AllInterfaces: {string.Join(", ", aSym.AllInterfaces.Select(i => i.Name))}");
        Console.WriteLine($"B.Interfaces: {string.Join(", ", bSym.Interfaces.Select(i => i.Name))}");
        Console.WriteLine($"C.Interfaces: {string.Join(", ", cSym.Interfaces.Select(i => i.Name))}");
    }

    public static void InspectFileAccessibility()
    {
        var code = "file class FileClass { }";
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
            code,
            new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview));
        var comp = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            "Test",
            new[] { syntaxTree },
            new[] { Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var sym = comp.Assembly.GlobalNamespace.GetTypeMembers().First(t => t.Name.Contains("FileClass"));
        var syntaxNode = (Microsoft.CodeAnalysis.CSharp.Syntax.BaseTypeDeclarationSyntax)sym.DeclaringSyntaxReferences[0].GetSyntax();
        Console.WriteLine($"FileClass: Name='{sym.Name}', MetadataName='{sym.MetadataName}', Modifiers='{syntaxNode.Modifiers}'");
    }

    public static void InspectRoslynSymbols()
    {
        var code = """
            using System;
            using System.Collections.Generic;

            public struct Point<T> { public T X, Y; }

            public static class SampleExtensions
            {
                extension<T>(ref Point<T> pt) where T : struct
                {
                    public void Reset() { pt.X = default; pt.Y = default; }
                }

                extension<T>(List<T> list) where T : class
                {
                    public T? FirstOrNull => list.Count > 0 ? list[0] : null;
                }

                extension(string)
                {
                    public static string Greeting => "Hello";
                }

                public static void RegularStaticMethod() { }
            }

            public record Person(string Name, int Age)
            {
                public void CustomMethod() { }
            }

            public class ClassWithPrimaryCtor(int x)
            {
                public int AutoProp { get; set; }
                public int ComputedProp => x;
            }

            public class GenericConstraints<T1, T2, T3, T4, T5, T6>
                where T1 : class
                where T2 : class?
                where T3 : struct
                where T4 : unmanaged
                where T5 : new()
                where T6 : allows ref struct
            {
            }
            """;

        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
            code,
            new Microsoft.CodeAnalysis.CSharp.CSharpParseOptions(Microsoft.CodeAnalysis.CSharp.LanguageVersion.Preview));

        var trustedAssembliesPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(System.IO.Path.PathSeparator)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(p))
            .Cast<Microsoft.CodeAnalysis.MetadataReference>()
            .ToList();

        var comp = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            trustedAssembliesPaths,
            new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
                Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true,
                nullableContextOptions: Microsoft.CodeAnalysis.NullableContextOptions.Enable));

        var diags = comp.GetDiagnostics().Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToList();
        if (diags.Count > 0)
        {
            Console.WriteLine("Compilation errors:");
            foreach (var d in diags) Console.WriteLine("  " + d);
            return;
        }

        var sampleExtSym = comp.GetTypeByMetadataName("SampleExtensions")!;
        Console.WriteLine($"SampleExtensions members count: {sampleExtSym.GetMembers().Length}");
        foreach (var m in sampleExtSym.GetMembers())
        {
            Console.WriteLine($"  Member: {m.Name} ({m.Kind}, MethodKind={(m is Microsoft.CodeAnalysis.IMethodSymbol ms ? ms.MethodKind.ToString() : "N/A")})");
        }
        Console.WriteLine($"SampleExtensions type members (nested): {sampleExtSym.GetTypeMembers().Length}");
        foreach (var tm in sampleExtSym.GetTypeMembers())
        {
            Console.WriteLine($"  Nested Type: '{tm.Name}', Kind={tm.TypeKind}");
            var synRef = tm.DeclaringSyntaxReferences.FirstOrDefault();
            if (synRef != null)
            {
                var sNode = synRef.GetSyntax();
                Console.WriteLine($"    Syntax: {sNode.GetType().FullName} => {sNode.ToString().Split('{')[0].Trim()}");
                foreach (var p in sNode.GetType().GetProperties())
                {
                    try
                    {
                        var v = p.GetValue(sNode);
                        if (v != null && (p.Name.Contains("Parameter") || p.Name.Contains("Type") || p.Name.Contains("Receiver") || p.Name.Contains("Constraint")))
                            Console.WriteLine($"      Syntax.{p.Name} = {v}");
                    }
                    catch { }
                }
            }
            foreach (var prop in tm.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
            {
                if (prop.Name.Contains("Receiver", StringComparison.OrdinalIgnoreCase) || prop.Name.Contains("Extension", StringComparison.OrdinalIgnoreCase))
                    Console.WriteLine($"    Prop: {prop.Name} ({prop.PropertyType.Name})");
            }
            foreach (var m in tm.GetMembers())
            {
                Console.WriteLine($"    Nested Member: {m.Name} ({m.Kind}, isStatic={m.IsStatic})");
            }
        }

        // Test syntax of SampleExtensions
        var syntaxRef = sampleExtSym.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxRef != null)
        {
            var node = syntaxRef.GetSyntax();
            Console.WriteLine($"Syntax Node Type: {node.GetType().FullName}");
            var childNodes = node.ChildNodes().Select(n => n.GetType().Name);
            Console.WriteLine($"Syntax Children: {string.Join(", ", childNodes)}");
        }

        // Check Person record
        var personSym = comp.GetTypeByMetadataName("Person")!;
        Console.WriteLine("\nPerson record members:");
        foreach (var m in personSym.GetMembers())
        {
            Console.WriteLine($"  {m.Name} (Kind: {m.Kind}, IsImplicitlyDeclared: {m.IsImplicitlyDeclared})");
        }

        // Check AutoProp backing field
        var classWithCtor = comp.GetTypeByMetadataName("ClassWithPrimaryCtor")!;
        Console.WriteLine("\nClassWithPrimaryCtor members:");
        foreach (var m in classWithCtor.GetMembers())
        {
            if (m is Microsoft.CodeAnalysis.IFieldSymbol f)
                Console.WriteLine($"  Field: {f.Name} (AssociatedSymbol: {f.AssociatedSymbol?.Name}, IsImplicitlyDeclared: {f.IsImplicitlyDeclared})");
            if (m is Microsoft.CodeAnalysis.IMethodSymbol ctor && ctor.MethodKind == Microsoft.CodeAnalysis.MethodKind.Constructor)
                Console.WriteLine($"  Ctor: {ctor.Name}, Params={ctor.Parameters.Length}, IsImplicitlyDeclared={ctor.IsImplicitlyDeclared}");
        }

        // Check GenericConstraints
        var genType = comp.GetTypeByMetadataName("GenericConstraints`6")!;
        Console.WriteLine("\nGenericConstraints type parameters:");
        foreach (var tp in genType.TypeParameters)
        {
            Console.WriteLine($"  {tp.Name}: HasRef={tp.HasReferenceTypeConstraint}, RefNullAnn={tp.ReferenceTypeConstraintNullableAnnotation}, HasVal={tp.HasValueTypeConstraint}, HasUnman={tp.HasUnmanagedTypeConstraint}, HasCtor={tp.HasConstructorConstraint}, AllowsRefLike={tp.AllowsRefLikeType}");
        }
    }
}
