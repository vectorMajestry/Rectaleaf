namespace Rectaleaf.Metadata;

[Flags]
public enum ParameterModifiers
{
    None = 0,
    This = 1,
    Ref = 1<< 1,
    RefReadOnly = 1<< 2,
    Out = 1<< 3,
    In = 1<< 4,
    Params = 1<< 5,
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

public enum AccessModifier
{
    Public,
    Private,
    Protected,
    Internal,
    ProtectedInternal,
    PrivateProtected
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
    And,
    /// <summary>
    /// Represents the '|' operator.
    /// </summary>
    Or,
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
    BitwiseNegation = 0x1,

    /// <summary>
    /// Represents the '!' operator.
    /// </summary>
    Not = 0x2,

    /// <summary>
    /// Represents the unary '+' operator.
    /// </summary>
    Plus = 0x3,

    /// <summary>
    /// Represents the unary '-' operator.
    /// </summary>
    Minus = 0x4,

    /// <summary>
    /// Represents the 'true' operator.
    /// </summary>
    True = 0x5,

    /// <summary>
    /// Represents the 'false' operator.
    /// </summary>
    False = 0x6,

    /// <summary>
    /// Represents the '^' operator.
    /// </summary>
    Hat = 0x7,
}

public enum ConversionOperatorKind
{
    Implicit,
    Explicit,
    CheckedExplicit,
}
