#nullable enable
namespace TestCases;

using System;
using System.Collections.Generic;

public class NullableTopologyContainer
{
    // Deeply nested generic nullability combinations
    public Dictionary<string?, List<int?[]?>>? DeeplyNestedGenericNullable;

    public Dictionary<string, List<string?>?> NonNullKeyNullableNestedList;

    public KeyValuePair<string?, (int? id, object? data)> NullableKeyValuePair;

    // Array nullability layers
    public string?[]? NullableArrayOfNullableStrings;

    public string?[] NonNullableArrayOfNullableStrings = Array.Empty<string?>();

    public string[]? NullableArrayOfNonNullableStrings;

    public string?[,] NonNullable2DArrayOfNullableStrings = new string?[0, 0];

    public string?[]?[]? JaggedArrayThreeLayersOfNullability;

    // Complex tuple nullability topologies
    public (string? text, object nonNullObj, (int? id, string? name)? nestedNullableTuple)? RichNullableTuple;

    // Method with varied parameter and return nullabilities
    public Dictionary<string, (int code, string? desc)>? ProcessPayload(
        ref readonly (string? id, object? state)? inputHeader,
        in List<int?[]?> dataMatrix,
        out string? errorReason)
    {
        errorReason = null;
        return null;
    }

    // Generic class handling unconstrained T? vs class? constraints
    public class GenericNullableResolver<TUnconstrained, TClassRef>
        where TClassRef : class?
    {
        public TUnconstrained? MaybeValue;
        public TClassRef NullableClassField = default!;

        public TResult? ConvertTo<TResult>(TUnconstrained? item) where TResult : notnull
        {
            return default;
        }
    }
}

// Oblivious nullable context declared with #nullable disable
#nullable disable
public class NullableObliviousContainer
{
    public string ObliviousStringField;
    public object[] ObliviousArray;
    public Dictionary<string, object> ObliviousDictionary;

    public string ObliviousMethod(string param1, object param2)
    {
        return param1;
    }
}
#nullable enable
