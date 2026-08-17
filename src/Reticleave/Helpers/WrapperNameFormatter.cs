
namespace Reticleave.Helpers;

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
}
