using Rectaleaf.Metadata;
using System.Text;

namespace Rectaleaf.Helpers;

public static class WrapperNameFormatter
{
    public static string ArrayName(string elementName) => $"{elementName}[]";
    public static string ArrayName(string elementName, int rank) => $"{elementName}[{new string(',', rank - 1)}]";

    public static string PointerName(string elementName) => $"{elementName}*";

    public static string RefName(string elementName) => $"ref {elementName}";
    public static string RefReadOnlyName(string elementName) => $"ref readonly {elementName}";

    public static string NullableName(string elementType) => $"{elementType}?";

    public static string GenericName(string baseName, IEnumerable<string> typeArguments)
        => $"{baseName}<{string.Join(", ", typeArguments)}>";

    //unsafe class CallConvFormatter
    //{
    //    const int FlagCount = 7;
    //    const int MaxFlagCount = 10; // Limits cache size (for n flags, the cache will have 2^n entries)
    //    const uint CombinationCount = 1u << FlagCount;
    //    const uint AllFlagsMask = (1u << FlagCount) - 1;
    //    static readonly string[] CallConvNames = new string[FlagCount]
    //    {
    //        nameof(CallConvFlags.Cdecl),
    //        nameof(CallConvFlags.Stdcall),
    //        nameof(CallConvFlags.Thiscall),
    //        nameof(CallConvFlags.Fastcall),
    //        nameof(CallConvFlags.Swift),
    //        nameof(CallConvFlags.MemberFunction),
    //        nameof(CallConvFlags.SuppressGCTransition),
    //    };

    //    static readonly string[] Cache;

    //    static readonly string[] DeBruijnOrderedNames = new string[32];

    //    const uint DeBruijnMultiplier = 0x077CB531u;

    //    const int CallConvListBufferSizeInChars = 512;

    //    static CallConvFormatter()
    //    {
    //        if (FlagCount > MaxFlagCount)
    //            throw new InvalidOperationException($"Too many calling conventions are allowed. " +
    //                $"Cache size exceeded the maximum of {1 << MaxFlagCount} entries ({MaxFlagCount} " +
    //                $"calling conventions). Adjust '{nameof(CallConvFormatter)}.{nameof(MaxFlagCount)}' " +
    //                $"or use a different formatter.");

    //        Cache = new string[CombinationCount];
    //        Cache[0] = string.Empty;

    //        // Calculating the maximum possible length of the comma separated list of calling
    //        // convention names assuming that non of them are mutually exclusive.
    //        // If it exceeds the limit (for example due to unusually long calling convention
    //        // names), an exception is thrown to avoid overflowing the stack buffer.
    //        // As long as the calling convention names are set up correctly, this should never happen.
    //        int maxPossibleLength = 0;
    //        for (int i = 0; i < CallConvNames.Length; i++)
    //            maxPossibleLength += CallConvNames[i].Length;
    //        maxPossibleLength += (CallConvNames.Length - 1) * 2; // Commas and spaces

    //        if (maxPossibleLength > CallConvListBufferSizeInChars)
    //            throw new InvalidOperationException($"A comma separated list of calling convention names " +
    //                $"may be too long to fit in the {CallConvListBufferSizeInChars}-character long buffer. " +
    //                $"This might be due to unusually long calling convention names.");

    //        // Ordering names according to the De Bruijn sequence to avoid converting
    //        // hashes to bit indices during lookup.
    //        for (int i = 0; i < CallConvNames.Length; i++)
    //        {
    //            uint hash = ((1u << i) * DeBruijnMultiplier) >> 27;
    //            DeBruijnOrderedNames[hash] = CallConvNames[i];
    //        }
    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public static string CallConvFlagsToString(CallConvFlags flags)
    //    {
    //        uint mask = (uint)flags;
    //        Debug.Assert(mask != 0, "Calling convention flags cannot be 0");

    //        if (mask > AllFlagsMask)
    //            ThrowInvalidFlags(mask);

    //        if ((mask & (mask - 1)) == 0) // If single call conv
    //        {
    //            uint hash = (mask * DeBruijnMultiplier) >> 27;
    //            return DeBruijnOrderedNames[hash];
    //        }

    //        string cached = Cache[mask];
    //        if (cached is not null)
    //            return cached;

    //        return FormatMultipleAndCache(flags);
    //    }

    //    [MethodImpl(MethodImplOptions.NoInlining)]
    //    static string FormatMultipleAndCache(CallConvFlags flags)
    //    {
    //        uint mask = (uint)flags;
    //        uint hash;

    //        ref string deBruijnNamesTable = ref DeBruijnOrderedNames[0];

    //        // A buffer for comma separated call conv names.
    //        char* bufferPtr = stackalloc char[CallConvListBufferSizeInChars];
    //        char* offsetPtr = bufferPtr;

    //        // Copy the first call conv name (no leading comma)
    //        uint lowestSetBit = mask & (uint)(-(int)mask);
    //        hash = (lowestSetBit * DeBruijnMultiplier) >> 27;
    //        mask ^= lowestSetBit; // Remove the lowest set bit from the mask

    //        string currentName = Unsafe.Add(ref deBruijnNamesTable, hash);
    //        fixed (char* namePtr = currentName) // Copy current name into the buffer
    //            for (int i = 0; i < currentName.Length; i++)
    //                *offsetPtr++ = namePtr[i];

    //        // Copy the remaining call conv names
    //        while (mask != 0)
    //        {
    //            lowestSetBit = mask & (uint)(-(int)mask);
    //            hash = (lowestSetBit * DeBruijnMultiplier) >> 27;
    //            mask ^= lowestSetBit; // Remove the lowest set bit from the mask

    //            *(uint*)offsetPtr = 0x0020002C; // ',' (0x002C) and ' ' (0x0020) in little-endian UTF-16
    //            offsetPtr += 2;

    //            currentName = Unsafe.Add(ref deBruijnNamesTable, hash);
    //            fixed (char* namePtr = currentName) // Copy current name into the buffer
    //                for (int i = 0; i < currentName.Length; i++)
    //                    *offsetPtr++ = namePtr[i];
    //        }

    //        string result = new string(bufferPtr, 0, (int)(offsetPtr - bufferPtr));
    //        Cache[(uint)flags] = result;
    //        return result;
    //    }

    //    [MethodImpl(MethodImplOptions.NoInlining)]
    //    static void ThrowInvalidFlags(uint mask) =>
    //    throw new ArgumentOutOfRangeException(nameof(mask), mask, "Undefined calling convention bit(s) set.");
    //}

    public static string ManagedFunctionPointerName(IEnumerable<string> parameterTypes, string returnType)
    {
        var sb = new StringBuilder("delegate*<");

        foreach (var paramType in parameterTypes)
            sb.Append(paramType).Append(", ");

        sb.Append(returnType).Append('>');
        return sb.ToString();
    }


    public static string UnmanagedFunctionPointerName(
        IEnumerable<string> parameterTypes,
        string returnType,
        IEnumerable<CallConvKind> callingConventions)
    {
        StringBuilder sb;
        using var ccEnumerable = callingConventions.GetEnumerator();

        if (!ccEnumerable.MoveNext())
            sb = new StringBuilder("delegate* unmanaged<");
        else
        {
            sb = new StringBuilder("delegate* unmanaged[")
                .Append(ccEnumerable.Current);

            while (ccEnumerable.MoveNext())
                sb.Append(", ").Append(ccEnumerable.Current);

            sb.Append("]<");
        }

        foreach (var paramType in parameterTypes)
            sb.Append(paramType).Append(", ");

        sb.Append(returnType).Append('>');
        return sb.ToString();
    }

    public static string RichTupleName(IEnumerable<(string Type, string? Name)> elements)
    {
        var sb = new StringBuilder("(");

        using var enumerator = elements.GetEnumerator();

        if (!enumerator.MoveNext())
            throw new ArgumentException("Rich tuples cannot be empty.", nameof(elements));

        // Append the first element without a leading comma
        sb.Append(enumerator.Current.Type);
        if (enumerator.Current.Name is not null)
            sb.Append(' ').Append(enumerator.Current.Name);

        // Append the remaining elements with leading commas
        while (enumerator.MoveNext())
        {
            sb.Append(", ").Append(enumerator.Current.Type);
            if (enumerator.Current.Name is not null)
                sb.Append(' ').Append(enumerator.Current.Name);
        }

        sb.Append(')');
        return sb.ToString();
    }
}
