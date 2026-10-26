using Microsoft.CodeAnalysis;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;

namespace Rectaleaf.Roslyn.DeclaredTypes;

internal abstract class RoslynEnumType : RoslynDeclaredType, IEnumType
{
    readonly RoslynTypeMembers members;
    IEnumUnderlyingType? underlyingType;

    public RoslynEnumType(INamedTypeSymbol symbol) : base(symbol)
    {
        members = new RoslynTypeMembers(symbol, this);
    }

    public abstract IReadOnlyList<IEnumMember> Members { get; }

    public IEnumUnderlyingType GetUnderlyingType() => underlyingType ??= ResolveUnderlyingType();

    public IReadOnlyList<IDeclaredType> GetNestedTypes() => members.GetNestedTypes();

    IEnumUnderlyingType ResolveUnderlyingType()
    {
        var special = symbol.EnumUnderlyingType?.SpecialType ?? SpecialType.System_Int32;
        var kind = RoslynSymbolMapping.ToEnumUnderlyingTypeEnum(special);
        return new EnumUnderlyingType(kind);
    }

    public static IEnumType Create(INamedTypeSymbol symbol)
    {
        var special = symbol.EnumUnderlyingType?.SpecialType ?? SpecialType.System_Int32;

        return special switch
        {
            SpecialType.System_Byte => new RoslynEnumType<byte>(symbol),
            SpecialType.System_SByte => new RoslynEnumType<sbyte>(symbol),
            SpecialType.System_Int16 => new RoslynEnumType<short>(symbol),
            SpecialType.System_UInt16 => new RoslynEnumType<ushort>(symbol),
            SpecialType.System_Int32 => new RoslynEnumType<int>(symbol),
            SpecialType.System_UInt32 => new RoslynEnumType<uint>(symbol),
            SpecialType.System_Int64 => new RoslynEnumType<long>(symbol),
            SpecialType.System_UInt64 => new RoslynEnumType<ulong>(symbol),
            _ => new RoslynEnumType<int>(symbol)
        };
    }
}

internal sealed class RoslynEnumType<T>(INamedTypeSymbol symbol) : RoslynEnumType(symbol), IEnumType<T>
    where T : struct
{
    IReadOnlyList<IEnumMember<T>>? typedMembers;

    public override IReadOnlyList<IEnumMember> Members => TypedMembers;

    public IReadOnlyList<IEnumMember<T>> TypedMembers => typedMembers ??= symbol.GetMembers()
        .OfType<IFieldSymbol>()
        .Where(f => f.HasConstantValue)
        .Select(f => (IEnumMember<T>)new RoslynEnumMember<T>(f))
        .ToList();

    IReadOnlyList<IEnumMember<T>> IEnumType<T>.Members => TypedMembers;
}

internal sealed class RoslynEnumMember<T>(IFieldSymbol symbol) : IEnumMember<T>
    where T : struct
{
    IReadOnlyList<IAttribute>? attributes;

    public string Name => symbol.Name;

    public T Value => (T)symbol.ConstantValue!;

    object IEnumMember.Value => symbol.ConstantValue!;

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}
