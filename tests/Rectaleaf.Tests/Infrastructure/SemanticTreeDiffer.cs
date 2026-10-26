using System.Text;
using System.Text.Json.Nodes;

namespace Rectaleaf.Tests.Infrastructure;

public sealed record Difference(
    string FormattedPathDetailed,
    string HeaderNormal,
    string HeaderDetailed,
    string Expected,
    string Actual,
    string Message
);

public sealed class DiffReport
{
    public bool HasDifferences => Differences.Count > 0;
    public List<Difference> Differences { get; } = [];

    public void Add(Difference difference) => Differences.Add(difference);
}

public static class SemanticTreeDiffer
{
    public static DiffReport Compare(JsonNode? expected, JsonNode? actual, string rootName = "Root")
    {
        DiffReport report = new();
        DiffContext context = new(report, rootName);
        CompareNodes(expected, actual, context);
        return report;
    }

    enum SegmentKind
    {
        Root,
        Type,
        MemberCollection,
        Member,
        SubCollection,
        CollectionItem,
        Property
    }

    sealed class DiffSegment(
        SegmentKind kind,
        string rawKey,
        string detailedPart,
        string normalPart,
        string pathPart,
        bool replacesParent = false,
        string? typeNamespace = null)
    {
        public SegmentKind Kind { get; } = kind;
        public string RawKey { get; } = rawKey;
        public string DetailedPart { get; } = detailedPart;
        public string NormalPart { get; } = normalPart;
        public string PathPart { get; } = pathPart;
        public bool ReplacesParent { get; } = replacesParent;
        public string? TypeNamespace { get; } = typeNamespace;
    }

    sealed class DiffContext
    {
        public DiffReport Report { get; }
        public string RootName { get; }
        readonly List<DiffSegment> segments = [];

        public DiffContext(DiffReport report, string rootName)
        {
            Report = report;
            RootName = rootName;
            segments.Add(new DiffSegment(SegmentKind.Root, rootName, rootName, rootName, rootName));
        }

        public void Push(DiffSegment segment) => segments.Add(segment);

        public void Pop()
        {
            if (segments.Count > 1)
                segments.RemoveAt(segments.Count - 1);
        }

        public DiffSegment? Peek() => segments.Count > 0 ? segments[^1] : null;

        public string GetCurrentNamespace()
        {
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                if (segments[i].Kind == SegmentKind.Type)
                {
                    if (segments[i].TypeNamespace is string ns)
                        return ns;

                    string detailed = segments[i].DetailedPart;
                    int lastDot = detailed.LastIndexOf('.');
                    if (lastDot > 0)
                        return detailed[..lastDot];
                }
            }
            return "";
        }

        public void Record(string expected, string actual, string message)
            => Report.Add(BuildDifference(expected, actual, message));

        Difference BuildDifference(string expected, string actual, string message)
        {
            int firstTypeIdx = -1;
            int lastTypeIdx = -1;

            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].Kind == SegmentKind.Type)
                {
                    if (firstTypeIdx == -1)
                        firstTypeIdx = i;
                    lastTypeIdx = i;
                }
            }

            string typeDetailed = lastTypeIdx >= 0 ? segments[lastTypeIdx].DetailedPart : RootName;
            string typeNormal = lastTypeIdx >= 0 ? segments[lastTypeIdx].NormalPart : RootName;

            int afterTypeStart = lastTypeIdx >= 0 ? lastTypeIdx + 1 : 1;
            int lastMemberIdx = -1;
            List<string> memberNormals = [];
            List<string> memberDetaileds = [];

            for (int i = afterTypeStart; i < segments.Count; i++)
            {
                if (segments[i].Kind == SegmentKind.Member)
                {
                    lastMemberIdx = i;
                    memberNormals.Add(segments[i].NormalPart);
                    memberDetaileds.Add(segments[i].DetailedPart);
                }
            }

            StringBuilder headerNormalBuilder = new(typeNormal);
            StringBuilder headerDetailedBuilder = new(typeDetailed);

            if (memberNormals.Count > 0)
            {
                headerNormalBuilder.Append("::").Append(string.Join("::", memberNormals));
                headerDetailedBuilder.Append("::").Append(string.Join("::", memberDetaileds));
            }

            int tailStart = lastMemberIdx >= 0 ? lastMemberIdx + 1 : afterTypeStart;
            bool isFirstTailPart = true;

            for (int i = tailStart; i < segments.Count; i++)
            {
                if (i + 1 < segments.Count && segments[i + 1].ReplacesParent)
                    continue;

                DiffSegment seg = segments[i];
                if (seg.Kind == SegmentKind.CollectionItem)
                {
                    headerNormalBuilder.Append(": ").Append(seg.NormalPart);
                    headerDetailedBuilder.Append(isFirstTailPart ? ": " : ".").Append(seg.DetailedPart);
                    isFirstTailPart = false;
                }
                else if (!string.IsNullOrEmpty(seg.NormalPart))
                {
                    headerNormalBuilder.Append('.').Append(seg.NormalPart);
                    headerDetailedBuilder.Append('.').Append(seg.DetailedPart);
                    isFirstTailPart = false;
                }
            }

            List<string> pathParts = [];
            int pathStart = firstTypeIdx >= 0 ? firstTypeIdx : 0;

            for (int i = pathStart; i < segments.Count; i++)
            {
                if (i + 1 < segments.Count && segments[i + 1].ReplacesParent)
                    continue;

                if (!string.IsNullOrEmpty(segments[i].PathPart))
                    pathParts.Add(segments[i].PathPart);
            }

            string formattedPathDetailed = string.Join(".", pathParts);
            return new Difference(
                formattedPathDetailed,
                headerNormalBuilder.ToString(),
                headerDetailedBuilder.ToString(),
                expected,
                actual,
                message);
        }
    }

    static void CompareNodes(JsonNode? expected, JsonNode? actual, DiffContext ctx)
    {
        if (expected is null && actual is null)
            return;

        if (expected is null)
        {
            string actualStr = actual!.ToJsonString();
            ctx.Record("null", actualStr, $"Unexpected value: expected null, got {actualStr}");
            return;
        }

        if (actual is null)
        {
            string expectedStr = expected.ToJsonString();
            ctx.Record(expectedStr, "null", $"Missing value: expected {expectedStr}, got null");
            return;
        }

        if (expected is JsonObject expObj && actual is JsonObject actObj)
        {
            CompareObjects(expObj, actObj, ctx);
        }
        else if (expected is JsonArray expArr && actual is JsonArray actArr)
        {
            CompareArrays(expArr, actArr, ctx);
        }
        else if (expected is JsonValue expVal && actual is JsonValue actVal)
        {
            CompareValues(expVal, actVal, ctx);
        }
        else if (ctx.Peek()?.Kind == SegmentKind.Root &&
                 ((expected is JsonObject && actual is JsonArray) || (expected is JsonArray && actual is JsonObject)))
        {
            JsonArray expNormalized = expected is JsonArray ea ? ea : new JsonArray(expected.DeepClone());
            JsonArray actNormalized = actual is JsonArray aa ? aa : new JsonArray(actual.DeepClone());
            CompareArrays(expNormalized, actNormalized, ctx);
        }
        else
        {
            ctx.Record(
                expected.ToJsonString(),
                actual.ToJsonString(),
                $"Mismatched node kinds: expected {expected.GetType().Name}, got {actual.GetType().Name}");
        }
    }

    static bool IsDeclaredTypeObject(JsonObject obj)
        => obj.ContainsKey("fullyQualifiedName") && obj.ContainsKey("kind");

    static DiffSegment CreateTypeSegment(JsonObject probe, DiffSegment? parentSeg)
    {
        string fqn = GetNonEmptyString(probe, "fullyQualifiedName") ?? "";
        string ns = GetNonEmptyString(probe, "namespace") ?? "";
        string normalName = !string.IsNullOrEmpty(ns) && fqn.StartsWith(ns + ".", StringComparison.Ordinal)
            ? fqn[(ns.Length + 1)..]
            : fqn;

        bool isNested = parentSeg?.RawKey == "nestedTypes";
        string pathPart = isNested
            ? $"nestedTypes[{GetNonEmptyString(probe, "localName") ?? normalName}]"
            : fqn;

        return new DiffSegment(SegmentKind.Type, fqn, fqn, normalName, pathPart, replacesParent: isNested, typeNamespace: ns);
    }

    static void CompareObjects(JsonObject expected, JsonObject actual, DiffContext ctx)
    {
        JsonObject probe = expected.ContainsKey("fullyQualifiedName") ? expected : actual;
        DiffSegment? parentSeg = ctx.Peek();
        bool pushType = IsDeclaredTypeObject(probe)
            && (parentSeg is null || parentSeg.Kind == SegmentKind.Root || parentSeg.RawKey == "nestedTypes");

        if (pushType)
            ctx.Push(CreateTypeSegment(probe, parentSeg));

        try
        {
            DiffSegment? currentParent = ctx.Peek();
            foreach ((string propName, JsonNode? expChild) in expected)
            {
                DiffSegment seg = CreatePropertySegment(propName, currentParent);
                ctx.Push(seg);

                if (actual.TryGetPropertyValue(propName, out JsonNode? actChild))
                {
                    CompareNodes(expChild, actChild, ctx);
                }
                else
                {
                    ctx.Record(expChild?.ToJsonString() ?? "null", "<missing>", $"Missing property '{propName}'");
                }
                ctx.Pop();
            }

            foreach ((string propName, JsonNode? actChild) in actual)
            {
                if (!expected.ContainsKey(propName))
                {
                    DiffSegment seg = CreatePropertySegment(propName, currentParent);
                    ctx.Push(seg);
                    ctx.Record("<none>", actChild?.ToJsonString() ?? "null", $"Unexpected extra property '{propName}'");
                    ctx.Pop();
                }
            }
        }
        finally
        {
            if (pushType)
                ctx.Pop();
        }
    }

    static DiffSegment CreatePropertySegment(string propName, DiffSegment? parentSeg = null)
    {
        if (parentSeg?.RawKey == "constructorArguments")
        {
            string combined = $"constructorArguments[{propName}]";
            return new DiffSegment(SegmentKind.CollectionItem, propName, combined, $"constructor argument '{propName}'", combined, replacesParent: true);
        }

        if (parentSeg?.RawKey == "namedArguments")
        {
            string combined = $"namedArguments[{propName}]";
            return new DiffSegment(SegmentKind.CollectionItem, propName, combined, $"named argument '{propName}'", combined, replacesParent: true);
        }

        if (propName == "staticConstructor")
            return new DiffSegment(SegmentKind.Member, "staticConstructor", ".cctor", ".cctor", "staticConstructor");

        if (IsMemberCollection(propName))
            return new DiffSegment(SegmentKind.MemberCollection, propName, propName, propName, propName);

        if (IsSubCollection(propName))
            return new DiffSegment(SegmentKind.SubCollection, propName, propName, propName, propName);

        return new DiffSegment(SegmentKind.Property, propName, propName, propName, propName);
    }

    static bool IsMemberCollection(string name) => name is
        "methods" or "fields" or "properties" or "indexers" or "events" or
        "instanceConstructors" or "operators" or "extensionsBlocks" or "enumMembers";

    static bool IsSubCollection(string name) => name is
        "parameters" or "genericParameters" or "attributes" or "receiverAttributes" or "localFunctions" or
        "directInterfaces" or "constraintTypes" or "accessors" or "nestedTypes";

    static bool IsStrictlyPositionalCollection(DiffSegment? parentSeg) => parentSeg?.RawKey is
        "typeArguments" or "elements" or "elementTypes" or "callingConventions" or "constructorArguments";

    static void CompareArrays(JsonArray expected, JsonArray actual, DiffContext ctx)
    {
        DiffSegment? parentSeg = ctx.Peek();

        if (expected.Count != actual.Count)
        {
            ctx.Record($"Count = {expected.Count}", $"Count = {actual.Count}",
                $"Collection count mismatch: expected {expected.Count} elements, got {actual.Count} elements.");
        }

        if (IsStrictlyPositionalCollection(parentSeg))
        {
            int maxCount = Math.Max(expected.Count, actual.Count);
            for (int i = 0; i < maxCount; i++)
            {
                JsonNode? expItem = i < expected.Count ? expected[i] : null;
                JsonNode? actItem = i < actual.Count ? actual[i] : null;
                CompareArrayElementPair(expItem, actItem, parentSeg, i, ctx);
            }
            return;
        }

        CompareKeyedArrays(expected, actual, parentSeg, ctx);
    }

    static void CompareKeyedArrays(JsonArray expected, JsonArray actual, DiffSegment? parentSeg, DiffContext ctx)
    {
        int expCount = expected.Count;
        int actCount = actual.Count;

        if (expCount == 0 && actCount == 0)
            return;

        string?[] expKeys = new string?[expCount];
        string?[] actKeys = new string?[actCount];
        string collectionKey = parentSeg?.RawKey ?? "";

        for (int i = 0; i < expCount; i++)
            expKeys[i] = GetElementSemanticKey(expected[i], collectionKey);

        for (int j = 0; j < actCount; j++)
            actKeys[j] = GetElementSemanticKey(actual[j], collectionKey);

        int[] expToAct = new int[expCount];
        Array.Fill(expToAct, -1);
        bool[] actMatched = new bool[actCount];

        // Pass 1: Match elements with identical semantic key AND deep equality (handles duplicate attribute types etc.)
        for (int i = 0; i < expCount; i++)
        {
            string? key = expKeys[i];
            if (key is null)
                continue;

            for (int j = 0; j < actCount; j++)
            {
                if (!actMatched[j] && key == actKeys[j] && JsonNode.DeepEquals(expected[i], actual[j]))
                {
                    expToAct[i] = j;
                    actMatched[j] = true;
                    break;
                }
            }
        }

        // Pass 2: Match remaining elements by semantic key
        for (int i = 0; i < expCount; i++)
        {
            if (expToAct[i] != -1)
                continue;

            string? key = expKeys[i];
            if (key is null)
                continue;

            for (int j = 0; j < actCount; j++)
            {
                if (!actMatched[j] && key == actKeys[j])
                {
                    expToAct[i] = j;
                    actMatched[j] = true;
                    break;
                }
            }
        }

        // Pass 3: Positional fallback for remaining unmatched elements
        int nextUnmatchedAct = 0;
        for (int i = 0; i < expCount; i++)
        {
            if (expToAct[i] != -1)
                continue;

            while (nextUnmatchedAct < actCount && actMatched[nextUnmatchedAct])
                nextUnmatchedAct++;

            if (nextUnmatchedAct < actCount)
            {
                expToAct[i] = nextUnmatchedAct;
                actMatched[nextUnmatchedAct] = true;
                nextUnmatchedAct++;
            }
        }

        // Compare matched and missing expected elements
        for (int i = 0; i < expCount; i++)
        {
            int matchedActIdx = expToAct[i];
            JsonNode? expItem = expected[i];
            JsonNode? actItem = matchedActIdx >= 0 ? actual[matchedActIdx] : null;
            CompareArrayElementPair(expItem, actItem, parentSeg, i, ctx);
        }

        // Report any leftover unmatched actual elements
        for (int j = 0; j < actCount; j++)
        {
            if (!actMatched[j])
                CompareArrayElementPair(null, actual[j], parentSeg, j, ctx);
        }
    }

    static void CompareArrayElementPair(JsonNode? expItem, JsonNode? actItem, DiffSegment? parentSeg, int index, DiffContext ctx)
    {
        JsonNode? probeItem = expItem ?? actItem;
        DiffSegment itemSeg = CreateArrayItemSegment(probeItem, parentSeg, index, ctx.GetCurrentNamespace());
        ctx.Push(itemSeg);

        if (expItem is null && actItem is not null)
        {
            string actStr = actItem.ToJsonString();
            ctx.Record("<none>", actStr, $"Unexpected extra element: {actStr}");
        }
        else if (actItem is null && expItem is not null)
        {
            string expStr = expItem.ToJsonString();
            ctx.Record(expStr, "<missing>", $"Missing expected element: {expStr}");
        }
        else
        {
            CompareNodes(expItem, actItem, ctx);
        }

        ctx.Pop();
    }

    static string? GetElementSemanticKey(JsonNode? item, string collectionName)
    {
        if (item is not JsonObject obj)
            return item?.ToJsonString();

        if (IsDeclaredTypeObject(obj))
            return GetNonEmptyString(obj, "fullyQualifiedName") ?? GetNonEmptyString(obj, "localName");

        switch (collectionName)
        {
            case "fields" or "properties" or "events" or "enumMembers" or "parameters" or "genericParameters" or "localFunctions":
                return GetNonEmptyString(obj, "name");

            case "accessors":
                return GetNonEmptyString(obj, "kind");

            case "attributes" or "receiverAttributes":
                if (obj["underlyingType"] is JsonObject utObj)
                {
                    if (utObj["typeDefinition"] is JsonObject tdObj && GetNonEmptyString(tdObj, "fullyQualifiedName") is string tdFqn)
                        return tdFqn;
                    return GetNonEmptyString(utObj, "typeDefinition") ?? GetNonEmptyString(utObj, "localName");
                }
                return GetNonEmptyString(obj, "fullyQualifiedName") ?? GetNonEmptyString(obj, "name");

            case "directInterfaces" or "constraintTypes":
                return GetNonEmptyString(obj, "typeDefinition")
                    ?? GetNonEmptyString(obj, "placeholderName")
                    ?? GetNonEmptyString(obj, "localName");

            case "methods":
            {
                string name = GetNonEmptyString(obj, "name") ?? "";
                int genCount = (obj["genericParameters"] as JsonArray)?.Count ?? 0;
                string paramKey = GetParametersKey(obj["parameters"] as JsonArray);
                return $"{name}`{genCount}({paramKey})";
            }

            case "instanceConstructors":
            {
                string paramKey = GetParametersKey(obj["parameters"] as JsonArray);
                return $".ctor({paramKey})";
            }

            case "indexers":
            {
                string name = GetNonEmptyString(obj, "name") ?? "this";
                string paramKey = GetParametersKey(obj["parameters"] as JsonArray);
                return $"{name}[{paramKey}]";
            }

            case "operators":
            {
                bool isChecked = obj["isChecked"]?.GetValue<bool>() == true;
                string opKind = GetNonEmptyString(obj, "binaryKind")
                    ?? GetNonEmptyString(obj, "unaryKind")
                    ?? GetNonEmptyString(obj, "conversionKind")
                    ?? GetNonEmptyString(obj, "operatorKind")
                    ?? "";
                string left = FormatOperandKey(obj["leftOperand"] ?? obj["operand"]);
                string right = FormatOperandKey(obj["rightOperand"]);
                string ret = FormatTypeKey(obj["returnInfo"]?["type"]);
                return $"{(isChecked ? "checked_" : "")}{opKind}({left},{right}):{ret}";
            }

            case "extensionsBlocks":
            {
                bool isStatic = obj["isStatic"]?.GetValue<bool>() == true;
                int genCount = (obj["genericParameters"] as JsonArray)?.Count ?? 0;
                string target = obj["receiver"] is JsonObject rec
                    ? $"{GetNonEmptyString(rec, "modifiers")}:{FormatTypeKey(rec["type"])}:{GetNonEmptyString(rec, "name")}"
                    : FormatTypeKey(obj["extendedType"]);
                return $"ext`{genCount}:{isStatic}:{target}";
            }

            default:
                return GetNonEmptyString(obj, "localName") ?? GetNonEmptyString(obj, "name");
        }

        static string GetParametersKey(JsonArray? paramArr)
        {
            if (paramArr is null || paramArr.Count == 0)
                return "";

            return string.Join(",", paramArr.Select(p =>
                p is JsonObject pObj ? (GetNonEmptyString(pObj, "name") ?? FormatTypeKey(pObj["type"])) : "?"));
        }

        static string FormatOperandKey(JsonNode? operandNode)
        {
            if (operandNode is not JsonObject opObj)
                return "";

            return $"{GetNonEmptyString(opObj, "name")}:{FormatTypeKey(opObj["type"])}";
        }

        static string FormatTypeKey(JsonNode? typeNode)
        {
            if (typeNode is not JsonObject tObj)
                return "";

            if (GetNonEmptyString(tObj, "usageKind") == "ByRef" && tObj["elementType"] is JsonObject elemObj)
            {
                bool isRo = tObj["isReadOnly"]?.GetValue<bool>() == true;
                return $"{(isRo ? "ref_readonly:" : "ref:")}{FormatTypeKey(elemObj)}";
            }

            string raw = GetNonEmptyString(tObj, "typeDefinition")
                ?? GetNonEmptyString(tObj, "predefinedKind")
                ?? GetNonEmptyString(tObj, "placeholderName")
                ?? GetNonEmptyString(tObj, "localName")
                ?? "";
            return raw.TrimEnd('?');
        }
    }

    static DiffSegment CreateArrayItemSegment(JsonNode? item, DiffSegment? parentSeg, int index, string currentNamespace)
    {
        if (item is not JsonObject obj)
        {
            if (parentSeg is not null && parentSeg.Kind != SegmentKind.Root)
            {
                string combined = $"{parentSeg.RawKey}[{index}]";
                return new DiffSegment(SegmentKind.Property, combined, combined, combined, combined, replacesParent: true);
            }
            return new DiffSegment(SegmentKind.CollectionItem, $"[{index}]", $"[{index}]", $"[{index}]", $"[{index}]");
        }

        if (IsDeclaredTypeObject(obj))
            return CreateTypeSegment(obj, parentSeg);

        if (parentSeg is not null && parentSeg.Kind == SegmentKind.MemberCollection)
        {
            string memberName = GetMemberName(obj, parentSeg.RawKey, index);
            string normalMember = SimplifyMemberName(memberName, currentNamespace);
            string pathPart = $"{parentSeg.RawKey}[{memberName}]";
            return new DiffSegment(SegmentKind.Member, memberName, memberName, normalMember, pathPart, replacesParent: true);
        }

        if (parentSeg is not null && parentSeg.Kind == SegmentKind.SubCollection)
        {
            switch (parentSeg.RawKey)
            {
                case "parameters":
                {
                    string pName = GetNonEmptyString(obj, "name") ?? $"{index}";
                    return new DiffSegment(SegmentKind.CollectionItem, pName, $"parameters[{pName}]", $"parameter '{pName}'", $"parameters[{pName}]", replacesParent: true);
                }

                case "genericParameters":
                {
                    string gName = GetNonEmptyString(obj, "name") ?? $"{index}";
                    return new DiffSegment(SegmentKind.CollectionItem, gName, $"genericParameters[{gName}]", $"generic parameter '{gName}'", $"genericParameters[{gName}]", replacesParent: true);
                }

                case "attributes" or "receiverAttributes":
                {
                    string aName = (obj["underlyingType"] is JsonObject utObj ? GetNonEmptyString(utObj, "localName") : null)
                        ?? GetNonEmptyString(obj, "name")
                        ?? $"{index}";
                    return new DiffSegment(SegmentKind.CollectionItem, aName, $"{parentSeg.RawKey}[{aName}]", $"attribute '{aName}'", $"{parentSeg.RawKey}[{aName}]", replacesParent: true);
                }

                case "localFunctions":
                {
                    string lfName = GetNonEmptyString(obj, "name") ?? $"{index}";
                    return new DiffSegment(SegmentKind.CollectionItem, lfName, $"localFunctions[{lfName}]", $"local function '{lfName}'", $"localFunctions[{lfName}]", replacesParent: true);
                }

                case "directInterfaces":
                {
                    string iName = GetNonEmptyString(obj, "localName") ?? $"{index}";
                    return new DiffSegment(SegmentKind.CollectionItem, iName, $"directInterfaces[{iName}]", $"interface '{iName}'", $"directInterfaces[{iName}]", replacesParent: true);
                }

                case "constraintTypes":
                {
                    string cName = GetNonEmptyString(obj, "localName") ?? $"{index}";
                    return new DiffSegment(SegmentKind.CollectionItem, cName, $"constraintTypes[{cName}]", $"constraint '{cName}'", $"constraintTypes[{cName}]", replacesParent: true);
                }

                case "accessors":
                {
                    string accKind = GetNonEmptyString(obj, "kind") ?? $"{index}";
                    return new DiffSegment(SegmentKind.CollectionItem, accKind, $"accessors[{accKind}]", $"accessor '{accKind}'", $"accessors[{accKind}]", replacesParent: true);
                }

                default:
                {
                    string fallback = $"{parentSeg.RawKey}[{index}]";
                    return new DiffSegment(SegmentKind.CollectionItem, $"[{index}]", fallback, fallback, fallback, replacesParent: true);
                }
            }
        }

        string itemKey = GetNonEmptyString(obj, "localName") ?? GetNonEmptyString(obj, "name") ?? $"{index}";
        if (parentSeg is not null && parentSeg.Kind != SegmentKind.Root)
        {
            string combined = $"{parentSeg.RawKey}[{itemKey}]";
            return new DiffSegment(SegmentKind.Property, combined, combined, combined, combined, replacesParent: true);
        }

        return new DiffSegment(SegmentKind.Property, $"[{itemKey}]", $"[{itemKey}]", $"[{itemKey}]", $"[{itemKey}]");
    }

    static string GetMemberName(JsonObject obj, string collectionName, int index)
    {
        if (GetNonEmptyString(obj, "name") is string name)
            return name;

        if (GetNonEmptyString(obj, "localName") is string localName)
            return localName;

        if (collectionName == "instanceConstructors")
        {
            if (obj.TryGetPropertyValue("parameters", out JsonNode? pNode) && pNode is JsonArray pArr && pArr.Count > 0)
            {
                var pTypes = pArr.Select(p => p?["type"]?["localName"]?.ToString() ?? "?");
                return $".ctor({string.Join(", ", pTypes)})";
            }
            return ".ctor";
        }

        if (collectionName == "operators")
        {
            bool isChecked = obj["isChecked"]?.GetValue<bool>() == true;
            string prefix = isChecked ? "op_Checked" : "op_";

            if (GetNonEmptyString(obj, "binaryKind") is string bKind)
                return $"{prefix}{bKind}";
            if (GetNonEmptyString(obj, "unaryKind") is string uKind)
                return $"{prefix}{uKind}";
            if (GetNonEmptyString(obj, "conversionKind") is string cKind)
                return $"op_{cKind}";
            if (GetNonEmptyString(obj, "operatorKind") is string opKind)
                return $"{prefix}{opKind}";
        }

        if (collectionName == "indexers")
        {
            if (obj.TryGetPropertyValue("parameters", out JsonNode? pNode) && pNode is JsonArray pArr && pArr.Count > 0)
            {
                var pTypes = pArr.Select(p => p?["type"]?["localName"]?.ToString() ?? "?");
                return $"this[{string.Join(", ", pTypes)}]";
            }
            return "this[]";
        }

        if (collectionName == "extensionsBlocks")
        {
            if (obj.TryGetPropertyValue("receiver", out JsonNode? recNode) && recNode is JsonObject recObj)
            {
                string mod = GetNonEmptyString(recObj, "modifiers") ?? "";
                JsonObject? typeObj = recObj["type"] as JsonObject;
                string type = (typeObj?["usageKind"]?.ToString() == "ByRef" ? typeObj["elementType"]?["localName"]?.ToString() : typeObj?["localName"]?.ToString()) ?? "?";
                string rName = GetNonEmptyString(recObj, "name") ?? "";
                string modPrefix = !string.IsNullOrEmpty(mod) && mod != "None" ? $"{mod} " : "";
                string nameSuffix = !string.IsNullOrEmpty(rName) ? $" {rName}" : "";
                return $"extension({modPrefix}{type}{nameSuffix})";
            }

            if (obj.TryGetPropertyValue("extendedType", out JsonNode? extTypeNode) && extTypeNode is JsonObject extTypeObj)
            {
                string type = GetNonEmptyString(extTypeObj, "localName") ?? "?";
                return $"extension({type})";
            }
        }

        return $"[{index}]";
    }

    static string? GetNonEmptyString(JsonObject obj, string propName)
    {
        if (obj.TryGetPropertyValue(propName, out JsonNode? node) && node is not null)
        {
            string str = node.ToString();
            if (!string.IsNullOrEmpty(str))
                return str;
        }
        return null;
    }

    static string SimplifyMemberName(string memberName, string currentNamespace)
    {
        if (string.IsNullOrEmpty(memberName))
            return memberName;

        string result = memberName;
        if (!string.IsNullOrEmpty(currentNamespace) && result.StartsWith(currentNamespace + ".", StringComparison.Ordinal))
            result = result[(currentNamespace.Length + 1)..];

        return result.Replace("System.String", "string", StringComparison.Ordinal)
                     .Replace("System.Int32", "int", StringComparison.Ordinal)
                     .Replace("System.UInt32", "uint", StringComparison.Ordinal)
                     .Replace("System.Int64", "long", StringComparison.Ordinal)
                     .Replace("System.UInt64", "ulong", StringComparison.Ordinal)
                     .Replace("System.Int16", "short", StringComparison.Ordinal)
                     .Replace("System.UInt16", "ushort", StringComparison.Ordinal)
                     .Replace("System.IntPtr", "nint", StringComparison.Ordinal)
                     .Replace("System.UIntPtr", "nuint", StringComparison.Ordinal)
                     .Replace("System.Boolean", "bool", StringComparison.Ordinal)
                     .Replace("System.Byte", "byte", StringComparison.Ordinal)
                     .Replace("System.SByte", "sbyte", StringComparison.Ordinal)
                     .Replace("System.Char", "char", StringComparison.Ordinal)
                     .Replace("System.Single", "float", StringComparison.Ordinal)
                     .Replace("System.Double", "double", StringComparison.Ordinal)
                     .Replace("System.Decimal", "decimal", StringComparison.Ordinal)
                     .Replace("System.Object", "object", StringComparison.Ordinal)
                     .Replace("System.Void", "void", StringComparison.Ordinal);
    }

    static void CompareValues(JsonValue expected, JsonValue actual, DiffContext ctx)
    {
        string expStr = expected.ToString();
        string actStr = actual.ToString();

        if (expStr != actStr)
            ctx.Record(expStr, actStr, $"Value mismatch: expected '{expStr}', got '{actStr}'");
    }
}
