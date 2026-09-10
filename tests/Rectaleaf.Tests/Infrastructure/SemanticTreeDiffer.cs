using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Rectaleaf.Tests.Infrastructure;

public sealed record Difference(
    string Path,
    string EntityDescription,
    string Expected,
    string Actual,
    string Message
);

public sealed class DiffReport
{
    public bool HasDifferences => Differences.Count > 0;
    public List<Difference> Differences { get; } = new();

    public void Add(string path, string entityDescription, string expected, string actual, string message)
    {
        Differences.Add(new Difference(path, entityDescription, expected, actual, message));
    }
}

public static class SemanticTreeDiffer
{
    public static DiffReport Compare(JsonNode? expected, JsonNode? actual, string rootName = "Root")
    {
        var report = new DiffReport();
        var context = new DiffContext(report);
        context.Push(rootName);
        CompareNodes(expected, actual, context);
        context.Pop();
        return report;
    }

    sealed class DiffContext
    {
        public DiffReport Report { get; }
        readonly List<string> pathSegments = new();

        public DiffContext(DiffReport report)
        {
            Report = report;
        }

        public void Push(string segment) => pathSegments.Add(segment);
        public void Pop()
        {
            if (pathSegments.Count > 0) pathSegments.RemoveAt(pathSegments.Count - 1);
        }

        public string CurrentPath => string.Join(".", pathSegments);

        public string GetCleanEntityPath()
        {
            var cleanSegments = pathSegments
                .Where(s => s is not ("nestedTypes" or "methods" or "localFunctions" or "fields" or "properties" or "indexers" or "events" or "parameters" or "instanceConstructors" or "operators" or "extensionsBlocks"))
                .ToList();
            return string.Join(".", cleanSegments);
        }

        public string GetCleanParentEntityPath()
        {
            var parentSegments = pathSegments.Count > 1 ? pathSegments.Take(pathSegments.Count - 1) : pathSegments;
            var cleanSegments = parentSegments
                .Where(s => s is not ("nestedTypes" or "methods" or "localFunctions" or "fields" or "properties" or "indexers" or "events" or "parameters" or "instanceConstructors" or "operators" or "extensionsBlocks"))
                .ToList();
            return string.Join(".", cleanSegments);
        }

        public void Record(string expected, string actual, string message)
        {
            Report.Add(CurrentPath, GetCleanEntityPath(), expected, actual, message);
        }

        public void RecordWithEntity(string entityDescription, string expected, string actual, string message)
        {
            Report.Add(CurrentPath, entityDescription, expected, actual, message);
        }
    }

    static void CompareNodes(JsonNode? expected, JsonNode? actual, DiffContext ctx)
    {
        if (expected == null && actual == null) return;

        if (expected == null)
        {
            ctx.Record("null", actual?.ToJsonString() ?? "null", $"Unexpected value at {ctx.GetCleanEntityPath()}: expected null, got {actual?.ToJsonString()}");
            return;
        }

        if (actual == null)
        {
            ctx.Record(expected.ToJsonString(), "null", $"Missing value at {ctx.GetCleanEntityPath()}: expected {expected.ToJsonString()}, got null");
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
        else
        {
            ctx.Record(expected.ToJsonString(), actual.ToJsonString(),
                $"Mismatched node kinds at {ctx.GetCleanEntityPath()}: expected {expected.GetType().Name}, got {actual.GetType().Name}");
        }
    }

    static void CompareObjects(JsonObject expected, JsonObject actual, DiffContext ctx)
    {
        // Check if this object represents a Parameter with nice formatting
        if (IsParameterObject(expected) || IsParameterObject(actual))
        {
            CompareParameterObjects(expected, actual, ctx);
            return;
        }

        // Compare expected properties
        foreach (var (propName, expChild) in expected)
        {
            ctx.Push(propName);
            if (actual.TryGetPropertyValue(propName, out var actChild))
            {
                CompareNodes(expChild, actChild, ctx);
            }
            else
            {
                ctx.Record(expChild?.ToJsonString() ?? "null", "<missing>", $"Missing property '{propName}' at {ctx.GetCleanEntityPath()}");
            }
            ctx.Pop();
        }

        // Check for unexpected extra properties in actual
        foreach (var (propName, actChild) in actual)
        {
            if (!expected.ContainsKey(propName))
            {
                ctx.Push(propName);
                ctx.Record("<none>", actChild?.ToJsonString() ?? "null", $"Unexpected extra property '{propName}' at {ctx.GetCleanEntityPath()}");
                ctx.Pop();
            }
        }
    }

    static bool IsParameterObject(JsonObject obj)
    {
        return obj.ContainsKey("name") && obj.ContainsKey("position") && obj.ContainsKey("modifiers") && obj.ContainsKey("type");
    }

    static void CompareParameterObjects(JsonObject expected, JsonObject actual, DiffContext ctx)
    {
        string expName = expected["name"]?.ToString() ?? "";
        string actName = actual["name"]?.ToString() ?? "";
        string expMod = expected["modifiers"]?.ToString() ?? "";
        string actMod = actual["modifiers"]?.ToString() ?? "";
        string expType = FormatTypeUsageNode(expected["type"]);
        string actType = FormatTypeUsageNode(actual["type"]);
        int position = expected["position"]?.GetValue<int>() ?? actual["position"]?.GetValue<int>() ?? 0;

        string expFormatted = FormatParameterSignature(expMod, expType, expName);
        string actFormatted = FormatParameterSignature(actMod, actType, actName);

        if (expFormatted != actFormatted)
        {
            string parentPath = ctx.GetCleanParentEntityPath();
            string entityDesc = $"Parameter {position} of {parentPath}";
            string message = $"{entityDesc} has to be '{expFormatted}'; got '{actFormatted}'.";
            ctx.RecordWithEntity(entityDesc, expFormatted, actFormatted, message);
        }

        // Also recursively compare other parameter details (attributes, default value)
        foreach (var (propName, expChild) in expected)
        {
            if (propName is "name" or "modifiers" or "type" or "position") continue;

            ctx.Push(propName);
            if (actual.TryGetPropertyValue(propName, out var actChild))
            {
                CompareNodes(expChild, actChild, ctx);
            }
            else
            {
                ctx.Record(expChild?.ToJsonString() ?? "null", "<missing>", $"Missing property '{propName}' on parameter {position} at {ctx.GetCleanEntityPath()}");
            }
            ctx.Pop();
        }
    }

    static string FormatParameterSignature(string modifiers, string type, string name)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(modifiers) && modifiers != "None")
        {
            parts.Add(modifiers);
        }
        if (!string.IsNullOrWhiteSpace(type))
        {
            parts.Add(type);
        }
        if (!string.IsNullOrWhiteSpace(name))
        {
            parts.Add(name);
        }
        return string.Join(" ", parts);
    }

    static string FormatTypeUsageNode(JsonNode? typeNode)
    {
        if (typeNode == null) return "void";
        if (typeNode is JsonObject obj && obj.TryGetPropertyValue("localName", out var localName) && localName != null)
        {
            return localName.ToString();
        }
        return typeNode.ToString();
    }

    static void CompareArrays(JsonArray expected, JsonArray actual, DiffContext ctx)
    {
        int maxCount = Math.Max(expected.Count, actual.Count);
        if (expected.Count != actual.Count)
        {
            ctx.Record($"Count = {expected.Count}", $"Count = {actual.Count}",
                $"Collection count mismatch at {ctx.GetCleanEntityPath()}: expected {expected.Count} elements, got {actual.Count} elements.");
        }

        for (int i = 0; i < maxCount; i++)
        {
            var expItem = i < expected.Count ? expected[i] : null;
            var actItem = i < actual.Count ? actual[i] : null;

            string segment = GetItemSegmentName(expItem ?? actItem, i);
            ctx.Push(segment);
            CompareNodes(expItem, actItem, ctx);
            ctx.Pop();
        }
    }

    static string GetItemSegmentName(JsonNode? item, int index)
    {
        if (item is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("localName", out var localName) && localName != null)
            {
                return localName.ToString();
            }
            if (obj.TryGetPropertyValue("name", out var name) && name != null)
            {
                return name.ToString();
            }
        }
        return $"[{index}]";
    }

    static void CompareValues(JsonValue expected, JsonValue actual, DiffContext ctx)
    {
        string expStr = expected.ToString();
        string actStr = actual.ToString();

        if (expStr != actStr)
        {
            ctx.Record(expStr, actStr, $"Value mismatch at {ctx.GetCleanEntityPath()}: expected '{expStr}', got '{actStr}'");
        }
    }
}
