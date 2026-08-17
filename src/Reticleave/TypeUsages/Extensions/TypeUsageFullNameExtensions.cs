using Reticleave.Helpers;

namespace Reticleave.Metadata.TypeUsages.Interfaces;

public static class TypeUsageFullNameExtensions
{
    extension(ITypeUsage<IKnown> knownType)
    {
        public string FullyQualifiedName => knownType switch
        {
            IByRefType<IKnown, IMutOrNot> byref => byref.FullyQualifiedName,
            IComposableType<IKnown> composable => composable.FullyQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IComposableType<IKnown> composable)
    {
        public string FullyQualifiedName => composable switch
        {
            IWrapperType<IKnown> wrapper => wrapper.FullyQualifiedName,
            IInnerType<IKnown> inner => inner.FullyQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IInnerType<IKnown> inner)
    {
        public string FullyQualifiedName => inner switch
        {
            IPredefinedType predefined => predefined.LocalName,
            IFixedSizeBufferType fsb => fsb.LocalName,
            IEnumUnderlyingType enumType => enumType.LocalName,
            IDeclTypeUsage<IKnown> declUsage => declUsage.FullyQualifiedName,
            IFunctionPointerType<IKnown> funcPtr => funcPtr.LocalName,
            ITupleType<IKnown> tuple => tuple.FullyQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IDeclTypeUsage<IKnown> declUsage)
    {
        public string FullyQualifiedName => declUsage switch
        {
            IPlainType<IDeclaredType> plain => plain.FullyQualifiedName,
            IBoundGenericType<IKnown, IHasGenericParameters> boundGeneric => boundGeneric.FullyQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IPlainType<IDeclaredType> plain)
    {
        public string FullyQualifiedName => plain.Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(plain.TypeDefinition.FullyQualifiedName)
            : plain.TypeDefinition.FullyQualifiedName;
    }

    extension(IWrapperType<IKnown> wrapper)
    {
        public string FullyQualifiedName => wrapper switch
        {
            IPointerType<IKnown> pointer => pointer.FullyQualifiedName,
            IArrayType<IKnown, IFlatOrNot> array => array.FullyQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IArrayType<IKnown, IFlatOrNot> array)
    {
        public string FullyQualifiedName => array switch
        {
            IArrayType<IKnown, IFlat> flat => flat.FullyQualifiedName,
            IArrayType<IKnown, IRectArr> rect => rect.FullyQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IByRefType<IKnown, IMutOrNot> byRef)
    {
        public string FullyQualifiedName => byRef switch
        {
            IByRefType<IKnown, IMut> byRefMut => byRefMut.FullyQualifiedName,
            IByRefType<IKnown, IReadOnly> byRefReadOnly => byRefReadOnly.FullyQualifiedName,
            _ => throw new InvalidOperationException("Unreachable case.")
        };
    }

    extension(IByRefType<IKnown, IMut> byrefMut)
    {
        public string FullyQualifiedName => WrapperNameFormatter.RefName(byrefMut.ElementType.FullyQualifiedName);
    }

    extension(IByRefType<IKnown, IReadOnly> byrefReadOnly)
    {
        public string FullyQualifiedName => WrapperNameFormatter.RefReadOnlyName(byrefReadOnly.ElementType.FullyQualifiedName);
    }

    extension(IPointerType<IKnown> pointer)
    {
        public string FullyQualifiedName => WrapperNameFormatter.PointerName(pointer.ElementType.FullyQualifiedName);
    }

    extension(IArrayType<IKnown, IFlat> linearArray)
    {
        public string FullyQualifiedName => linearArray.Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(WrapperNameFormatter.ArrayName(linearArray.ElementType.FullyQualifiedName))
            : WrapperNameFormatter.ArrayName(linearArray.ElementType.FullyQualifiedName);
    }

    extension(IArrayType<IKnown, IRectArr> rectArray)
    {
        public string FullyQualifiedName => rectArray.Nullability is NullabilityKind.Nullable
            ? WrapperNameFormatter.NullableName(WrapperNameFormatter.ArrayName(rectArray.ElementType.FullyQualifiedName, rectArray.Rank))
            : WrapperNameFormatter.ArrayName(rectArray.ElementType.FullyQualifiedName, rectArray.Rank);
    }

    extension(IBoundGenericType<IKnown, IHasGenericParameters> boundGeneric)
    {
        public string FullyQualifiedName
        {
            get
            {
                string baseName = WrapperNameFormatter.GenericName(
                    baseName: boundGeneric.TypeDefinition.FullyQualifiedName,
                    typeArguments: boundGeneric.TypeArguments.Select(t => t.FullyQualifiedName));

                return boundGeneric.Nullability is NullabilityKind.Nullable
                    ? WrapperNameFormatter.NullableName(baseName)
                    : baseName;
            }
        }
    }

    extension(ITupleType<IKnown> tuple)
    {
        public string FullyQualifiedName
        {
            get
            {
                string baseName = tuple switch
                {
                    IRichTupleType<IKnown> rich => $"({string.Join(", ", rich.Elements.Select(e => e.Name is not null ? $"{e.Type.FullyQualifiedName} {e.Name}" : e.Type.FullyQualifiedName))})",
                    IChainTupleType<IKnown> or IObliviousTupleType<IKnown> => tuple.UnderlyingValueTupleType.FullyQualifiedName,
                    _ => throw new InvalidOperationException("Unreachable case.")
                };

                return tuple.Nullability is NullabilityKind.Nullable
                    ? WrapperNameFormatter.NullableName(baseName)
                    : baseName;
            }
        }
    }
}
