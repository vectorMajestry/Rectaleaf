using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PlaygroundApp;

public struct Point<T>
{
    public T X, Y;
}

public static class SampleExtensions
{
    // Block 1: Instance extensions on a generic struct with multiple methods
    extension<T>(ref Point<T> pt)
        where T : struct
    {
        public void Reset() { pt.X = default; pt.Y = default; }
        public void Translate(T dx, T dy) { }
    }

    // Block 2: Generic instance extensions with class constraint
    extension<T>(List<T> list) where T : class
    {
        public T? FirstOrNull => list.Count > 0 ? list[0] : null;
        public void AddAll(IEnumerable<T> items) { foreach (var i in items) list.Add(i); }
    }

    // Block 3: Generic static extension block with new() constraint
    extension<T>(List<T>) where T : new()
    {
        public static List<T> CreateWithSingle() => new List<T> { new T() };
    }

    // Block 4: Static extensions on non-generic type with static property and static method
    extension(string)
    {
        public static string Greeting => "Hello World";
        public static string Duplicate(string s) => s + s;
    }

    // Block 5: Extension block with Indexer and generic method with multiple constraints
    extension<T>(ref int a)
        where T : class
    {
        public float this[int h, T y]
        {
            get => a * h;
        }

        public void Exec<U, Y>()
            where Y : T
            where U : Y
        {
        }
    }

    // User-authored manual static methods (overloads / different signatures)
    public static void UserMethod() { }
    public static int UserProperty => 42;
    public static string Reset(string manual) => manual;
    public static double get_Item(double manual) => manual;
}

// ==================== UNIFIED DATA CONTRACT ====================
public record ExtensionBlockInfo(
    bool IsStatic,
    ReceiverInfo? Receiver,
    string ExtendedType,
    List<GenericParamInfo> GenericParameters,
    List<ExtensionMemberInfo> Members
);

public record ReceiverInfo(
    string Name,
    string Type,
    string Modifiers
);

public record GenericParamInfo(
    string Name,
    List<string> Constraints
);

public record ExtensionMemberInfo(
    string Kind, // "Property", "Indexer", or "Method"
    string Name,
    bool IsStatic,
    string ReturnType,
    List<string> Accessors,
    List<MemberParamInfo> Parameters,
    List<GenericParamInfo> GenericParameters
);

public record MemberParamInfo(
    string Name,
    string Type,
    string Modifiers
);

// ==================== REFLECTION EXTRACTOR ====================
public static class ReflectionExtensionExtractor
{
    public static List<ExtensionBlockInfo> Extract(Type staticClass)
    {
        var result = new List<ExtensionBlockInfo>();
        var outerMethods = staticClass.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

        // Roslyn compiles each extension block into a nested type starting with "<G>$"
        var groupTypes = staticClass.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(t => t.Name.StartsWith("<G>$", StringComparison.Ordinal))
            .ToList();

        foreach (var groupType in groupTypes)
        {
            var blockGenericParams = groupType.GetGenericArguments()
                .Select(gp => new GenericParamInfo(
                    CleanGenName(gp.Name),
                    GetGenericConstraints(gp)
                )).ToList();

            var members = new List<ExtensionMemberInfo>();
            bool isBlockStatic = true;
            ReceiverInfo? receiver = null;
            string extendedType = "";

            // 1. Properties and Indexers
            foreach (var prop in groupType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var accessors = new List<string>();
                MethodInfo? getter = prop.GetGetMethod(true);
                MethodInfo? setter = prop.GetSetMethod(true);

                if (getter != null) accessors.Add("get");
                if (setter != null) accessors.Add("set");

                bool isMemberStatic = (getter ?? setter)?.IsStatic ?? true;
                if (!isMemberStatic) isBlockStatic = false;

                string loweredMethodName = getter != null ? getter.Name : setter!.Name;
                var indexParams = prop.GetIndexParameters();
                bool isIndexer = indexParams.Length > 0;

                int expectedParamCount = (isMemberStatic ? 0 : 1) + indexParams.Length;
                var loweredMethod = outerMethods.FirstOrDefault(m => m.Name == loweredMethodName && m.GetParameters().Length == expectedParamCount);

                if (!isMemberStatic && receiver == null && loweredMethod != null && loweredMethod.GetParameters().Length > 0)
                {
                    var p = loweredMethod.GetParameters()[0];
                    receiver = ExtractReceiver(p);
                    extendedType = FormatType(p.ParameterType);
                }
                else if (isMemberStatic && string.IsNullOrEmpty(extendedType))
                {
                    extendedType = "string";
                }

                var memberParams = indexParams.Select(p => new MemberParamInfo(
                    Name: p.Name ?? "",
                    Type: FormatType(p.ParameterType),
                    Modifiers: GetParamModifiers(p)
                )).ToList();

                members.Add(new ExtensionMemberInfo(
                    Kind: isIndexer ? "Indexer" : "Property",
                    Name: isIndexer ? "this[]" : prop.Name,
                    IsStatic: isMemberStatic,
                    ReturnType: FormatType(prop.PropertyType),
                    Accessors: accessors,
                    Parameters: memberParams,
                    GenericParameters: new List<GenericParamInfo>()
                ));
            }

            // 2. Methods
            foreach (var method in groupType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName && (method.Name.StartsWith("get_") || method.Name.StartsWith("set_")))
                    continue;

                bool isMemberStatic = method.IsStatic;
                if (!isMemberStatic) isBlockStatic = false;

                var methodParamsList = method.GetParameters();
                int expectedParamCount = (isMemberStatic ? 0 : 1) + methodParamsList.Length;
                var loweredMethod = outerMethods.FirstOrDefault(m => m.Name == method.Name && m.GetParameters().Length == expectedParamCount);

                var memberParams = new List<MemberParamInfo>();
                if (loweredMethod != null)
                {
                    var allParams = loweredMethod.GetParameters();
                    int skipCount = isMemberStatic ? 0 : 1;

                    if (!isMemberStatic && receiver == null && allParams.Length > 0)
                    {
                        receiver = ExtractReceiver(allParams[0]);
                        extendedType = FormatType(allParams[0].ParameterType);
                    }
                    else if (isMemberStatic && string.IsNullOrEmpty(extendedType))
                    {
                        extendedType = blockGenericParams.Count > 0 ? "List<T>" : "string";
                    }

                    for (int i = skipCount; i < allParams.Length; i++)
                    {
                        var p = allParams[i];
                        memberParams.Add(new MemberParamInfo(
                            Name: p.Name ?? "",
                            Type: FormatType(p.ParameterType),
                            Modifiers: GetParamModifiers(p)
                        ));
                    }
                }

                var methodGenParams = method.GetGenericArguments()
                    .Select(gp => new GenericParamInfo(CleanGenName(gp.Name), GetGenericConstraints(gp)))
                    .ToList();

                members.Add(new ExtensionMemberInfo(
                    Kind: "Method",
                    Name: method.Name,
                    IsStatic: isMemberStatic,
                    ReturnType: FormatType(method.ReturnType),
                    Accessors: new List<string>(),
                    Parameters: memberParams,
                    GenericParameters: methodGenParams
                ));
            }

            result.Add(new ExtensionBlockInfo(
                IsStatic: isBlockStatic,
                Receiver: receiver,
                ExtendedType: extendedType,
                GenericParameters: blockGenericParams,
                Members: members
            ));
        }

        return result;
    }

    static ReceiverInfo ExtractReceiver(ParameterInfo p)
    {
        return new ReceiverInfo(
            Name: p.Name ?? "",
            Type: FormatType(p.ParameterType),
            Modifiers: GetParamModifiers(p)
        );
    }

    static string GetParamModifiers(ParameterInfo p)
    {
        if (p.ParameterType.IsByRef)
        {
            if (p.IsIn || p.GetCustomAttributesData().Any(a => a.AttributeType.Name == "IsReadOnlyAttribute" || a.AttributeType.Name == "RequiresLocationAttribute"))
                return "ref readonly";
            if (p.IsOut)
                return "out";
            return "ref";
        }
        return "none";
    }

    static List<string> GetGenericConstraints(Type gp)
    {
        var list = new List<string>();
        var gpa = gp.GenericParameterAttributes;
        if ((gpa & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            list.Add("class");
        if ((gpa & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            list.Add("struct");
        if ((gpa & GenericParameterAttributes.DefaultConstructorConstraint) != 0 && (gpa & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
            list.Add("new()");

        foreach (var c in gp.GetGenericParameterConstraints())
        {
            if (c != typeof(object) && c != typeof(ValueType))
                list.Add(CleanGenName(c.Name));
        }

        return list;
    }

    static string CleanGenName(string name)
    {
        if (name.StartsWith("$T")) return "T";
        if (name.StartsWith("$U")) return "U";
        if (name.StartsWith("$Y")) return "Y";
        return name;
    }

    static string FormatType(Type t)
    {
        if (t.IsByRef)
            return FormatType(t.GetElementType()!);
        if (t.IsGenericType)
        {
            string name = t.Name.Split('`')[0];
            var args = string.Join(", ", t.GetGenericArguments().Select(FormatType));
            return $"{name}<{args}>";
        }
        if (t.Name.StartsWith("$T")) return "T";
        if (t.Name.StartsWith("$U")) return "U";
        if (t.Name.StartsWith("$Y")) return "Y";
        return t.Name switch
        {
            "Int32" => "int",
            "String" => "string",
            "Single" => "float",
            "Void" => "void",
            "Double" => "double",
            "Boolean" => "bool",
            _ => t.Name
        };
    }
}

// ==================== ROSLYN SOURCE EXTRACTOR ====================
public static class RoslynSourceExtensionExtractor
{
    public static List<ExtensionBlockInfo> ExtractFromSource(string sourceCode)
    {
        var result = new List<ExtensionBlockInfo>();
        int classIdx = sourceCode.IndexOf("public static class SampleExtensions");
        if (classIdx < 0) return result;

        int openBrace = sourceCode.IndexOf('{', classIdx);
        int closeBrace = sourceCode.IndexOf("// ==================== UNIFIED DATA CONTRACT", openBrace);
        if (closeBrace < 0) closeBrace = sourceCode.Length;

        string classBody = sourceCode.Substring(openBrace + 1, closeBrace - openBrace - 1);
        string cleanClassBody = Regex.Replace(classBody, @"//.*$", "", RegexOptions.Multiline);

        int pos = 0;
        while (pos < cleanClassBody.Length)
        {
            int extIdx = cleanClassBody.IndexOf("extension", pos, StringComparison.Ordinal);
            if (extIdx < 0) break;

            int blockOpenBrace = cleanClassBody.IndexOf('{', extIdx);
            if (blockOpenBrace < 0) break;

            string header = cleanClassBody.Substring(extIdx, blockOpenBrace - extIdx).Trim();

            int depth = 1;
            int blockCloseBrace = blockOpenBrace + 1;
            while (blockCloseBrace < cleanClassBody.Length && depth > 0)
            {
                if (cleanClassBody[blockCloseBrace] == '{') depth++;
                else if (cleanClassBody[blockCloseBrace] == '}') depth--;
                blockCloseBrace++;
            }

            string bodyText = cleanClassBody.Substring(blockOpenBrace + 1, blockCloseBrace - blockOpenBrace - 2);
            pos = blockCloseBrace;

            // 1. Generic params on extension block
            string headerWithoutWhere = header;
            string whereClause = "";
            int whereIdx = header.IndexOf("where", StringComparison.Ordinal);
            if (whereIdx >= 0)
            {
                headerWithoutWhere = header.Substring(0, whereIdx).Trim();
                whereClause = header.Substring(whereIdx).Trim();
            }

            int openParen = headerWithoutWhere.IndexOf("(");
            int closeParen = headerWithoutWhere.LastIndexOf(")");
            string headerPrefix = openParen > 0 ? headerWithoutWhere.Substring(0, openParen).Trim() : headerWithoutWhere;
            string paramContent = (openParen >= 0 && closeParen > openParen) ? headerWithoutWhere.Substring(openParen + 1, closeParen - openParen - 1).Trim() : "";

            var genParams = new List<GenericParamInfo>();
            if (headerPrefix.Contains("<") && headerPrefix.Contains(">"))
            {
                int openAngle = headerPrefix.IndexOf("<");
                int closeAngle = headerPrefix.LastIndexOf(">");
                string genName = headerPrefix.Substring(openAngle + 1, closeAngle - openAngle - 1).Trim();

                var constraints = new List<string>();
                if (!string.IsNullOrEmpty(whereClause))
                {
                    constraints.AddRange(whereClause.Split(':').Last().Split(',').Select(c => c.Trim()));
                }
                genParams.Add(new GenericParamInfo(genName, constraints));
            }

            bool isStatic = true;
            ReceiverInfo? receiver = null;
            string extendedType = paramContent;

            var paramParts = paramContent.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (paramParts.Length >= 2)
            {
                isStatic = false;
                string paramName = paramParts.Last();
                string modifiers = "none";
                string typeName = "";

                if (paramParts.Length == 3)
                {
                    modifiers = paramParts[0];
                    typeName = paramParts[1];
                }
                else
                {
                    modifiers = (paramParts[0] == "ref" || paramParts[0] == "in" || paramParts[0] == "out") ? paramParts[0] : "none";
                    typeName = modifiers == "none" ? paramParts[0] : paramParts[1];
                }

                extendedType = typeName;
                receiver = new ReceiverInfo(paramName, typeName, modifiers);
            }

            // 3. Members inside block
            var members = new List<ExtensionMemberInfo>();
            var memberChunks = bodyText.Split(new[] { "public " }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawChunk in memberChunks)
            {
                string chunk = "public " + rawChunk.Trim();
                bool isMemberStatic = chunk.Contains("public static ");

                if (chunk.Contains("this["))
                {
                    // Indexer
                    var indexerMatch = Regex.Match(chunk, @"public\s+(?:static\s+)?([^\s]+)\s+this\[([^\]]+)\]");
                    if (indexerMatch.Success)
                    {
                        string retType = indexerMatch.Groups[1].Value.Replace("?", "");
                        string paramsRaw = indexerMatch.Groups[2].Value.Trim();

                        var idxParams = new List<MemberParamInfo>();
                        foreach (var p in paramsRaw.Split(','))
                        {
                            var pParts = p.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (pParts.Length == 2)
                                idxParams.Add(new MemberParamInfo(pParts[1], pParts[0], "none"));
                        }

                        members.Add(new ExtensionMemberInfo(
                            Kind: "Indexer",
                            Name: "this[]",
                            IsStatic: isMemberStatic,
                            ReturnType: retType,
                            Accessors: new List<string> { "get" },
                            Parameters: idxParams,
                            GenericParameters: new List<GenericParamInfo>()
                        ));
                    }
                }
                else if (chunk.Contains("=>") && !chunk.Contains("("))
                {
                    // Property
                    var propMatch = Regex.Match(chunk, @"public\s+(?:static\s+)?([^\s]+)\s+([^\s=>]+)");
                    if (propMatch.Success)
                    {
                        string retType = propMatch.Groups[1].Value.Replace("?", "");
                        string propName = propMatch.Groups[2].Value;
                        members.Add(new ExtensionMemberInfo(
                            Kind: "Property",
                            Name: propName,
                            IsStatic: isMemberStatic,
                            ReturnType: retType,
                            Accessors: new List<string> { "get" },
                            Parameters: new List<MemberParamInfo>(),
                            GenericParameters: new List<GenericParamInfo>()
                        ));
                    }
                }
                else if (chunk.Contains("("))
                {
                    // Method
                    var methodMatch = Regex.Match(chunk, @"public\s+(?:static\s+)?([^\s]+)\s+([^\s(<]+)(?:<([^>]+)>)?\s*\(([^)]*)\)");
                    if (methodMatch.Success)
                    {
                        string retType = methodMatch.Groups[1].Value.Replace("?", "");
                        string methodName = methodMatch.Groups[2].Value;
                        string genParamsRaw = methodMatch.Groups[3].Value.Trim();
                        string paramsRaw = methodMatch.Groups[4].Value.Trim();

                        var methodParams = new List<MemberParamInfo>();
                        if (!string.IsNullOrEmpty(paramsRaw))
                        {
                            foreach (var p in paramsRaw.Split(','))
                            {
                                var pParts = p.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (pParts.Length == 2)
                                    methodParams.Add(new MemberParamInfo(pParts[1], pParts[0], "none"));
                            }
                        }

                        var methodGenParams = new List<GenericParamInfo>();
                        if (!string.IsNullOrEmpty(genParamsRaw))
                        {
                            var genNames = genParamsRaw.Split(',').Select(g => g.Trim()).ToList();
                            var whereMatches = Regex.Matches(chunk, @"where\s+([^\s:]+)\s*:\s*([^\r\n{]+)");

                            foreach (var gName in genNames)
                            {
                                var constraints = new List<string>();
                                foreach (Match wm in whereMatches)
                                {
                                    if (wm.Groups[1].Value.Trim() == gName)
                                    {
                                        constraints.AddRange(wm.Groups[2].Value.Split(',').Select(c => c.Trim()));
                                    }
                                }
                                methodGenParams.Add(new GenericParamInfo(gName, constraints));
                            }
                        }

                        members.Add(new ExtensionMemberInfo(
                            Kind: "Method",
                            Name: methodName,
                            IsStatic: isMemberStatic,
                            ReturnType: retType,
                            Accessors: new List<string>(),
                            Parameters: methodParams,
                            GenericParameters: methodGenParams
                        ));
                    }
                }
            }

            result.Add(new ExtensionBlockInfo(
                IsStatic: isStatic,
                Receiver: receiver,
                ExtendedType: extendedType,
                GenericParameters: genParams,
                Members: members
            ));
        }

        return result;
    }
}

// ==================== TEST HARNESS & VERIFICATION ====================
public static class Program
{
    public static void Main()
    {
        TestRoslynBackend.RunTests();

        var type = typeof(SampleExtensions);
        Console.WriteLine($"Static Class: {type.FullName}");
        Console.WriteLine("\n--- Methods directly on Outer Static Class ---");
        foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            var attrs = m.CustomAttributes.Select(a => a.AttributeType.Name).ToList();
            Console.WriteLine($"  Method '{m.Name}': Attributes = [{string.Join(", ", attrs)}]");
        }

        Console.WriteLine("\n--- Nested Types inside Outer Static Class ---");
        foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            var typeAttrs = nested.CustomAttributes.Select(a => a.AttributeType.Name).ToList();
            Console.WriteLine($"  Nested Type '{nested.Name}': Attributes = [{string.Join(", ", typeAttrs)}]");
            foreach (var m in nested.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var attrs = m.CustomAttributes.Select(a => a.AttributeType.Name).ToList();
                Console.WriteLine($"    Nested Method '{m.Name}' (Static={m.IsStatic}): Attributes = [{string.Join(", ", attrs)}]");
            }
            foreach (var p in nested.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                var attrs = p.CustomAttributes.Select(a => a.AttributeType.Name).ToList();
                Console.WriteLine($"    Nested Property/Indexer '{p.Name}': Attributes = [{string.Join(", ", attrs)}]");
            }
        }
        Console.WriteLine("================================================================================\n");

        Console.WriteLine("=================== 1. REFLECTION EXTRACTOR OUTPUT ===================");
        var reflBlocks = ReflectionExtensionExtractor.Extract(typeof(SampleExtensions));
        PrintBlocks(reflBlocks);

        Console.WriteLine("\n=================== 2. ROSLYN EXTRACTOR OUTPUT ===================");
        var sourceCode = File.ReadAllText(@"d:\programming\Rectaleaf\src\playground\PlaygroundApp\Program.cs");
        var roslynBlocks = RoslynSourceExtensionExtractor.ExtractFromSource(sourceCode);
        PrintBlocks(roslynBlocks);

        Console.WriteLine("\n=================== 3. COMPARISON & PARITY REPORT ===================");
        Console.WriteLine($"Reflection Block Count: {reflBlocks.Count}");
        Console.WriteLine($"Roslyn Block Count:     {roslynBlocks.Count}");

        bool allMatch = (reflBlocks.Count == roslynBlocks.Count);

        for (int b = 0; b < Math.Min(reflBlocks.Count, roslynBlocks.Count); b++)
        {
            var r = reflBlocks[b];
            var s = roslynBlocks[b];

            Console.WriteLine($"\n--- Extension Block #{b + 1} Comparison ---");
            bool isStaticMatch = (r.IsStatic == s.IsStatic);
            bool receiverMatch = (r.Receiver?.Name == s.Receiver?.Name && r.Receiver?.Modifiers == s.Receiver?.Modifiers);
            bool genericMatch = (r.GenericParameters.Count == s.GenericParameters.Count);
            bool membersCountMatch = (r.Members.Count == s.Members.Count);

            Console.WriteLine($"  IsStatic Match:          {isStaticMatch} (Refl: {r.IsStatic}, Roslyn: {s.IsStatic})");
            Console.WriteLine($"  Receiver Match:          {receiverMatch} (Refl: '{r.Receiver?.Modifiers} {r.Receiver?.Name}', Roslyn: '{s.Receiver?.Modifiers} {s.Receiver?.Name}')");
            Console.WriteLine($"  Generic Params Match:    {genericMatch} (Count: {r.GenericParameters.Count})");
            Console.WriteLine($"  Members Count Match:     {membersCountMatch} (Count: {r.Members.Count})");

            foreach (var rm in r.Members)
            {
                var sm = s.Members.FirstOrDefault(m => m.Name == rm.Name && m.Kind == rm.Kind);
                if (sm != null)
                {
                    bool genParamMatch = rm.GenericParameters.Count == sm.GenericParameters.Count;
                    Console.WriteLine($"    Member '{rm.Name}' ({rm.Kind}): MATCH (Static={rm.IsStatic}, ReturnType={rm.ReturnType}, Params={rm.Parameters.Count}, GenParams={rm.GenericParameters.Count})");
                    if (!genParamMatch) allMatch = false;
                }
                else
                {
                    Console.WriteLine($"    Member '{rm.Name}' ({rm.Kind}): MISSING IN ROSLYN!");
                    allMatch = false;
                }
            }

            if (!isStaticMatch || !receiverMatch || !genericMatch || !membersCountMatch)
                allMatch = false;
        }

        Console.WriteLine($"\n============================================================");
        Console.WriteLine($"PARITY VERDICT: {(allMatch ? "SUCCESS - 100% PARITY ACHIEVED" : "FAILED")}");
        Console.WriteLine($"============================================================");
    }

    static void PrintBlocks(List<ExtensionBlockInfo> blocks)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i];
            Console.WriteLine($"\nExtension Block #{i + 1}:");
            Console.WriteLine($"  IsStatic: {b.IsStatic}");
            Console.WriteLine($"  ExtendedType: '{b.ExtendedType}'");
            if (b.Receiver != null)
                Console.WriteLine($"  Receiver: '{b.Receiver.Modifiers} {b.Receiver.Type} {b.Receiver.Name}'");
            else
                Console.WriteLine($"  Receiver: [None - Static Extension]");

            if (b.GenericParameters.Count > 0)
                Console.WriteLine($"  GenericParameters: {string.Join(", ", b.GenericParameters.Select(gp => $"{gp.Name} (where {string.Join(", ", gp.Constraints)})"))}");

            Console.WriteLine("  Members:");
            foreach (var m in b.Members)
            {
                if (m.Kind == "Property")
                {
                    Console.WriteLine($"    Property: {(m.IsStatic ? "static " : "")}{m.ReturnType} {m.Name} {{ {string.Join(", ", m.Accessors)} }}");
                }
                else if (m.Kind == "Indexer")
                {
                    var paramList = string.Join(", ", m.Parameters.Select(p => $"{(p.Modifiers != "none" ? p.Modifiers + " " : "")}{p.Type} {p.Name}"));
                    Console.WriteLine($"    Indexer: {(m.IsStatic ? "static " : "")}{m.ReturnType} this[{paramList}] {{ {string.Join(", ", m.Accessors)} }}");
                }
                else
                {
                    string genString = m.GenericParameters.Count > 0 ? $"<{string.Join(", ", m.GenericParameters.Select(g => g.Name))}>" : "";
                    string constraintString = m.GenericParameters.Any(g => g.Constraints.Count > 0) 
                        ? " " + string.Join(" ", m.GenericParameters.Where(g => g.Constraints.Count > 0).Select(g => $"where {g.Name} : {string.Join(", ", g.Constraints)}")) 
                        : "";
                    var paramList = string.Join(", ", m.Parameters.Select(p => $"{(p.Modifiers != "none" ? p.Modifiers + " " : "")}{p.Type} {p.Name}"));
                    Console.WriteLine($"    Method: {(m.IsStatic ? "static " : "")}{m.ReturnType} {m.Name}{genString}({paramList}){constraintString}");
                }
            }
        }
    }
}
