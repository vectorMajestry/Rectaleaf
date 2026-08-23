using Rectaleaf.Components;
using Rectaleaf.Helpers;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class UnmanagedFunctionPointerType<TKind>(
    ITypeUsage<TKind> returnType,
    IReadOnlyList<IParameter> parameters,
    CallConvFlags callingConventions)
    : TypeUsage<TKind>, IUnmanagedFunctionPointerType<TKind>
    where TKind : IKnownOrNot
{
    public override string LocalName =>
        field ??= WrapperNameFormatter.UnmanagedFunctionPointerName(
            Parameters.Select(p => p.Type.LocalName),
            ReturnType.LocalName,
            CallingConventions);

    public ITypeUsage<TKind> ReturnType { get; } = returnType;

    public IReadOnlyList<IParameter> Parameters { get; } = parameters;

    public CallConvFlags CallingConventions { get; } = callingConventions;
}
