namespace TestCases;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public unsafe class FunctionPointerTestContainer
{
    // Managed function pointer with primitive arguments and void return
    public delegate*<int, string, void> ManagedActionPointer;

    // Managed function pointer with return value
    public delegate*<int, bool, string> ManagedFuncPointer;

    // Default unmanaged function pointer (no calling convention types specified)
    public delegate* unmanaged<void> DefaultUnmanagedAction;

    // Unmanaged function pointer with Cdecl then Fastcall
    public delegate* unmanaged[Cdecl, Fastcall]<void> UnmanagedCdeclFastcall;

    // Unmanaged function pointer with Fastcall then Cdecl (ordering must be preserved)
    public delegate* unmanaged[Fastcall, Cdecl]<void> UnmanagedFastcallCdecl;

    // Unmanaged function pointer with Stdcall and Thiscall
    public delegate* unmanaged[Stdcall, Thiscall]<int, double, float> UnmanagedStdcallThiscall;

    // Unmanaged function pointer with SuppressGCTransition modifier (modreq)
    public delegate* unmanaged[Cdecl, SuppressGCTransition]<void> UnmanagedWithSuppressGc;

    // Reversed order with SuppressGCTransition first
    public delegate* unmanaged[SuppressGCTransition, Stdcall]<int, int> UnmanagedSuppressGcFirst;

    // Unmanaged function pointer with duplicated and conflicting calling conventions
    public delegate* unmanaged[Cdecl, Cdecl, SuppressGCTransition, MemberFunction, Stdcall, Cdecl]<void> UnmanagedDuplicatedConventions;

    // Function pointer with ref, in, and out parameter modifiers
    public delegate*<ref int, in double, out byte, void> ManagedWithRefModifiers;

    // Unmanaged function pointer with ref, in, out parameter modifiers
    public delegate* unmanaged[Cdecl]<ref int, in double, out byte, int> UnmanagedWithRefModifiers;

    // Pointer to an unmanaged function pointer
    public delegate* unmanaged[Cdecl]<int, int>* PointerToFunctionPointer;

    // Function pointer returning another function pointer
    public delegate*<delegate* unmanaged[Cdecl]<int, void>> NestedReturnFunctionPointer;

    // Function pointer accepting another function pointer as an argument
    public delegate*<delegate*<int, bool>, int, bool> HigherOrderFunctionPointer;

    // Function pointer taking raw pointers and void*
    public delegate* unmanaged[Cdecl]<void*, int*, double**, void> RawPointersFunctionPointer;

    // Method with function pointer parameters and return types
    public delegate* unmanaged[Cdecl, Fastcall]<int, int> ExecuteCallback(
        delegate* unmanaged[Cdecl]<int, int> callback,
        delegate*<int, bool> filter)
    {
        return (delegate* unmanaged[Cdecl, Fastcall]<int, int>)callback;
    }
}
