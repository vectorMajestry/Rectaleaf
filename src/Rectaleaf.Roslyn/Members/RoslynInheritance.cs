using System;
using Rectaleaf.Metadata.Members;

namespace Rectaleaf.Roslyn.Members;

internal sealed class RoslynInheritanceNone<TMember> : IInheritanceNone<TMember>
{
    public static readonly RoslynInheritanceNone<TMember> Instance = new();

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynInheritanceAbstract<TMember> : IInheritanceAbstract<TMember>
{
    public static readonly RoslynInheritanceAbstract<TMember> Instance = new();

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynInheritanceVirtual<TMember> : IInheritanceVirtual<TMember>
{
    public static readonly RoslynInheritanceVirtual<TMember> Instance = new();

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal class RoslynInheritanceOverride<TMember>(Func<TMember> overriddenMemberResolver) : IInheritanceOverride<TMember>
{
    TMember? overriddenMember;

    public TMember OverriddenMember => overriddenMember ??= overriddenMemberResolver();

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}

internal sealed class RoslynInheritanceSealedOverride<TMember>(Func<TMember> overriddenMemberResolver)
    : RoslynInheritanceOverride<TMember>(overriddenMemberResolver), IInheritanceSealedOverride<TMember>;

internal sealed class RoslynInheritanceExplicitImplementation<TMember>(Func<TMember> explicitMemberResolver)
    : IInheritanceExplicitImplementation<TMember>
{
    TMember? explicitMember;

    public TMember ExplicitInterfaceMember => explicitMember ??= explicitMemberResolver();

    public override bool Equals(object? obj) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();
}
