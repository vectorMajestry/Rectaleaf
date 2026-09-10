using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Properties;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn;

namespace PlaygroundApp;

public static class TestRoslynBackend
{
    public static void RunTests()
    {
        Console.WriteLine("==================================================");
        Console.WriteLine("  TESTING RECTALEAF ROSLYN BACKEND IMPLEMENTATION");
        Console.WriteLine("==================================================");

        string testSource = @"
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TestNamespace
{
    public class BaseClass
    {
        public virtual void BaseMethod() { }
    }

    public class SampleClass : BaseClass
    {
        public int AutoProp { get; set; }
        public required string RequiredProp { get; init; }
        public int ComputedProp => 42;

        public int this[int index] => index * 2;

        public event Action? SampleEvent;

        public static SampleClass operator +(SampleClass a, SampleClass b) => a;

        public SampleClass() { }
        public SampleClass(int x) { }

        public void MethodWithLocalFunction()
        {
            int LocalHelper(int a) => a + 1;
            int x = LocalHelper(5);
        }
    }

    public abstract class AbstractClass
    {
        public abstract void AbstractMethod();
    }

    public record Person(string FirstName, string LastName)
    {
        public int Age { get; set; }
    }

    public struct Point
    {
        public int X, Y;
        public Point(int x, int y) { X = x; Y = y; }
    }

    public ref struct StackSpan
    {
        public ref int Field;
    }

    public record struct ValuePoint(double X, double Y);

    [InlineArray(10)]
    public struct IntBuffer
    {
        private int _element0;
    }

    public enum ByteEnum : byte
    {
        Zero = 0,
        One = 1
    }

    public interface IWorker
    {
        void DoWork();
        void DefaultWork() => Console.WriteLine(""DIM"");
        static abstract void StaticWork();
    }

    public delegate int NumberProcessor(int input, string label);

    public static class ExtensionContainer
    {
        public static void RegularStaticMethod() { }

        extension(ref Point pt)
        {
            public void Reset() { pt.X = 0; pt.Y = 0; }
        }

        extension(string)
        {
            public static int DefaultLength => 0;
        }
    }
}
";

        var syntaxTree = CSharpSyntaxTree.ParseText(testSource, new CSharpParseOptions(LanguageVersion.Preview));
        var bclPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(bclPath, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(bclPath, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Combine(bclPath, "System.Console.dll")),
        };

        var compilation = CSharpCompilation.Create(
            "TestBackendAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        var diags = compilation.GetDiagnostics();
        var errors = diags.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            Console.WriteLine("Compilation errors in test source:");
            foreach (var e in errors)
                Console.WriteLine("  " + e);
            return;
        }

        Console.WriteLine("[PASS] Test compilation succeeded with 0 errors.\n");

        // 1. Test Classes
        var sampleClassSym = compilation.GetTypeByMetadataName("TestNamespace.SampleClass")!;
        var sampleClass = RectaleafRoslyn.FromSymbol(sampleClassSym) as IRegularClassType;
        Assert(sampleClass is not null, "SampleClass should be IRegularClassType");
        Assert(sampleClass!.BaseType is not null, "SampleClass should have non-null BaseType (BaseClass)");
        Assert(sampleClass.BaseType!.TypeDefinition.LocalName == "BaseClass", "SampleClass BaseType should be BaseClass");

        // 2. Test Fields & AutoProperty Backing Fields
        var fields = sampleClass.GetFields();
        Assert(fields.All(f => !f.Name.Contains("<")), "GetFields() must exclude compiler-generated backing fields");
        var autoProp = sampleClass.GetProperties().First(p => p.Name == "AutoProp") as IRegularProperty<IKnownOrNot, IAccess>;
        Assert(autoProp is not null, "AutoProp should be found");
        Assert(autoProp!.BackingField is not null, "AutoProp should have a non-null BackingField");
        var computedProp = sampleClass.GetProperties().First(p => p.Name == "ComputedProp") as IRegularProperty<IKnownOrNot, IAccess>;
        Assert(computedProp!.BackingField is null, "ComputedProp should have a null BackingField");

        // 3. Test Required Property
        var reqProp = sampleClass.GetProperties().First(p => p.Name == "RequiredProp") as IInstanceProperty<IKnownOrNot, IAccess>;
        Assert(reqProp is not null, "RequiredProp should be found");
        Assert(reqProp is IPropertyRequirement req && req.IsRequired, "RequiredProp should be marked IsRequired");

        // 4. Test Indexer separation from Properties
        Assert(sampleClass.GetProperties().All(p => p.Name != "this" && p.Name != "Item"), "GetProperties() must exclude indexers");
        var indexers = sampleClass.GetIndexers();
        Assert(indexers.Count == 1, "SampleClass should have exactly 1 indexer");

        // 5. Test Events
        var events = sampleClass.GetEvents();
        Assert(events.Count == 1 && events[0].Name == "SampleEvent", "SampleClass should have SampleEvent");

        // 6. Test Operators
        var operators = sampleClass.GetOperators();
        Assert(operators.Count == 1 && operators[0] is IBinaryOperator binOp && binOp.Kind == BinaryOperatorKind.Add, "SampleClass should have operator +");

        // 7. Test Local Functions
        var methodWithLocal = sampleClass.GetMethods().First(m => m.Name == "MethodWithLocalFunction");
        var localFunctions = methodWithLocal.GetLocalFunctions();
        Assert(localFunctions.Count == 1 && localFunctions[0].Name == "LocalHelper", "MethodWithLocalFunction should have local function 'LocalHelper'");
        Assert(localFunctions[0].Ordinal == 0, "LocalHelper should have Ordinal 0");


        // 8. Test Abstract Class
        var absClassSym = compilation.GetTypeByMetadataName("TestNamespace.AbstractClass")!;
        var absClass = RectaleafRoslyn.FromSymbol(absClassSym) as IAbstractClassType;
        Assert(absClass is not null, "AbstractClass should be IAbstractClassType");

        // 9. Test Record Class
        var recordSym = compilation.GetTypeByMetadataName("TestNamespace.Person")!;
        var recordClass = RectaleafRoslyn.FromSymbol(recordSym) as IRecordClassType;
        Assert(recordClass is not null, "Person should be IRecordClassType");
        Assert(recordClass!.BaseType is null, "Person inherits directly from object, BaseType should be null");
        var recMethods = recordClass.GetMethods();
        Assert(recMethods.All(m => m.Name != "Equals" && m.Name != "GetHashCode" && m.Name != "ToString" && m.Name != "<Clone>$"),
            "Record methods must exclude synthesized boilerplate Equals, GetHashCode, ToString, <Clone>$");
        Assert(recMethods.Any(m => m.Name == "PrintMembers"), "Record methods must include PrintMembers");
        Assert(recMethods.Any(m => m.Name == "Deconstruct"), "Record methods must include Deconstruct");
        var recDirectIfaces = recordClass.GetDirectInterfaces();
        Assert(recDirectIfaces.Any(i => i.TypeDefinition.LocalName == "IEquatable"), "Record must include direct IEquatable<Person>");

        // 10. Test Structs
        var ptSym = compilation.GetTypeByMetadataName("TestNamespace.Point")!;
        var ptStruct = RectaleafRoslyn.FromSymbol(ptSym) as IStructType;
        Assert(ptStruct is not null, "Point should be IStructType");

        var spanSym = compilation.GetTypeByMetadataName("TestNamespace.StackSpan")!;
        var refStruct = RectaleafRoslyn.FromSymbol(spanSym) as IRefStructType;
        Assert(refStruct is not null, "StackSpan should be IRefStructType");
        var refField = refStruct!.GetFields().First(f => f.Name == "Field");
        Assert(refField is IRefField<IKnownOrNot, IMutOrNot>, "Field in StackSpan should be IRefField");

        var recStructSym = compilation.GetTypeByMetadataName("TestNamespace.ValuePoint")!;
        var recStruct = RectaleafRoslyn.FromSymbol(recStructSym) as IRecordStructType;
        Assert(recStruct is not null, "ValuePoint should be IRecordStructType");

        var inlineArrSym = compilation.GetTypeByMetadataName("TestNamespace.IntBuffer")!;
        var inlineArr = RectaleafRoslyn.FromSymbol(inlineArrSym) as IInlineArrayType;
        Assert(inlineArr is not null, "IntBuffer should be IInlineArrayType");
        Assert(inlineArr!.Length == 10, "IntBuffer Length should be 10");
        Assert(inlineArr.ElementType.LocalName == "int", "IntBuffer ElementType should be int");
        Assert(inlineArr is not IHasFields, "IInlineArrayType must NOT implement IHasFields");

        // 11. Test Enums
        var enumSym = compilation.GetTypeByMetadataName("TestNamespace.ByteEnum")!;
        var byteEnum = RectaleafRoslyn.FromSymbol(enumSym) as IEnumType<byte>;
        Assert(byteEnum is not null, "ByteEnum should be IEnumType<byte>");
        Assert(byteEnum!.Members.Count == 2, "ByteEnum should have 2 members");
        Assert(byteEnum.Members[0].Value == (byte)0 && byteEnum.Members[1].Value == (byte)1, "ByteEnum values should match");

        // 12. Test Interfaces & DIM
        var workerSym = compilation.GetTypeByMetadataName("TestNamespace.IWorker")!;
        var workerIface = RectaleafRoslyn.FromSymbol(workerSym) as IInterfaceType;
        Assert(workerIface is not null, "IWorker should be IInterfaceType");
        var workerMethods = workerIface!.GetMethods();
        Assert(workerMethods.Count == 3, "IWorker should have 3 methods (DoWork, DefaultWork, StaticWork)");

        // 13. Test Delegates
        var delSym = compilation.GetTypeByMetadataName("TestNamespace.NumberProcessor")!;
        var delType = RectaleafRoslyn.FromSymbol(delSym) as IDelegateType;
        Assert(delType is not null, "NumberProcessor should be IDelegateType");
        Assert(delType!.ReturnInfo.Type.LocalName == "int", "NumberProcessor ReturnInfo should be int");
        Assert(delType.GetParameters().Count == 2, "NumberProcessor should have 2 parameters");

        // 14. Test Extension Blocks
        var extClassSym = compilation.GetTypeByMetadataName("TestNamespace.ExtensionContainer")!;
        var extClass = RectaleafRoslyn.FromSymbol(extClassSym) as IStaticClassType;
        Assert(extClass is not null, "ExtensionContainer should be IStaticClassType");
        var extMethods = extClass!.GetMethods();
        Assert(extMethods.Count == 1 && extMethods[0].Name == "RegularStaticMethod",
            "StaticClass.GetMethods() must ONLY contain RegularStaticMethod and filter out lowered extension methods");
        var extBlocks = extClass.GetExtensionsBlocks();
        Assert(extBlocks.Count == 2, "ExtensionContainer should have 2 extension blocks");
        var instBlock = extBlocks.OfType<IInstanceExtensionsBlock>().FirstOrDefault();
        Assert(instBlock is not null, "Should have an IInstanceExtensionsBlock");
        Assert(instBlock!.Receiver.Name == "pt", "Named receiver should be 'pt'");
        Assert(instBlock.Receiver.Type.LocalName == "Point", "Receiver type should be Point");
        var statBlock = extBlocks.OfType<IStaticExtensionsBlock>().FirstOrDefault();
        Assert(statBlock is not null, "Should have an IStaticExtensionsBlock");
        Assert(statBlock!.ExtendedType.LocalName == "string", "Static block extended type should be 'string'");

        // 15. Test Equals() and Equality Comparers
        Console.WriteLine("\nTesting Equals() propagation:");

        // DeclaredType equality
        var sampleClass2 = RectaleafRoslyn.FromSymbol(sampleClassSym);
        Assert(sampleClass.Equals(sampleClass2), "DeclaredType.Equals() should return true for same type symbol");
        Assert(sampleClass.GetHashCode() == sampleClass2.GetHashCode(), "DeclaredType.GetHashCode() should match");

        // Method equality
        var m1 = sampleClass.GetMethods().First(m => m.Name == "MethodWithLocalFunction");
        var m2 = ((IRegularClassType)sampleClass2).GetMethods().First(m => m.Name == "MethodWithLocalFunction");
        Assert(m1.Equals(m2), "Method.Equals() should return true for matching method");
        Assert(m1.GetHashCode() == m2.GetHashCode(), "Method.GetHashCode() should match");

        // LocalFunction equality
        var lf1 = m1.GetLocalFunctions()[0];
        var lf2 = m2.GetLocalFunctions()[0];
        Assert(lf1.Equals(lf2), "LocalFunction.Equals() should return true for matching local function");
        Assert(lf1.GetHashCode() == lf2.GetHashCode(), "LocalFunction.GetHashCode() should match");

        // Parameter equality
        var ctor1 = sampleClass.GetInstanceConstructors().First(c => c.GetParameters().Count == 1);
        var ctor2 = ((IRegularClassType)sampleClass2).GetInstanceConstructors().First(c => c.GetParameters().Count == 1);
        Assert(ctor1.GetParameters()[0].Equals(ctor2.GetParameters()[0]), "Parameter.Equals() should return true");

        // Types without equality comparers must throw NotImplementedException
        bool threwField = false;
        try { autoProp.BackingField!.Equals(autoProp.BackingField); }
        catch (NotImplementedException) { threwField = true; }
        Assert(threwField, "Field.Equals() must throw NotImplementedException");

        bool threwProp = false;
        try { autoProp.Equals(autoProp); }
        catch (NotImplementedException) { threwProp = true; }
        Assert(threwProp, "Property.Equals() must throw NotImplementedException");

        bool threwExtBlock = false;
        try { instBlock.Equals(instBlock); }
        catch (NotImplementedException) { threwExtBlock = true; }
        Assert(threwExtBlock, "ExtensionBlock.Equals() must throw NotImplementedException");

        bool threwCtor = false;
        try { ctor1.Equals(ctor2); }
        catch (NotImplementedException) { threwCtor = true; }
        Assert(threwCtor, "Constructor.Equals() must throw NotImplementedException");

        bool threwOp = false;
        try { operators[0].Equals(operators[0]); }
        catch (NotImplementedException) { threwOp = true; }
        Assert(threwOp, "Operator.Equals() must throw NotImplementedException");

        // 16. Test RectaleafRoslyn Direct Factory Entry Points
        Console.WriteLine("\nTesting RectaleafRoslyn direct factory methods:");
        var methodSym = sampleClassSym.GetMembers().OfType<IMethodSymbol>().First(m => m.Name == "MethodWithLocalFunction");
        var directMethod = RectaleafRoslyn.FromMethod(methodSym);
        Assert(directMethod.Name == "MethodWithLocalFunction", "FromMethod should work");

        var fieldSym = ptSym.GetMembers().OfType<IFieldSymbol>().First(f => f.Name == "X");
        var directField = RectaleafRoslyn.FromField(fieldSym);
        Assert(directField.Name == "X", "FromField should work");

        var propSym = sampleClassSym.GetMembers().OfType<IPropertySymbol>().First(p => p.Name == "AutoProp");
        var directProp = RectaleafRoslyn.FromProperty(propSym);
        Assert(directProp.Name == "AutoProp", "FromProperty should work");

        var indexerSym = sampleClassSym.GetMembers().OfType<IPropertySymbol>().First(p => p.IsIndexer);
        var directIndexer = RectaleafRoslyn.FromIndexer(indexerSym);
        Assert(directIndexer.GetParameters().Count == 1, "FromIndexer should work");

        var eventSym = sampleClassSym.GetMembers().OfType<IEventSymbol>().First(e => e.Name == "SampleEvent");
        var directEvent = RectaleafRoslyn.FromEvent(eventSym);
        Assert(directEvent.Name == "SampleEvent", "FromEvent should work");

        var paramSym = sampleClassSym.Constructors[1].Parameters[0];
        var directParam = RectaleafRoslyn.FromParameter(paramSym);
        Assert(directParam.Name == paramSym.Name, "FromParameter should work");

        var genSym = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Int32));
        var directUsage = RectaleafRoslyn.FromTypeUsage(genSym);
        Assert(directUsage is IArrayType<IKnownOrNot, IFlat>, "FromTypeUsage should create linear array type");

        var ctorSym = sampleClassSym.Constructors.First(c => !c.IsStatic);
        var directCtor = RectaleafRoslyn.FromConstructor(ctorSym);
        Assert(directCtor is not null, "FromConstructor should work");

        var opSym = sampleClassSym.GetMembers().OfType<IMethodSymbol>().First(m => m.MethodKind == MethodKind.UserDefinedOperator);
        var directOp = RectaleafRoslyn.FromOperator(opSym);
        Assert(directOp is not null, "FromOperator should work");

        Console.WriteLine("\n==================================================");
        Console.WriteLine("  ALL ROSLYN BACKEND TESTS PASSED SUCCESSFULLY!   ");
        Console.WriteLine("==================================================");
    }

    static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {message}");
            Console.ResetColor();
            throw new Exception($"Assertion failed: {message}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[PASS] {message}");
            Console.ResetColor();
        }
    }
}
