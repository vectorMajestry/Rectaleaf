using Rectaleaf.Helpers;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using System.Runtime.CompilerServices;

namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public abstract class TupleType<TKind> : TypeUsage<TKind>, ITupleType<TKind>
    where TKind : IKnownOrNot
{
    public TupleType(IBoundGenericType<TKind, IStructType> underlyingValueTupleType, NullabilityKind nullability)
    {
        UnderlyingValueTupleType = underlyingValueTupleType;
        Nullability = nullability;
    }

    public override string LocalName
        => field ??= Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(UnderlyingValueTupleType.LocalName)
            : UnderlyingValueTupleType.LocalName;

    public IBoundGenericType<TKind, IStructType> UnderlyingValueTupleType { get; }
    public NullabilityKind Nullability { get; }

    public IReadOnlyList<IComposableType<IKnownOrNot>> ElementTypesMixed
        => field ??= ExtractElementTypes(UnderlyingValueTupleType);

#pragma warning disable CS0618 // Type or member is obsolete
    static IReadOnlyList<IComposableType<IKnownOrNot>> ExtractElementTypes(IBoundGenericType<TKind, IStructType> sourceTupleStruct)
    {
        const int DefaultValueTupleItemCount = 7;
        const int MaxValueTupleItemCount = 7;
        const int MaxValueTupleArity = MaxValueTupleItemCount + 1;
        const int RestIndex = MaxValueTupleArity - 1;

        int bufferSize = EstimateTupleItemCount(sourceTupleStruct);
        List<IComposableType<IKnownOrNot>> list = new(bufferSize);

        IBoundGenericType<TKind, IStructType> current = sourceTupleStruct;
        while (true)
        {
            var typeArgs = current.TypeArgumentsMixed;
            if (typeArgs.Count == MaxValueTupleArity && typeArgs[RestIndex] is IBoundGenericType<TKind, IStructType> nextTuple)
            {
                for (int i = 0; i < MaxValueTupleItemCount; i++)
                    list.Add(typeArgs[i]);

                current = nextTuple;
            }
            else
            {
                for (int i = 0; i < typeArgs.Count; i++)
                    list.Add(typeArgs[i]);

                return list;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int EstimateTupleItemCount(IBoundGenericType<TKind, IStructType> tupleStruct)
        {
            int count = 0;
            while (true)
            {
                if (tupleStruct is IFastGenericArity arityInfo)
                {
                    if (arityInfo.Arity == MaxValueTupleArity && tupleStruct.TypeArgumentsMixed[RestIndex] is IBoundGenericType<TKind, IStructType> nextTuple)
                    {
                        count += MaxValueTupleArity - 1;
                        tupleStruct = nextTuple;
                        continue;
                    }
                    else
                    {
                        count += arityInfo.Arity;
                        return count;
                    }
                }
                else
                {
                    return count + DefaultValueTupleItemCount;
                }
            }
        }
    }
#pragma warning restore CS0618
}
