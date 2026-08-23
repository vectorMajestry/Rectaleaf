using System.Runtime.CompilerServices;

namespace Rectaleaf.Metadata.TypeUsages.Interfaces;

public static class TypeUsageMiscExtensions
{
#pragma warning disable CS0618 // Type or member is obsolete
    extension(IBoundGenericType<IKnown, IHasGenericParameters> boundGeneric)
    {
        public IReadOnlyList<IComposableType<IKnown>> TypeArguments
            => Unsafe.As<IReadOnlyList<IComposableType<IKnown>>>(boundGeneric.TypeArgumentsMixed);
    }

    extension(IBoundGenericType<IIncomplete, IHasGenericParameters> boundGeneric)
    {
        public IReadOnlyList<IComposableType<IKnownOrNot>> TypeArguments => boundGeneric.TypeArgumentsMixed;
    }

    extension(IChainTupleType<IKnown> chainTuple)
    {
        public IReadOnlyList<IComposableType<IKnown>> ElementTypes
            => Unsafe.As<IReadOnlyList<IComposableType<IKnown>>>(chainTuple.ElementTypesMixed);
    }

    extension(IChainTupleType<IIncomplete> chainTuple)
    {
        public IReadOnlyList<IComposableType<IKnownOrNot>> ElementTypes => chainTuple.ElementTypesMixed;
    }
#pragma warning restore CS0618

    extension(IWrapperType<IIncomplete> wrapper)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IInnerType<IIncomplete> UnwrapIterative() => wrapper.UnwrapIterative();
    }

    extension(IWrapperType<IKnownOrNot> wrapper)
    {
        public IInnerType<IKnownOrNot> UnwrapIterative()
        {
            IComposableType<IKnownOrNot> current = wrapper;
            while (current is IWrapperType<IKnownOrNot> currentWrapper)
                current = currentWrapper.ElementType;
            return (IInnerType<IKnownOrNot>)current;
        }
    }
}
