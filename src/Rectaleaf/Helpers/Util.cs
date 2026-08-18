using Rectaleaf.Metadata;
using System.Text;

namespace Geyser.Generation.Helpers;

public static class Util
{
    public static string GetContainingTypes(Type type)
    {
        if (type.DeclaringType is null)
            return string.Empty;

        var segments = new List<string>();
        Type? current = type.DeclaringType;

        while (current is not null)
        {
            segments.Add(GetTypeCodeName(current));
            current = current.DeclaringType;
        }
        segments.Reverse();
        return string.Join(".", segments);
    }

    public static string GetTypeCodeName(Type type)
    {
        if (type.IsPointer)
        {
            return GetTypeCodeName(type.GetElementType()!) + "*";
        }
        if (type.IsByRef)
        {
            return "ref " + GetTypeCodeName(type.GetElementType()!);
        }

        if (type.IsGenericType)
        {
            ReadOnlySpan<char> genericTypeName = type.Name.AsSpan(0, type.Name!.IndexOf('`'));
            string genericArguments = string.Join(", ", type.GetGenericArguments().Select(GetTypeCodeName));
            return $"{genericTypeName.ToString()}<{genericArguments}>";
        }

        return type.Name switch
        {
            "Void" => "void",
            "Boolean" => "bool",
            "SByte" => "sbyte",
            "Byte" => "byte",
            "Int16" => "short",
            "UInt16" => "ushort",
            "Int32" => "int",
            "UInt32" => "uint",
            "Int64" => "long",
            "UInt64" => "ulong",
            "Single" => "float",
            "Double" => "double",
            "Char" => "char",
            "String" => "string",
            "IntPtr" => "nint",
            "UIntPtr" => "nuint",
            _ => type.Name!
        };
    }

    public static string GetTypeFullCodeName(Type type, bool dropLastGenericArgs = false)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        if (type.IsPrimitive) return type.Name switch
        {
            "Void" => "void",
            "Boolean" => "bool",
            "SByte" => "sbyte",
            "Byte" => "byte",
            "Int16" => "short",
            "UInt16" => "ushort",
            "Int32" => "int",
            "UInt32" => "uint",
            "Int64" => "long",
            "UInt64" => "ulong",
            "Single" => "float",
            "Double" => "double",
            "Char" => "char",
            "String" => "string",
            "IntPtr" => "nint",
            "UIntPtr" => "nuint",
            _ => type.Name!
        };

        if (type.IsArray)
        {
            int rank = type.GetArrayRank();
            string brackets = rank == 1 ? "[]" : $"[{new string(',', rank - 1)}]";
            return GetTypeFullCodeName(type.GetElementType()!) + brackets;
        }

        if (type.IsByRef)
        {
            return "ref " + GetTypeFullCodeName(type.GetElementType()!);
        }

        if (type.IsPointer)
        {
            return GetTypeFullCodeName(type.GetElementType()!) + "*";
        }

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(type.Namespace))
            sb.Append(type.Namespace).Append('.');

        Span<Type> genericArgs = type.GetGenericArguments();

        if (type.IsNested)
        {
            List<Type> declaringTypes = [];
            Type? declaringType = type.DeclaringType!;

            while (declaringType is not null)
            {
                declaringTypes.Add(declaringType);
                declaringType = declaringType.DeclaringType;
            }

            int genericArityConsumed = 0;

            for (int i = declaringTypes.Count - 1; i >= 0; i--)
            {
                declaringType = declaringTypes[i];

                string declTypeName = declaringType.Name;
                if (declaringType.IsGenericType)
                {
                    int genericArgCount = declaringType.GetGenericArguments().Length - genericArityConsumed;
                    AppendGenericName(sb, declaringType, genericArgs.Slice(0, genericArgCount));
                    genericArgs = genericArgs[genericArgCount..];
                    genericArityConsumed += genericArgCount;
                }
                else
                    sb.Append(declTypeName);

                sb.Append('.');
            }
        }

        if (type.IsGenericType)
        {
            if (dropLastGenericArgs)
                sb.Append(type.Name.AsSpan()[..type.Name.IndexOf('`')].ToString());
            else
                AppendGenericName(sb, type, genericArgs);
        }
        else
            sb.Append(type.Name);

        return sb.ToString();
    }

    public static void AppendGenericName(StringBuilder sb, Type t, Span<Type> genericArgs)
    {
        ReadOnlySpan<char> noBacktickName = t.Name.AsSpan()[..t.Name.IndexOf('`')];

        sb.Append(noBacktickName.ToString());

        sb.Append('<');
        sb.Append(GetTypeFullCodeName(genericArgs[0]));
        for (int j = 1; j < genericArgs.Length; j++)
            sb.Append(',').Append(GetTypeFullCodeName(genericArgs[j]));
        sb.Append('>');
    }
    public static string PredefinedTypeToString(PredefinedTypeKind t) => t switch
    {
        _ when t == PredefinedTypeKind.Void => "void",
        _ when t == PredefinedTypeKind.Bool => "bool",
        _ when t == PredefinedTypeKind.Byte => "byte",
        _ when t == PredefinedTypeKind.SByte => "sbyte",
        _ when t == PredefinedTypeKind.Char => "char",
        _ when t == PredefinedTypeKind.Short => "short",
        _ when t == PredefinedTypeKind.UShort => "ushort",
        _ when t == PredefinedTypeKind.Int => "int",
        _ when t == PredefinedTypeKind.UInt => "uint",
        _ when t == PredefinedTypeKind.Long => "long",
        _ when t == PredefinedTypeKind.ULong => "ulong",
        _ when t == PredefinedTypeKind.Float => "float",
        _ when t == PredefinedTypeKind.Double => "double",
        _ when t == PredefinedTypeKind.Decimal => "decimal",
        _ when t == PredefinedTypeKind.Object => "object",
        _ when t == PredefinedTypeKind.String => "string",
        _ when t == PredefinedTypeKind.NInt => "nint",
        _ when t == PredefinedTypeKind.NUint => "nuint",
        _ => throw new ArgumentOutOfRangeException($"Unknown predefined type: {t}."),
    };
}