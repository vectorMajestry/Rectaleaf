using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using Rectaleaf.Roslyn.Symbols;

namespace Rectaleaf.Roslyn.Members;

internal abstract class RoslynOperator(IMethodSymbol symbol) : IOperator
{
    protected readonly IMethodSymbol symbol = symbol;
    IInheritanceKind<IOperator>? inheritance;
    IReadOnlyList<ILocalFunction>? localFunctions;

    public AccessModifier AccessModifier => RoslynSymbolMapping.ToAccessModifier(symbol);

    public IInheritanceKind<IOperator> Inheritance => inheritance ??= ResolveInheritance();

    public IReadOnlyList<ILocalFunction> GetLocalFunctions()
        => localFunctions ??= RoslynLocalFunctionHelper.ExtractLocalFunctions(symbol, this);

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();

    IInheritanceKind<IOperator> ResolveInheritance()
    {
        if (symbol.IsAbstract)
            return RoslynInheritanceAbstract<IOperator>.Instance;

        if (symbol.IsVirtual)
            return RoslynInheritanceVirtual<IOperator>.Instance;

        return RoslynInheritanceNone<IOperator>.Instance;
    }

    public static IOperator? Create(IMethodSymbol symbol)
    {
        var unaryKind = RoslynSymbolMapping.ToUnaryOperatorKind(symbol.Name);
        if (unaryKind.HasValue)
            return new RoslynUnaryOperator(symbol, unaryKind.Value);

        var binaryKind = RoslynSymbolMapping.ToBinaryOperatorKind(symbol.Name);
        if (binaryKind.HasValue)
            return new RoslynBinaryOperator(symbol, binaryKind.Value);

        var convKind = RoslynSymbolMapping.ToConversionOperatorKind(symbol.Name);
        if (convKind.HasValue)
            return new RoslynConversionOperator(symbol, convKind.Value);

        return null;
    }
}

internal sealed class RoslynUnaryOperator(IMethodSymbol symbol, UnaryOperatorKind kind) : RoslynOperator(symbol), IUnaryOperator
{
    IParameter? operand;
    IReturnInfo<IKnownOrNot>? returnInfo;

    public UnaryOperatorKind Kind => kind;

    public IParameter Operand => operand ??= new RoslynParameter(symbol.Parameters[0]);

    public bool IsChecked => RoslynSymbolMapping.IsCheckedOperator(symbol.Name);

    public IReturnInfo<IKnownOrNot> ReturnInfo => returnInfo ??= new RoslynReturnInfo<IKnownOrNot>(symbol);
}

internal sealed class RoslynBinaryOperator(IMethodSymbol symbol, BinaryOperatorKind kind) : RoslynOperator(symbol), IBinaryOperator
{
    IParameter? leftOperand;
    IParameter? rightOperand;
    IReturnInfo<IKnownOrNot>? returnInfo;

    public BinaryOperatorKind Kind => kind;

    public IParameter LeftOperand => leftOperand ??= new RoslynParameter(symbol.Parameters[0]);

    public IParameter RightOperand => rightOperand ??= new RoslynParameter(symbol.Parameters[1]);

    public bool IsChecked => RoslynSymbolMapping.IsCheckedOperator(symbol.Name);

    public IReturnInfo<IKnownOrNot> ReturnInfo => returnInfo ??= new RoslynReturnInfo<IKnownOrNot>(symbol);
}

internal sealed class RoslynConversionOperator(IMethodSymbol symbol, ConversionOperatorKind kind) : RoslynOperator(symbol), IConversionOperator
{
    IParameter? operand;
    IReturnInfo<IKnownOrNot>? returnInfo;

    public ConversionOperatorKind Kind => kind;

    public IParameter Operand => operand ??= new RoslynParameter(symbol.Parameters[0]);

    public IReturnInfo<IKnownOrNot> ReturnInfo => returnInfo ??= new RoslynReturnInfo<IKnownOrNot>(symbol);
}
