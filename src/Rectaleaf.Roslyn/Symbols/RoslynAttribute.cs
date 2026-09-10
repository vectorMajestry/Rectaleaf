using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.TypeUsages;

namespace Rectaleaf.Roslyn.Symbols;

internal sealed class RoslynAttribute(AttributeData data) : IAttribute
{
    IReadOnlyList<AttributeValue>? constructorArguments;
    IReadOnlyDictionary<string, AttributeValue>? namedArguments;

    public string Name => data.AttributeClass?.Name ?? string.Empty;

    public string FullyQualifiedName => data.AttributeClass?.ToDisplayString() ?? string.Empty;

    public IReadOnlyList<AttributeValue> ConstructorArguments
        => constructorArguments ??= data.ConstructorArguments.Select(ToAttributeValue).ToList();

    public IReadOnlyDictionary<string, AttributeValue> NamedArguments
        => namedArguments ??= data.NamedArguments.ToDictionary(kvp => kvp.Key, kvp => ToAttributeValue(kvp.Value));

    public static bool IsCompilerInternalAttribute(AttributeData attr)
    {
        var cls = attr.AttributeClass;
        if (cls is null)
            return false;

        string name = cls.Name;

        if (name is "NullableAttribute"
            or "NullableContextAttribute"
            or "TupleElementNamesAttribute"
            or "IsReadOnlyAttribute"
            or "IsByRefLikeAttribute"
            or "IsUnmanagedAttribute"
            or "ParamArrayAttribute"
            or "ScopedRefAttribute"
            or "InAttribute"
            or "OutAttribute"
            or "RequiredMemberAttribute"
            or "CompilerFeatureRequiredAttribute"
            or "CompilerGeneratedAttribute"
            or "RefSafetyRulesAttribute"
            or "AsyncStateMachineAttribute"
            or "IteratorStateMachineAttribute"
            or "AsyncIteratorStateMachineAttribute")
            return true;

        if (name is "ObsoleteAttribute")
        {
            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string msg)
            {
                if (msg is "Types with embedded references are not supported in this version of your compiler."
                    or "Constructors of types with required members are not supported in this version of your compiler.")
                    return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<IAttribute> FilterAndCreate(ImmutableArray<AttributeData> attributes)
    {
        if (attributes.IsDefaultOrEmpty)
            return Array.Empty<IAttribute>();

        List<IAttribute>? list = null;
        for (int i = 0; i < attributes.Length; i++)
        {
            var attr = attributes[i];
            if (IsCompilerInternalAttribute(attr))
                continue;

            list ??= new List<IAttribute>(attributes.Length - i);
            list.Add(new RoslynAttribute(attr));
        }

        return (IReadOnlyList<IAttribute>?)list ?? Array.Empty<IAttribute>();
    }

    static AttributeValue ToAttributeValue(TypedConstant constant)
    {
        if (constant.Kind == TypedConstantKind.Array)
            return new AttributeValueArray(constant.Values.Select(v => (AttributeValueUnit)ToAttributeValue(v)).ToList());

        if (constant.Kind == TypedConstantKind.Enum)
        {
            var enumType = constant.Type is INamedTypeSymbol nts ? RectaleafRoslyn.FromSymbol(nts) as IEnumType : null;
            return new AttributeValueEnum(enumType!, constant.Value);
        }

        if (constant.Kind == TypedConstantKind.Type)
        {
            var targetType = constant.Value is ITypeSymbol ts ? RoslynTypeUsageFactory.Create(ts) as ITypeUsage<IKnown> : null;
            return new AttributeValueType(targetType);
        }

        var predKind = constant.Type is not null
            ? RoslynSymbolMapping.ToPredefinedTypeKind(constant.Type.SpecialType) ?? PredefinedTypeKind.Object
            : PredefinedTypeKind.Object;

        return new AttributeValuePrimitive(new PredefinedType(predKind, NullabilityKind.NonNullable), constant.Value);
    }

    public override bool Equals(object? obj) => throw new NotImplementedException();

    public override int GetHashCode() => throw new NotImplementedException();
}

