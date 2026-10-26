using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rectaleaf.Metadata;

namespace Rectaleaf.Roslyn.Symbols;

internal static class RoslynSymbolMapping
{
    public static AccessModifier ToAccessModifier(ISymbol symbol)
    {
        if (IsFileLocal(symbol))
            return AccessModifier.File;

        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => AccessModifier.Public,
            Accessibility.Private => AccessModifier.Private,
            Accessibility.Protected => AccessModifier.Protected,
            Accessibility.Internal => AccessModifier.Internal,
            Accessibility.ProtectedOrInternal => AccessModifier.ProtectedInternal,
            Accessibility.ProtectedAndInternal => AccessModifier.PrivateProtected,
            _ => AccessModifier.Private
        };
    }

    public static bool IsFileLocal(ISymbol symbol)
    {
        if (symbol is INamedTypeSymbol namedType)
        {
            if (namedType.MetadataName.StartsWith("<", StringComparison.Ordinal) && namedType.MetadataName.Contains("__"))
                return true;

            foreach (var synRef in namedType.DeclaringSyntaxReferences)
            {
                if (synRef.GetSyntax() is BaseTypeDeclarationSyntax btd && btd.Modifiers.Any(SyntaxKind.FileKeyword))
                    return true;
            }
        }

        return false;
    }

    public static PredefinedTypeKind? ToPredefinedTypeKind(SpecialType specialType) => specialType switch
    {
        SpecialType.System_Void => PredefinedTypeKind.Void,
        SpecialType.System_Boolean => PredefinedTypeKind.Bool,
        SpecialType.System_Int32 => PredefinedTypeKind.Int,
        SpecialType.System_UInt32 => PredefinedTypeKind.UInt,
        SpecialType.System_Int64 => PredefinedTypeKind.Long,
        SpecialType.System_UInt64 => PredefinedTypeKind.ULong,
        SpecialType.System_Single => PredefinedTypeKind.Float,
        SpecialType.System_Double => PredefinedTypeKind.Double,
        SpecialType.System_Byte => PredefinedTypeKind.Byte,
        SpecialType.System_SByte => PredefinedTypeKind.SByte,
        SpecialType.System_Int16 => PredefinedTypeKind.Short,
        SpecialType.System_UInt16 => PredefinedTypeKind.UShort,
        SpecialType.System_Char => PredefinedTypeKind.Char,
        SpecialType.System_IntPtr => PredefinedTypeKind.NInt,
        SpecialType.System_UIntPtr => PredefinedTypeKind.NUint,
        SpecialType.System_String => PredefinedTypeKind.String,
        SpecialType.System_Object => PredefinedTypeKind.Object,
        SpecialType.System_Decimal => PredefinedTypeKind.Decimal,
        _ => null
    };

    public static FixedSizeBufferTypeEnum? ToFixedSizeBufferTypeEnum(ITypeSymbol type) => type.SpecialType switch
    {
        SpecialType.System_Boolean => FixedSizeBufferTypeEnum.Bool,
        SpecialType.System_Byte => FixedSizeBufferTypeEnum.Byte,
        SpecialType.System_SByte => FixedSizeBufferTypeEnum.SByte,
        SpecialType.System_Int16 => FixedSizeBufferTypeEnum.Short,
        SpecialType.System_UInt16 => FixedSizeBufferTypeEnum.UShort,
        SpecialType.System_Int32 => FixedSizeBufferTypeEnum.Int,
        SpecialType.System_UInt32 => FixedSizeBufferTypeEnum.UInt,
        SpecialType.System_Int64 => FixedSizeBufferTypeEnum.Long,
        SpecialType.System_UInt64 => FixedSizeBufferTypeEnum.ULong,
        SpecialType.System_Char => FixedSizeBufferTypeEnum.Char,
        SpecialType.System_Single => FixedSizeBufferTypeEnum.Float,
        SpecialType.System_Double => FixedSizeBufferTypeEnum.Double,
        _ => null
    };

    public static EnumUnderlyingTypeEnum ToEnumUnderlyingTypeEnum(SpecialType specialType) => specialType switch
    {
        SpecialType.System_Byte => EnumUnderlyingTypeEnum.Byte,
        SpecialType.System_SByte => EnumUnderlyingTypeEnum.SByte,
        SpecialType.System_Int16 => EnumUnderlyingTypeEnum.Short,
        SpecialType.System_UInt16 => EnumUnderlyingTypeEnum.UShort,
        SpecialType.System_Int32 => EnumUnderlyingTypeEnum.Int,
        SpecialType.System_UInt32 => EnumUnderlyingTypeEnum.UInt,
        SpecialType.System_Int64 => EnumUnderlyingTypeEnum.Long,
        SpecialType.System_UInt64 => EnumUnderlyingTypeEnum.ULong,
        _ => EnumUnderlyingTypeEnum.Int
    };

    public static CallConvKind? ToCallConvKind(string name) => name switch
    {
        "CallConvCdecl" or "Cdecl" => CallConvKind.Cdecl,
        "CallConvStdcall" or "Stdcall" => CallConvKind.Stdcall,
        "CallConvThiscall" or "Thiscall" => CallConvKind.Thiscall,
        "CallConvFastcall" or "Fastcall" => CallConvKind.Fastcall,
        "CallConvSwift" or "Swift" => CallConvKind.Swift,
        "CallConvMemberFunction" or "MemberFunction" => CallConvKind.MemberFunction,
        "CallConvSuppressGCTransition" or "SuppressGCTransition" => CallConvKind.SuppressGCTransition,
        _ => null
    };

    public static CallConvFlags ToCallConvFlags(IEnumerable<CallConvKind> kinds)
    {
        uint flags = 0;
        foreach (var kind in kinds)
            flags |= 1u << (int)kind;
        return (CallConvFlags)flags;
    }


    public static NullabilityKind ToNullabilityKind(NullableAnnotation annotation) => annotation switch
    {
        NullableAnnotation.Annotated => NullabilityKind.Nullable,
        NullableAnnotation.NotAnnotated => NullabilityKind.NonNullable,
        _ => NullabilityKind.Oblivious
    };

    public static bool IsCheckedOperator(string methodName)
        => methodName.StartsWith("op_Checked", StringComparison.Ordinal);

    // TODO: add increment/decrement
    public static UnaryOperatorKind? ToUnaryOperatorKind(string methodName) => methodName switch
    {
        "op_UnaryPlus" => UnaryOperatorKind.Plus,
        "op_UnaryNegation" or "op_CheckedUnaryNegation" => UnaryOperatorKind.Minus,
        "op_LogicalNot" => UnaryOperatorKind.Not,
        "op_OnesComplement" => UnaryOperatorKind.BitwiseNegation,
        "op_True" => UnaryOperatorKind.True,
        "op_False" => UnaryOperatorKind.False,
        "op_Hat" => UnaryOperatorKind.Hat,
        _ => null
    };

    public static BinaryOperatorKind? ToBinaryOperatorKind(string methodName) => methodName switch
    {
        "op_Addition" or "op_CheckedAddition" => BinaryOperatorKind.Add,
        "op_Subtraction" or "op_CheckedSubtraction" => BinaryOperatorKind.Subtract,
        "op_Multiply" or "op_CheckedMultiply" => BinaryOperatorKind.Multiply,
        "op_Division" or "op_CheckedDivision" => BinaryOperatorKind.Divide,
        "op_Modulus" => BinaryOperatorKind.Remainder,
        "op_LeftShift" => BinaryOperatorKind.LeftShift,
        "op_RightShift" => BinaryOperatorKind.RightShift,
        "op_UnsignedRightShift" => BinaryOperatorKind.UnsignedRightShift,
        "op_BitwiseAnd" => BinaryOperatorKind.BitwiseAnd,
        "op_BitwiseOr" => BinaryOperatorKind.BitwiseOr,
        "op_ExclusiveOr" => BinaryOperatorKind.ExclusiveOr,
        "op_Equality" => BinaryOperatorKind.Equals,
        "op_Inequality" => BinaryOperatorKind.NotEquals,
        "op_LessThan" => BinaryOperatorKind.LessThan,
        "op_LessThanOrEqual" => BinaryOperatorKind.LessThanOrEqual,
        "op_GreaterThan" => BinaryOperatorKind.GreaterThan,
        "op_GreaterThanOrEqual" => BinaryOperatorKind.GreaterThanOrEqual,
        _ => null
    };

    public static ConversionOperatorKind? ToConversionOperatorKind(string methodName) => methodName switch
    {
        "op_Implicit" => ConversionOperatorKind.Implicit,
        "op_Explicit" => ConversionOperatorKind.Explicit,
        "op_CheckedExplicit" => ConversionOperatorKind.CheckedExplicit,
        _ => null
    };
}
