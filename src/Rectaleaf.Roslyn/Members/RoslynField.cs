using Microsoft.CodeAnalysis;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;
using Rectaleaf.Roslyn.TypeUsages;

namespace Rectaleaf.Roslyn.Members;

internal abstract class RoslynField<TKind>(IFieldSymbol symbol, IHasFields? declaringType = null) : IField<TKind>
    where TKind : IKnownOrNot
{
    protected readonly IFieldSymbol symbol = symbol;
    IHasFields? resolvedDeclaringType = declaringType;
    IReadOnlyList<IAttribute>? attributes;

    public string Name => symbol.Name;

    public AccessModifier AccessModifier => RoslynSymbolMapping.ToAccessModifier(symbol);

    public IHasFields DeclaringType => resolvedDeclaringType ??= (RectaleafRoslyn.FromTypeDefinition(symbol.ContainingType) as IHasFields)!;

    public IReadOnlyList<IAttribute> GetAttributes()
        => attributes ??= RoslynAttribute.FilterAndCreate(symbol.GetAttributes());

    public override bool Equals(object? obj) => throw new NotImplementedException();

    public override int GetHashCode() => throw new NotImplementedException();

    public static IField<IKnownOrNot> Create(IFieldSymbol symbol, IHasFields? declaringType = null)
    {
        if (symbol.IsConst)
            return new RoslynConstField(symbol, declaringType);

        if (symbol.IsFixedSizeBuffer)
            return new RoslynFixedSizeBufferField(symbol, declaringType);

        var typeUsage = RoslynTypeUsageFactory.Create(symbol.Type, symbol.NullableAnnotation, RoslynTypeUsageFactory.ExtractTypeSyntax(symbol));
        bool isIncomplete = typeUsage is ITypeUsage<IIncomplete>;

        if (symbol.RefKind != RefKind.None)
        {
            return isIncomplete
                ? CreateRefField<IIncomplete>(symbol, declaringType)
                : CreateRefField<IKnown>(symbol, declaringType);
        }

        if (symbol.IsStatic)
        {
            return isIncomplete
                ? new RoslynStaticField<IIncomplete>(symbol, declaringType)
                : new RoslynStaticField<IKnown>(symbol, declaringType);
        }

        return isIncomplete
            ? new RoslynInstanceField<IIncomplete>(symbol, declaringType)
            : new RoslynInstanceField<IKnown>(symbol, declaringType);
    }

    static IField<TTargetKind> CreateRefField<TTargetKind>(IFieldSymbol symbol, IHasFields? declaringType)
        where TTargetKind : IKnownOrNot
    {
        var elemUsage = (IComposableType<TTargetKind>)RoslynTypeUsageFactory.Create(symbol.Type, symbol.NullableAnnotation, RoslynTypeUsageFactory.ExtractTypeSyntax(symbol));

        if (symbol.RefKind == RefKind.RefReadOnly)
            return new RoslynRefField<TTargetKind, IReadOnly>(symbol, new ByRefType<TTargetKind, IReadOnly>(elemUsage), declaringType);

        return new RoslynRefField<TTargetKind, IMut>(symbol, new ByRefType<TTargetKind, IMut>(elemUsage), declaringType);
    }
}

internal sealed class RoslynConstField(IFieldSymbol symbol, IHasFields? declaringType = null)
    : RoslynField<IKnown>(symbol, declaringType), ConstField
{
    IComposableType<IKnown>? type;

    public IComposableType<IKnown> Type
        => type ??= (IComposableType<IKnown>)RoslynTypeUsageFactory.Create(symbol.Type, symbol.NullableAnnotation, RoslynTypeUsageFactory.ExtractTypeSyntax(symbol));

    public object GetConstantValue() => symbol.ConstantValue!;
}

internal sealed class RoslynFixedSizeBufferField(IFieldSymbol symbol, IHasFields? declaringType = null)
    : RoslynField<IKnown>(symbol, declaringType), FixedSizeBufferField
{
    IFixedSizeBufferType? type;

    public IFixedSizeBufferType Type => type ??= ResolveBufferType();

    public int Length => symbol.FixedSize;

    IFixedSizeBufferType ResolveBufferType()
    {
        var elemType = symbol.Type is IPointerTypeSymbol pts ? pts.PointedAtType : symbol.Type;
        var kind = RoslynSymbolMapping.ToFixedSizeBufferTypeEnum(elemType) ?? FixedSizeBufferTypeEnum.Int;
        return new FixedSizeBufferType(kind);
    }
}

internal sealed class RoslynRefField<TKind, TMutability>(
    IFieldSymbol symbol,
    IByRefType<TKind, TMutability> byRefType,
    IHasFields? declaringType = null)
    : RoslynField<TKind>(symbol, declaringType), IRefField<TKind, TMutability>
    where TKind : IKnownOrNot
    where TMutability : IMutOrNot
{
    public IByRefType<TKind, TMutability> Type => byRefType;
}

internal sealed class RoslynStaticField<TKind>(IFieldSymbol symbol, IHasFields? declaringType = null)
    : RoslynField<TKind>(symbol, declaringType), IStaticField<TKind>
    where TKind : IKnownOrNot
{
    IComposableType<TKind>? type;

    public IComposableType<TKind> Type
        => type ??= (IComposableType<TKind>)RoslynTypeUsageFactory.Create(symbol.Type, symbol.NullableAnnotation, RoslynTypeUsageFactory.ExtractTypeSyntax(symbol));

    public bool IsReadOnly => symbol.IsReadOnly;

    public bool IsVolatile => symbol.IsVolatile;
}

internal sealed class RoslynInstanceField<TKind>(IFieldSymbol symbol, IHasFields? declaringType = null)
    : RoslynField<TKind>(symbol, declaringType), IInstanceField<TKind>
    where TKind : IKnownOrNot
{
    IComposableType<TKind>? type;

    public IComposableType<TKind> Type
        => type ??= (IComposableType<TKind>)RoslynTypeUsageFactory.Create(symbol.Type, symbol.NullableAnnotation, RoslynTypeUsageFactory.ExtractTypeSyntax(symbol));

    public bool IsReadOnly => symbol.IsReadOnly;

    public bool IsVolatile => symbol.IsVolatile;

    public bool IsRequired => symbol.IsRequired;
}
