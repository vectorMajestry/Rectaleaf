using Rectaleaf.Helpers;
using Rectaleaf.Metadata.DeclaredTypes;

namespace Rectaleaf.Metadata.TypeUsages.Interfaces;

public static class TypeUsageMaxNameExtensions
{
    extension(ITypeUsage<IKnownOrNot> type)
    {
        public string MaximumQualifiedName => type switch
        {
            IComposableType<IKnownOrNot> composable => composable.MaximumQualifiedName,
            IByRefType<IKnown, IMutOrNot> byRefKnown => byRefKnown.FullyQualifiedName,
            IByRefType<IIncomplete, IMutOrNot> byRefIncomplete => byRefIncomplete.MaximumQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IComposableType<IKnownOrNot> composable)
    {
        public string MaximumQualifiedName => composable switch
        {
            IComposableType<IKnown> known => known.FullyQualifiedName,
            IComposableType<IIncomplete> incomplete => incomplete.MaximumQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(ITypeUsage<IIncomplete> incompleteType)
    {
        public string MaximumQualifiedName => incompleteType switch
        {
            IByRefType<IIncomplete, IMutOrNot> byRef => byRef.MaximumQualifiedName,
            IComposableType<IIncomplete> composable => composable.MaximumQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IByRefType<IIncomplete, IMut> byrefMut)
    {
        public string MaximumQualifiedName => WrapperNameFormatter.RefName(byrefMut.ElementType.MaximumQualifiedName);
    }

    extension(IByRefType<IIncomplete, IReadOnly> byrefReadOnly)
    {
        public string MaximumQualifiedName => WrapperNameFormatter.RefReadOnlyName(byrefReadOnly.ElementType.MaximumQualifiedName);
    }

    extension(IPointerType<IIncomplete> pointer)
    {
        public string MaximumQualifiedName => WrapperNameFormatter.PointerName(pointer.ElementType.MaximumQualifiedName);
    }

    extension(IArrayType<IIncomplete, IFlat> linearArray)
    {
        public string MaximumQualifiedName => linearArray.Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(WrapperNameFormatter.ArrayName(linearArray.ElementType.MaximumQualifiedName))
            : WrapperNameFormatter.ArrayName(linearArray.ElementType.MaximumQualifiedName);
    }

    extension(IArrayType<IIncomplete, IRectArr> rectArray)
    {
        public string MaximumQualifiedName => rectArray.Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(WrapperNameFormatter.ArrayName(rectArray.ElementType.MaximumQualifiedName, rectArray.Rank))
            : WrapperNameFormatter.ArrayName(rectArray.ElementType.MaximumQualifiedName, rectArray.Rank);
    }

    extension(IArrayType<IIncomplete, IFlatOrNot> array)
    {
        public string MaximumQualifiedName => array switch
        {
            IArrayType<IIncomplete, IFlat> flat => flat.MaximumQualifiedName,
            IArrayType<IIncomplete, IRectArr> rect => rect.MaximumQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IBoundGenericType<IIncomplete, IHasGenericParameters> boundGeneric)
    {
#pragma warning disable CS0618 // obsolete
        public string MaximumQualifiedName
        {
            get
            {
                string baseName = WrapperNameFormatter.GenericName(
                    baseName: boundGeneric.TypeDefinition.FullyQualifiedName,
                    typeArguments: boundGeneric.TypeArgumentsMixed.Select(t => t.MaximumQualifiedName));

                return boundGeneric.Nullability is NullabilityKind.Nullable
                    ? WrapperNameFormatter.NullableName(baseName)
                    : baseName;
            }
        }
#pragma warning restore CS0618 // obsolete
    }

    extension(IInnerType<IIncomplete> inner)
    {
        public string MaximumQualifiedName => inner switch
        {
            IPlaceholderType p => p.LocalName,
            IDeclTypeUsage<IIncomplete> decl => decl.MaximumQualifiedName,
            IFunctionPointerType<IIncomplete> funcPtr => funcPtr.MaximumQualifiedName,
            ITupleType<IIncomplete> tuple => tuple.MaximumQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IByRefType<IIncomplete, IMutOrNot> byRef)
    {
        public string MaximumQualifiedName => byRef switch
        {
            IByRefType<IIncomplete, IMut> byRefMut => byRefMut.MaximumQualifiedName,
            IByRefType<IIncomplete, IReadOnly> byRefReadOnly => byRefReadOnly.MaximumQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IWrapperType<IIncomplete> wrapper)
    {
        public string MaximumQualifiedName => wrapper switch
        {
            IPointerType<IIncomplete> pointer => pointer.MaximumQualifiedName,
            IArrayType<IIncomplete, IFlatOrNot> array => array.MaximumQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IDeclTypeUsage<IIncomplete, IDeclaredType> decl)
    {
        public string MaximumQualifiedName => decl switch
        {
            IBoundGenericType<IIncomplete, IHasGenericParameters> boundGeneric => boundGeneric.MaximumQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IComposableType<IIncomplete> composable)
    {
        public string MaximumQualifiedName => composable switch
        {
            IWrapperType<IIncomplete> wrapper => wrapper.MaximumQualifiedName,
            IInnerType<IIncomplete> inner => inner.MaximumQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(ITupleType<IIncomplete> tuple)
    {
        public string MaximumQualifiedName
        {
            get
            {
                string baseName = tuple switch
                {
                    IRichTupleType<IIncomplete> rich => WrapperNameFormatter.RichTupleName(rich.Elements.Select(e => (e.Type.MaximumQualifiedName, e.Name))),
                    IExplicitValueTupleType<IIncomplete> or IObliviousTupleType<IIncomplete> => tuple.UnderlyingValueTupleType.MaximumQualifiedName,
                    _ => throw new InvalidOperationException("Unreachable case.")
                };

                return tuple.Nullability is NullabilityKind.Nullable
                    ? WrapperNameFormatter.NullableName(baseName)
                    : baseName;
            }
        }
    }

    extension(IFunctionPointerType<IIncomplete> funcPtr)
    {
        public string MaximumQualifiedName => funcPtr switch
        {
            IManagedFunctionPointerType<IIncomplete> man
                => WrapperNameFormatter.ManagedFunctionPointerName(
                    man.Parameters.Select(p => p.Type.MaximumQualifiedName),
                    man.ReturnType.MaximumQualifiedName),
            IUnmanagedFunctionPointerType<IIncomplete> unman
                => WrapperNameFormatter.UnmanagedFunctionPointerName(
                    unman.Parameters.Select(p => p.Type.MaximumQualifiedName),
                    unman.ReturnType.MaximumQualifiedName,
                    unman.CallingConventions),
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }
}
