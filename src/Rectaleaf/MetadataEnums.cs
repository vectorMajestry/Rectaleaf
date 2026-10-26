namespace Rectaleaf.Metadata;

[Flags]
public enum ParameterModifiers
{
    None,
    This,
    Ref = 1 << 1,
    RefReadOnly = 1 << 2,
    Out = 1 << 3,
    In = 1 << 4,
    Params = 1 << 5,
    Scoped = 1 << 6,
}

public enum PredefinedTypeKind
{
    Void, Bool, Int, UInt, Long, ULong, Float, Double, Byte, SByte,
    Short, UShort, Char, NInt, NUint, String, Object, Decimal
}

public enum PrimitiveTypeKind
{
    Void = PredefinedTypeKind.Void,
    Bool = PredefinedTypeKind.Bool,
    Int = PredefinedTypeKind.Int,
    Uint = PredefinedTypeKind.UInt,
    Long = PredefinedTypeKind.Long,
    ULong = PredefinedTypeKind.ULong,
    Float = PredefinedTypeKind.Float,
    Double = PredefinedTypeKind.Double,
    Byte = PredefinedTypeKind.Byte,
    SByte = PredefinedTypeKind.SByte,
    Short = PredefinedTypeKind.Short,
    UShort = PredefinedTypeKind.UShort,
    Char = PredefinedTypeKind.Char,
    NInt = PredefinedTypeKind.NInt,
    NUint = PredefinedTypeKind.NUint
}

public enum FixedSizeBufferTypeEnum
{
    Bool = PredefinedTypeKind.Bool,
    Byte = PredefinedTypeKind.Byte,
    SByte = PredefinedTypeKind.SByte,
    Short = PredefinedTypeKind.Short,
    UShort = PredefinedTypeKind.UShort,
    Int = PredefinedTypeKind.Int,
    UInt = PredefinedTypeKind.UInt,
    Long = PredefinedTypeKind.Long,
    ULong = PredefinedTypeKind.ULong,
    Char = PredefinedTypeKind.Char,
    Float = PredefinedTypeKind.Float,
    Double = PredefinedTypeKind.Double
}

public enum EnumUnderlyingTypeEnum
{
    Byte = PredefinedTypeKind.Byte,
    SByte = PredefinedTypeKind.SByte,
    Short = PredefinedTypeKind.Short,
    UShort = PredefinedTypeKind.UShort,
    Int = PredefinedTypeKind.Int,
    UInt = PredefinedTypeKind.UInt,
    Long = PredefinedTypeKind.Long,
    ULong = PredefinedTypeKind.ULong
}

[Flags]
public enum CallConvFlags : uint
{
    None = 0,
    Cdecl = 1 << CallConvKind.Cdecl,
    Stdcall = 1 << CallConvKind.Stdcall,
    Thiscall = 1 << CallConvKind.Thiscall,
    Fastcall = 1 << CallConvKind.Fastcall,
    Swift = 1 << CallConvKind.Swift,
    MemberFunction = 1 << CallConvKind.MemberFunction,
    SuppressGCTransition = 1 << CallConvKind.SuppressGCTransition,
}

public enum CallConvKind : int
{
    Cdecl = 0,
    Stdcall = 1,
    Thiscall = 2,
    Fastcall = 3,
    Swift = 4,
    MemberFunction = 5,
    SuppressGCTransition = 6,
}

public enum AccessModifier
{
    Public,
    Private,
    Protected,
    Internal,
    ProtectedInternal,
    PrivateProtected,
    File,
}

public enum NullabilityKind
{
    Nullable,
    NonNullable,
    Oblivious
}
public enum BinaryOperatorKind
{
    /// <summary>
    /// Represents the '+' operator.
    /// </summary>
    Add,
    /// <summary>
    /// Represents the '-' operator.
    /// </summary>
    Subtract,
    /// <summary>
    /// Represents the '*' operator.
    /// </summary>
    Multiply,
    /// <summary>
    /// Represents the '/' operator.
    /// </summary>
    Divide,
    /// <summary>
    /// Represents the '%' operator
    /// </summary>
    Remainder,
    /// <summary>
    /// Represents the '&lt;&lt;' operator.
    /// </summary>
    LeftShift,
    /// <summary>
    /// Represents the '&gt;&gt;' operator.
    /// </summary>
    RightShift,
    /// <summary>
    /// Represents the '&amp;' operator,
    /// </summary>
    BitwiseAnd,
    /// <summary>
    /// Represents the '|' operator.
    /// </summary>
    BitwiseOr,
    /// <summary>
    /// Represents the '^' operator.
    /// </summary>
    ExclusiveOr,
    /// <summary>
    /// Represents the '&amp;&amp;' operator.
    /// </summary>
    ConditionalAnd,
    /// <summary>
    /// Represents the '||' operator.
    /// </summary>
    ConditionalOr,
    /// <summary>
    /// Represents the '==' operator.
    /// </summary>
    Equals,
    /// <summary>
    /// Represents the '!=' operator.
    /// </summary>
    NotEquals,
    /// <summary>
    /// Represents the '&lt;' operator.
    /// </summary>
    LessThan,
    /// <summary>
    /// Represents the '&lt;=' operator.
    /// </summary>
    LessThanOrEqual,
    /// <summary>
    /// Represents the '&gt;=' operator.
    /// </summary>
    GreaterThanOrEqual,
    /// <summary>
    /// Represents the '&gt;' operator.
    /// </summary>
    GreaterThan,
    /// <summary>
    /// Represents the '&gt;&gt;&gt;' operator.
    /// </summary>
    UnsignedRightShift
}

public enum UnaryOperatorKind
{
    /// <summary>
    /// Represents the '~' operator.
    /// </summary>
    BitwiseNegation,

    /// <summary>
    /// Represents the '!' operator.
    /// </summary>
    Not,

    /// <summary>
    /// Represents the unary '+' operator.
    /// </summary>
    Plus,

    /// <summary>
    /// Represents the unary '-' operator.
    /// </summary>
    Minus,

    /// <summary>
    /// Represents the unary '++' operator.
    /// </summary>
    Increment,

    /// <summary>
    /// Represents the unary '--' operator.
    /// </summary>
    Decrement,

    /// <summary>
    /// Represents the 'true' operator (used to evaluate objects in conditionals).
    /// </summary>
    True,

    /// <summary>
    /// Represents the 'false' operator (used to evaluate objects in conditionals).
    /// </summary>
    False,

    /// <summary>
    /// Represents the '^' operator.
    /// </summary>
    Hat,
}

public enum ConversionOperatorKind
{
    Implicit,
    Explicit,
    CheckedExplicit,
}
