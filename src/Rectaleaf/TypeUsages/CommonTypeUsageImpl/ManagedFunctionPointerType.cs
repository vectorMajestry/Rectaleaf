using Rectaleaf.Components;
using Rectaleaf.Helpers;
using Rectaleaf.Metadata.TypeUsages.Interfaces;

namespace Rectaleaf.Metadata.TypeUsages.CommonTypeUsageImpl;

public sealed class ManagedFunctionPointerType<TKind>(ITypeUsage<TKind> returnType, IReadOnlyList<IParameter> parameters)
    : TypeUsage<TKind>, IManagedFunctionPointerType<TKind>
    where TKind : IKnownOrNot
{
    public override string LocalName =>
        field ??= WrapperNameFormatter.ManagedFunctionPointerName(Parameters.Select(p => p.Type.LocalName), ReturnType.LocalName);

    public ITypeUsage<TKind> ReturnType { get; } = returnType;

    public IReadOnlyList<IParameter> Parameters { get; } = parameters;
}
