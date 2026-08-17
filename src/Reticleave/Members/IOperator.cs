using Reticleave.Metadata.TypeUsages.Interfaces;
using Reticleave.Components;

namespace Reticleave.Metadata.Members;

/// <summary>
/// Base interface for all user-defined operator declarations.
/// </summary>
public interface IOperator : IHasLocalFunctions
{
    AccessModifier AccessModifier { get; }
    IInheritanceKind<IOperator> Inheritance { get; }
}

/// <summary>
/// Represents a unary operator overload (e.g. +x, -x, !x, ++x, --x, true, false).
/// </summary>
public interface IUnaryOperator : IOperator
{
    UnaryOperatorKind Kind { get; }
    IParameter Operand { get; }
    bool IsChecked { get; }
    IReturnInfo<IKnownOrNot> ReturnInfo { get; }
}

/// <summary>
/// Represents a binary operator overload (e.g. a + b, a == b, a &amp; b, a &lt;&lt; b).
/// </summary>
public interface IBinaryOperator : IOperator
{
    BinaryOperatorKind Kind { get; }
    IParameter LeftOperand { get; }
    IParameter RightOperand { get; }
    bool IsChecked { get; }
    IReturnInfo<IKnownOrNot> ReturnInfo { get; }
}

/// <summary>
/// Represents a user-defined conversion operator.
/// </summary>
public interface IConversionOperator : IOperator
{
    ConversionOperatorKind Kind { get; }
    IParameter Operand { get; }
    IReturnInfo<IKnownOrNot> ReturnInfo { get; }
}

