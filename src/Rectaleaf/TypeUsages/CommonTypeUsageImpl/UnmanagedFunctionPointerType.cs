using Rectaleaf.Components;
using Rectaleaf.Helpers;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class UnmanagedFunctionPointerType<TKind>
    : TypeUsage<TKind>, IUnmanagedFunctionPointerType<TKind>
    where TKind : IKnownOrNot
{
    public override string LocalName =>
        field ??= WrapperNameFormatter.UnmanagedFunctionPointerName(
            Parameters.Select(p => p.Type.LocalName),
            ReturnType.LocalName,
            CallingConventions);

    public ITypeUsage<TKind> ReturnType { get; }

    public IReadOnlyList<IParameter> Parameters { get; }

    public IReadOnlyList<CallConvKind> CallingConventions { get; }

    public CallConvFlags CallingConventionFlags { get; }

    public UnmanagedFunctionPointerType(
        ITypeUsage<TKind> returnType,
        IReadOnlyList<IParameter> parameters,
        IReadOnlyList<CallConvKind> callingConventions)
    {
        ReturnType = returnType;
        Parameters = parameters;
        CallingConventions = callingConventions;

        uint flags = 0;
        for (int i = 0; i < callingConventions.Count; i++)
            flags |= 1u << (int)callingConventions[i];
        CallingConventionFlags = (CallConvFlags)flags;
    }

    public UnmanagedFunctionPointerType(
        ITypeUsage<TKind> returnType,
        IReadOnlyList<IParameter> parameters,
        IReadOnlyList<CallConvKind> callingConventions,
        CallConvFlags callingConventionFlags)
    {
        ReturnType = returnType;
        Parameters = parameters;
        CallingConventions = callingConventions;
        CallingConventionFlags = callingConventionFlags;
    }
}
