using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Properties;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using System.Reflection;

namespace Rectaleaf.Reflection;

public static class RectaleafReflection
{
    public static IDeclaredType FromDeclaredType(Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        throw new NotImplementedException("Reflection backend model creation from Type is not yet implemented.");
    }

    public static IMethod FromMethod(MethodInfo method)
    {
        if (method is null)
            throw new ArgumentNullException(nameof(method));
        throw new NotImplementedException("Reflection backend model creation from MethodInfo is not yet implemented.");
    }

    public static IField<IKnownOrNot> FromField(FieldInfo field)
    {
        if (field is null)
            throw new ArgumentNullException(nameof(field));
        throw new NotImplementedException("Reflection backend model creation from FieldInfo is not yet implemented.");
    }

    public static IProperty<IKnownOrNot, IAccess> FromProperty(PropertyInfo property)
    {
        if (property is null)
            throw new ArgumentNullException(nameof(property));
        throw new NotImplementedException("Reflection backend model creation from PropertyInfo is not yet implemented.");
    }
}
