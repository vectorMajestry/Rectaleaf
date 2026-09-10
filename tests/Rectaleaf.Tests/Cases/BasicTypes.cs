namespace TestCases;

[AttributeUsage(AttributeTargets.All)]
public class MyCustomAttribute : Attribute
{
    public string Tag { get; }
    public MyCustomAttribute(string tag) => Tag = tag;
}

[MyCustom("CursedRefStruct")]
public unsafe ref struct MyRefStruct<T, U>
    where T : U
    where U : class
{
    public delegate* unmanaged[Cdecl, Fastcall]<T, U, int>* myFunctionPointer;
    public MyStruct<int, int>* myStructPointer;

    public MyRefStruct(delegate* unmanaged[Cdecl, Fastcall]<T, U, int>* functionPointer, MyStruct<int, int>* structPointer)
    {
        myFunctionPointer = functionPointer;
        myStructPointer = structPointer;
    }
}

public struct MyStruct<T, U>
{
    public T valT;
    public U valU;
}