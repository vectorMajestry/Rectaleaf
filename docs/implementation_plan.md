# Metadata Contract Semantic Specifications & Architecture Guide

This document defines the core philosophy, exact semantic boundaries, and return specifications for every query and capability method across `Geyser.Generation.Types.Metadata`. It serves as the authoritative blueprint for implementing backends (Roslyn & Reflection) and authoring comprehensive test suites.

---

## 1. Library Purpose & Core Philosophy

### The Problem with Roslyn and Reflection
In standard Roslyn (Source Generator API) and Reflection, methods that process metadata are forced to perform extensive defensive runtime validation:
* If a function processes structs, it must take a generic `Type` or `INamedTypeSymbol` parameter, perform runtime checks (`type.IsValueType && !type.IsEnum`), and throw an exception if the caller passed a class, enum, or interface.
* The API surfaces of `System.Type` and `INamedTypeSymbol` are "API soup" — they expose dozens of methods and properties, half of which are nonsensical or illegal on a given instance (e.g. querying instance constructors on interfaces/static classes, querying fields on delegates, or querying base classes on interfaces).
* Developers must constantly read documentation and perform manual defensive checks to know which properties are safe to touch on any given type shape.

### The Geyser Solution: Shifting Checks to Compile Time
`Geyser.Generation` models all C# language features as distinct, fine-grained interfaces (such as `IStructType`, `IRefStructType`, `IRecordStructType`, `IClassType`, `IRegularClassType`, `IAbstractClassType`, `IStaticClassType`, `IInterfaceType`, `IEnumType`, `IDelegateType`, `IInlineArrayType`):
1. **Compile-Time Validation:** A function that processes structs directly declares its parameter as `IStructType`. This eliminates all defensive runtime checks because the caller is forced by the C# type system to prove that the passed object is indeed a valid struct metadata model.
2. **Eliminating Nonsensical Queries:** Each interface only exposes methods and properties that are logically valid on that specific language construct.
   * *Example:* `IInlineArrayType` deliberately omits `IHasFields`. Structs decorated with `[InlineArray]` always have a single private backing field; if a consumer iterates over fields as if it were a normal struct with variable fields, they are treating it incorrectly without respecting its repeating array nature.
3. **Pattern Matching & Exhaustive Downcasting:** Consumers can either write functions accepting specific models or downcast general models (`IDeclaredType`, `ITypeUsage`) using C# pattern matching against a clean, closed set of models. Once downcast, they get a strongly-typed model exposing only the meaningful operations for that category.
4. **Unified Abstraction Over Multiple Backends:** The library provides a single, high-fidelity contract that can be backed by either the **Roslyn Source Generator API** (during compile-time source generation) or **Reflection / `MetadataLoadContext`** (during runtime or post-compile binary analysis) with 100% semantic parity.

---

## 2. Guiding Architectural Principle: The Golden Rule

$$\textbf{Expose C\# Semantic Members} \quad \longleftrightarrow \quad \textbf{Hide Lowered Implementation Artifacts}$$

* **C# Semantic Members (EXPOSE):** Any construct defined by the C# Language Specification as part of the effective API of a type (e.g. implicit class default ctor `C()`, struct default ctor `S()`, record copy ctor `R(R other)`, record positional properties, `PrintMembers`, synthesized `Deconstruct`).
* **Lowered Implementation Artifacts (HIDE):** Compiler-generated CLR plumbing with no standalone C# source identity (e.g. `<Prop>k__BackingField`, closure classes `<>c__DisplayClass`, state machine structs `<M>d__0`, `<Clone>$`, delegate `BeginInvoke`/`EndInvoke`).
* **Backend Parity Guarantee:** `Geyser` is an authoritative C# Semantic Model. Where a backend lacks a language construct (such as Reflection omitting implicit constructors), the backend synthesizes it so that consumers receive 100% identical semantic data from both Roslyn and Reflection.

---

## 3. Capability Interfaces (`IHas*`)

### `IHasMethods.GetMethods()`
* **Specification:** Returns all callable regular methods directly declared on this type.
* **Inclusions:**
  * User-authored instance and static methods
  * Explicit Interface Implementations (e.g. `void IDisposable.Dispose()`)
  * Default Interface Methods (DIM) with or without bodies on `IInterfaceType`
  * Extension methods (as static methods on static classes)
  * Record language-level semantic methods: `PrintMembers(StringBuilder)` and positional `Deconstruct(...)`
* **Exclusions:**
  * Property accessors (`get_`, `set_`, `init_`)
  * Indexer accessors (`get_Item`, `set_Item`, or custom indexer accessor names)
  * Event accessors (`add_`, `remove_`)
  * User-defined and conversion operators (`op_*`)
  * Instance and static constructors (`.ctor`, `.cctor`)
  * Compiler-lowered local functions (`<M>g__*`)
  * Inherited methods from base classes/interfaces
  * Compiler-internal lowered record methods (`<Clone>$`)
  * Compiler-synthesized record equality boilerplate (`Equals(R)`, `Equals(object)`, `GetHashCode()`, `ToString()`) unless explicitly written in source by the user

### `IHasFields.GetFields()`
* **Specification:** Returns strictly user-declared fields directly declared on this type.
* **Inclusions:**
  * Regular instance and static fields
  * Constant fields (`const`) represented as `ConstField`
  * Fixed-size buffer fields (`fixed int b[10]`) represented as `FixedSizeBufferField`
  * By-ref fields (`ref int f`) in `IRefStructType` represented as `IRefField`
* **Exclusions:**
  * Auto-property compiler-generated backing fields (`<Prop>k__BackingField`)
  * Event compiler-generated backing delegate fields
  * Primary constructor captured parameter fields (e.g. `<param>P`)
  * Fixed-size buffer internal backing struct definitions (`<field>e__FixedBuffer`)
  * Inherited fields from base classes

### `IHasProperties.GetProperties()`
* **Specification:** Returns strictly user-declared named properties directly declared on this type.
* **Inclusions:**
  * User-authored auto-properties and manual properties (instance and static)
  * Record positional properties (e.g. `X` and `Y` on `record Point(int X, int Y)`)
  * Explicit Interface Implementation properties (e.g. `int IInterface.Prop => ...`)
  * Static abstract/virtual properties on interfaces
* **Exclusions:**
  * All indexers (regardless of whether named `Item` or renamed via `[IndexerName("CustomIndex")]`)
  * Compiler-synthesized record `EqualityContract` property
  * Inherited properties from base classes/interfaces

### `IHasIndexers.GetIndexers()`
* **Specification:** Returns strictly user-declared indexers directly declared on this type (including indexers with default name `this[...]` or renamed via `[IndexerName]`).
* **Inclusions:**
  * User-authored indexers (with single or multiple parameters)
  * Explicit Interface Implementation indexers
  * Static abstract/virtual indexers on interfaces
* **Exclusions:**
  * Regular named properties (properties with 0 index parameters)
  * Inherited indexers from base classes/interfaces

### `IHasEvents.GetEvents()`
* **Specification:** Returns strictly user-declared events directly declared on this type.
* **Inclusions:**
  * Field-like events (`public event Action OnChange;`)
  * Custom explicit events (`public event Action OnChange { add { } remove { } }`)
  * Explicit Interface Implementation events
  * Static abstract/virtual events on interfaces
* **Exclusions:**
  * Compiler-generated backing delegate fields
  * Inherited events from base classes/interfaces

### `IHasInstanceConstructors.GetInstanceConstructors()`
* **Specification:** Returns the effective set of C# instance constructors available on this type according to the C# language model, regardless of access modifier (including `public`, `protected`, `internal`, and `private`).
* **Inclusions:**
  * Explicitly declared instance constructors on classes (including `abstract` classes) and structs
  * Implicit parameterless class constructor (`public C()`) when no constructors and no primary constructor are declared
  * Struct default parameterless constructor (`public S()`) on all struct types (explicit C# 10+ or semantic default constructor)
  * Primary constructors (both positional record primary ctors and C# 12+ class/struct primary ctors, with `IsPrimary == true`)
  * Record copy constructors on record classes (e.g. `protected R(R original)` on unsealed record classes, and `private` on sealed record classes)
* **Exclusions:**
  * Static constructors (`.cctor`) — queried exclusively via `IHasStaticConstructor.StaticConstructor`
  * Module initializers (`<Module>..cctor`)
  * Struct zero-initialization memory mechanism (`default(S)`)
  * Anonymous type implementation constructors (`<>f__AnonymousType0<...>..ctor`)
  * Record structs synthesized copy constructors (record structs do not synthesize copy constructors)

### `IHasStaticConstructor.StaticConstructor`
* **Specification:** Returns `IStaticConstructor?` representing the type initializer (`.cctor`) if one is explicitly declared or emitted for the type, otherwise `null`.

### `IHasNestedTypes.GetNestedTypes()`
* **Specification:** Returns strictly user-declared nested types directly defined inside this type (including nested classes, structs, records, interfaces, enums, and delegates).
* **Exclusions:**
  * Compiler-generated closure display classes (`<>c__DisplayClass...`, `<>c`)
  * Async and iterator state machine structs/classes (`<Method>d__...`)
  * Local function closure classes
  * Fixed-size buffer compiler-generated internal backing structs (`<field>e__FixedBuffer`)

### `IHasInterfaces.GetDirectInterfaces()`
* **Specification:** Returns strictly the interfaces directly declared in this type's declaration header (including interfaces explicitly re-listed on derived types).
* **Inclusions:**
  * Explicitly listed interfaces on the type declaration
  * `IEquatable<R>` for record declarations (as mandated by the C# Language Specification)
* **Exclusions:**
  * Transitively inherited interfaces from base classes (unless explicitly re-listed in the derived type's declaration header)
  * Transitive base interfaces of declared interfaces (e.g. if `IChild : IParent` is declared, only `IChild` is direct)

### `IHasGenericParameters.GetGenericParameters()`
* **Specification:** Returns strictly the generic parameters directly declared on this entity (type, method, or local function) in declaration order (`Position` 0..N-1). Returns an empty collection for non-generic entities.
* **Nested Generic Types:** For nested generic types (`Outer<T>.Inner<U>`), returns only the directly declared generic parameters (`[U]`). Outer generic parameters (`[T]`) are accessed via `ContainingType`.
* **Each Parameter Contains:**
  * `Name`, `Position`, `Variance` (`Invariant`, `Covariant`, `Contravariant`)
  * `DeclaringEntity` (`IHasGenericParameters`)
  * All 8 constraint flags (reference, nullable ref, notnull, struct, unmanaged, new(), allows ref struct, default)
  * `ConstraintTypes` (`IReadOnlyList<IValidConstraint>`)
  * `GetAttributes()`

### `IHasOperators.GetOperators()`
* **Specification:** Returns strictly user-declared operators directly declared on this type.
* **Inclusions:**
  * Unary operators (`IUnaryOperator`: `+`, `-`, `!`, `~`, `++`, `--`, `true`, `false`, `^`, `IsChecked`)
  * Binary operators (`IBinaryOperator`: arithmetic, bitwise, shift including `>>>`, comparisons, `IsChecked`)
  * User-defined conversion operators (`IConversionOperator`: `implicit`, `explicit`, `checked explicit`)
  * Record equality operators (`operator ==` and `operator !=` synthesized per C# specification)
  * Static abstract/virtual operators declared on `IInterfaceType`
* **Exclusions:**
  * Inherited operators from base classes

### `IHasLocalFunctions.GetLocalFunctions()`
* **Specification:** Returns strictly the immediate direct child local functions declared within this callable entity in lexical declaration order (`Ordinal` 0..N-1).
* **Hierarchical Structure:** If a local function contains nested local functions, those nested functions are queried on the child `ILocalFunction.GetLocalFunctions()`, rather than being flattened into the top-level container.
* **Each Local Function Exposes:**
  * `Name` (clean source identifier, e.g. `"Helper"`, without compiler name mangling)
  * `Ordinal` (zero-based declaration index within its direct declaring entity)
  * `DeclaringEntity` (`IHasLocalFunctions` pointing to the immediate parent callable)
  * `ReturnInfo`, `IsStatic`, `IsGeneric`
  * `GetParameters()`, `GetAttributes()`, `GetGenericParameters()`, `GetLocalFunctions()`

---

## 4. Members & Types

### `IDelegateType`
* **Specification:** Represents the C# delegate declaration as a first-class callable reference type.
* **Signature Mapping:**
  * `ReturnInfo`: The logical return type and return-scoped attributes of the delegate.
  * `GetParameters()`: The logical parameters of the delegate in declaration order.
  * `GetGenericParameters()`: The generic parameters of the delegate (e.g. `T` in `Action<T>`).
* **Exclusions:**
  * Legacy CLR asynchronous plumbing methods (`BeginInvoke`, `EndInvoke`)
  * CLR delegate constructor (`.ctor(object, IntPtr)`)
  * `Invoke` is projected directly via `ReturnInfo` and `GetParameters()`, rather than exposing delegate internals as pseudo-methods.

### `IAttribute` and `GetAttributes()`
* **Specification:** Returns strictly user-applied attributes directly decorating the target entity (Type, Member, Parameter, Accessor, or Return Info).
* **Scoped Distribution:**
  * Type-level attributes $\rightarrow$ `IDeclaredType.GetAttributes()`
  * Member-level attributes $\rightarrow$ `IMethod`, `IField`, `IProperty`, etc. `GetAttributes()`
  * Return-type attributes (e.g. `[return: NotNull]`) $\rightarrow$ `IReturnInfo.GetAttributes()`
  * Parameter attributes (e.g. `[param: NotNull]`) $\rightarrow$ `IParameter.GetAttributes()`
  * Accessor attributes (e.g. `[get: AllowNull]`) $\rightarrow$ `IAccessor.GetAttributes()`
* **Exclusions (Compiler-Internal Pseudo-Attributes & Synthetic Gates):**
  * Nullability metadata attributes (`[Nullable]`, `[NullableContext]`)
  * Tuple metadata attributes (`[TupleElementNames]`)
  * Struct/Member modifiers (`[IsReadOnly]`, `[IsByRefLike]`, `[IsUnmanaged]`)
  * Parameter modifiers (`[ParamArray]`, `[ScopedRef]`, `[In]`, `[Out]`)
  * Required member metadata (`[RequiredMember]`, `[CompilerFeatureRequired]`)
  * Lowering indicators (`[CompilerGenerated]`, `[RefSafetyRules]`, `[AsyncStateMachine]`, `[IteratorStateMachine]`)
  * Synthetic `[Obsolete]` compatibility gates: Filter out `[Obsolete]` attributes co-occurring with `[IsByRefLike]` (on `ref struct` types) or `[CompilerFeatureRequired]` (on types/constructors with `required` members), as these are compiler-injected errors to block legacy C# compilers, not user-authored deprecations.

### `BaseType` on Classes
* **Specification:** Returns the direct base class type usage if one was explicitly specified, or `null` if the class directly inherits from `System.Object` (i.e. no custom base class was explicitly declared).
* **For `IClassType` (`IRegularClassType`, `IAbstractClassType`):**
  * Returns `IDeclTypeUsage<IKnownOrNot, IClassType>?`
  * Returns `null` for `System.Object` and for any class that does not declare an explicit custom base class.
* **For `IRecordClassType`:**
  * Returns `IDeclTypeUsage<IKnownOrNot, IRecordClassType>?`
  * Returns `null` if the record class does not explicitly inherit from another record class (i.e. roots at `System.Object`).

---

## 5. Backend Realization & Reflection Strategy Guide

This reference section documents the low-level metadata signals and strategies used by the Reflection backend to fulfill the semantic specifications deterministically without heuristics or runtime code execution risks.

### Metadata Detection Signals & Reliability Tiers

| Target Concept / Filter | Detection Signal in Reflection | Reliability Tier |
| :--- | :--- | :--- |
| **Accessors, Operators, Ctors** (exclude from `GetMethods`) | `MethodAttributes.SpecialName` (ECMA-335 flag) + method prefix check (`get_`, `set_`, `add_`, `remove_`, `op_`) | **Bulletproof (ECMA-335 Spec)** |
| **Value vs Ref, Sealed, Abstract, Interface, Enum** | `TypeAttributes` (`Type.IsValueType`, `Type.IsSealed`, `Type.IsAbstract`, `Type.IsInterface`, `Type.IsEnum`) | **Bulletproof (ECMA-335 Spec)** |
| **Fixed-Size Buffers** | `[FixedBufferAttribute]` on the field (exposes `ElementType` and `Length`) | **Bulletproof (Official BCL Type)** |
| **Async / Iterator State Machines** | Nested types referenced by `[AsyncStateMachine]`, `[IteratorStateMachine]`, or `[AsyncIteratorStateMachine]` on containing methods | **Bulletproof (Official BCL Attribute)** |
| **`readonly struct`, `ref struct`** | `[IsReadOnlyAttribute]`, `[IsByRefLikeAttribute]` | **Bulletproof** |
| **`required` Members** | `[RequiredMemberAttribute]` + `[CompilerFeatureRequiredAttribute("RequiredMembers")]` | **Bulletproof (Official BCL Attribute)** |
| **Unmanaged Generic Constraint** | `[IsUnmanagedAttribute]` decorating the generic type parameter representation | **Bulletproof (Attribute Check)** |
| **Compiler Plumbing (Closures, Backing Fields, Ordinal Local Functions)** | Member metadata name contains non-lexable characters (`<`, `>`, `|`, `$`), e.g. `<>c__DisplayClass`, `<Prop>k__BackingField`, `<M>g__Local\|0_0`, `<Clone>$` | **Bulletproof Guarantee:** `< > \| $` cannot be written in C# source identifiers per the C# lexer grammar. |
| **Direct Interfaces Subtraction** | `type.GetInterfaces().Except(baseInterfaces).Except(transitiveParentInterfaces)` | **Deterministic Set Subtraction** |
| **Direct Generic Parameters Slicing** | `type.GetGenericArguments().Skip(containingTypeGenericCount)` | **Deterministic Slicing** |

### Critical Implementation Guidelines for Reflection Backend

1. **Attribute Comparison Rule (Always compare `FullName`):**
   * Some compiler-injected marker attributes (like `IsUnmanagedAttribute` or internal `NullableAttribute`) are synthesized locally per-assembly by Roslyn.
   * **Rule:** Never check `attr.GetType() == typeof(...)` identity across assembly boundaries. Always compare via `attr.GetType().FullName == "System.Runtime.CompilerServices.IsUnmanagedAttribute"` to ensure cross-assembly reliability.

2. **Synthetic `[Obsolete]` Compatibility Gates:**
   * When Roslyn emits `ref struct` types or types with `required` members, it automatically injects `[Obsolete("Types with embedded references are not supported...", true)]` so legacy compilers cannot accidentally consume them.
   * **Rule:** Filter out `[Obsolete]` if `[IsByRefLike]` or `[CompilerFeatureRequired]` is present on the same entity.

3. **Loading Strategy: `MetadataLoadContext` vs. Standard Reflection:**
   * **Standard Reflection (`Assembly.Load`):** Loads assemblies into the active runtime process. Can inadvertently trigger static initializers (`.cctor`), locks DLL files on disk, and fails when inspecting assemblies targeted for different runtime versions.
   * **`MetadataLoadContext` (`System.Reflection.MetadataLoadContext`):** An official, high-level, read-only reflection container.
     * Exposes the **exact same `Type`, `MethodInfo`, `FieldInfo`, `PropertyInfo`** APIs as standard Reflection.
     * Runs **purely in data-only mode** (never executes any code or `.cctor`).
     * Inspects assemblies targeting any framework (.NET Framework, .NET Core, .NET Standard) without dependency conflicts.
     * Fully disposable (releases disk file locks immediately).
