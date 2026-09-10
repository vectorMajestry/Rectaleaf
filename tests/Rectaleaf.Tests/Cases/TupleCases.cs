namespace TestCases;

using System;
using System.Collections.Generic;

public class TupleTestContainer
{
    // Rich tuple with fully named elements
    public (int code, string message) RichTupleField;

    // Partially named tuple
    public (int, string name, bool isFlag) PartiallyNamedTuple;

    // Explicit ValueTuple syntax (no TupleElementNamesAttribute in metadata)
    public ValueTuple<int, string> ExplicitValueTupleField;

    // Long tuple (> 7 elements, lower to ValueTuple with Rest chains)
    public (int a, int b, int c, int d, int e, int f, int g, int h, int i) LongTuple9;

    // Nested tuple testing preorder depth-first attribute decoding
    public (int a, (string b, int c) nested) NestedTupleField;

    // Deeply nested tuple with multiple levels of nesting
    public (int id, (string first, (int subId, string subName) inner) meta, double score) DeeplyNestedTuple;

    // Explicit names colliding with default Item1/Item2 naming pattern
    public (int Item1, string item2, bool Item3) DefaultNameCollisionTuple;

    // Rich tuple with custom item0 naming
    public (int item0, bool anotherItem) CustomItem0Tuple { get; set; }

    // Tuple as method parameter and return value
    public (int statusCode, string statusMessage, (bool ok, string detail) details) ProcessRequest(
        (string requestId, DateTime timestamp) header,
        (int code, (string first, string last) user, double rating) payload)
    {
        return (200, "OK", (true, $"{header.requestId}:{payload.user.first}"));
    }

    // Tuples as generic type arguments
    public List<(string key, int count)> TupleListField = new();

    public Dictionary<(int x, int y), (string tag, bool isVisible)> TupleKeyedDictionary = new();

    // Tuple with nullable value types and reference types
    public (int? nullableInt, string? nullableString, (bool? flag, object? obj) nested) NullableTupleField;
}
