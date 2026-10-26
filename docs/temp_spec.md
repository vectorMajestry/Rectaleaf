docs-unslop conformant.

# Metadata Contract Semantic Specifications & Architecture Guide

This document defines the core philosophy, exact semantic boundaries, and return specifications for every query and capability method across `Rectaleaf.Metadata`. It serves as the authoritative blueprint for implementing backends (Roslyn & Reflection) and authoring comprehensive test suites.

---

## 1. Library Purpose & Core Philosophy

### The Problem with Roslyn and Reflection
In standard Roslyn (Source Generator API) and Reflection, methods that process metadata are forced to perform extensive defensive runtime validation:
* If a function processes structs, it must take a generic `Type` or `INamedTypeSymbol` parameter, perform runtime checks (`type.IsValueType && !type.IsEnum`), and throw an exception if the caller passed a class, enum, or interface.
* The API surfaces of `System.Type` and `INamedTypeSymbol` are "API soup". They expose dozens of methods and properties, half of which are nonsensical or illegal on a given instance (e.g. querying instance constructors on interfaces/static classes, querying fields on delegates, or querying base classes on interfaces).
* Developers must constantly read documentation and perform manual defensive checks to know which properties are safe to touch on any given type shape.

### The Rectaleaf Solution: Shifting Checks to Compile Time
`Rectaleaf` models all C# language features as distinct, fine-grained interfaces (such as `IStructType`, `IRefStructType`, `IRecordStructType`, `IClassType`, `IRegularClassType`, `IAbstractClassType`, `IStaticClassType`, `IInterfaceType`, `IEnumType`, `IDelegateType`, `IInlineArrayType`):
1. **Compile-Time Validation:** A function that processes structs directly declares its parameter as `IStructType`. This eliminates all defensive runtime checks because the caller is forced by the C# type system to prove that the passed object is indeed a valid struct metadata model.
2. **Eliminating Nonsensical Queries:** Each interface only exposes methods and properties that are logically valid on that specific language construct.
   * *Example:* `IInlineArrayType` deliberately omits `IHasFields`. Structs decorated with `[InlineArray]` always have a single private backing field; if a consumer iterates over fields as if it were a normal struct with variable fields, they are treating it incorrectly without respecting its repeating array nature.
3. **Pattern Matching & Downcasting:** Consumers can either write functions accepting specific models or downcast more general models (`IDeclaredType`, `ITypeUsage`) using C# pattern matching against a clean set of models. Once downcast, they get a strongly-typed model exposing only the meaningful operations for that category.
4. **Unified Abstraction Over Multiple Backends:** The library provides a single, high-fidelity contract that can be backed by either the **Roslyn Source Generator API** (during compile-time source generation) or **Reflection / `MetadataLoadContext`** (during runtime or post-compile binary analysis) with near 100% semantic parity (exceptions are obvious and are clearly documented).

---

## 2. Guiding Architectural Principle

Expose C# Semantic Members; Hide Lowered Implementation Artifacts.

* **C# Semantic Members (Expose):** Any construct defined by the C# Language Specification as part of the effective API of a type (e.g. implicit class default ctor `C()`, struct default ctor `S()`, record copy ctor `R(R other)`, record positional properties, `PrintMembers`, synthesized `Deconstruct`).
* **Lowered Implementation Artifacts (Hide):** Compiler-generated CLR plumbing with no standalone C# source identity (e.g. `<Prop>k__BackingField`, closure classes `<>c__DisplayClass`, state machine structs `<M>d__0`, `<Clone>$`, delegate `BeginInvoke`/`EndInvoke`).
* **Backend Parity Guarantee:** `Rectaleaf` is an authoritative C# Semantic Model. Where a backend lacks a language construct (such as Reflection omitting implicit constructors), the backend synthesizes it so that consumers receive 100% identical semantic data from both Roslyn and Reflection.

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
* **Specification:** Returns strictly user-declared indexers directly declared on this type.
* **Naming Convention:**
  * An indexer's `Name` property returns `"Item"` if `[IndexerName("CustomName")]` is absent.
  * Custom renamed indexers (declared with `[IndexerName("CustomName")]`) return the custom name via `Name`, e.g. `"Grid"`.
  * Explicit interface implementation indexers return `"Item"` via `Name`, without the interface name prepended. Applying `[IndexerName]` directly to an explicit interface member declaration is invalid C# syntax (compiler error CS0415). Even if the target interface indexer was declared with `[IndexerName("CustomName")]`, the implementing type's explicit implementation member is emitted as `Interface.Item` in CLI metadata and reflection, and its `Name` property returns `"Item"`.
* **Attribute Filtering:**
  * `IndexerNameAttribute` is treated as compiler plumbing and must never appear in `IIndexer.GetAttributes()`.
  * `DefaultMemberAttribute` is treated as compiler plumbing and is filtered out lazily from `IType.GetAttributes()` if the type contains indexers.
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
  * Static constructors (`.cctor`), queried exclusively via `IHasStaticConstructor.StaticConstructor`
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
  * `Source` (`GenericParameterSource`): Discriminated union indicating whether the parameter was declared on a type (`GenericType`), method (`GenericMethod`), local function (`GenericLocalFunction`), or extension block (`GenericExtensionBlock`).
  * All 8 constraint flags:
    * `HasReferenceTypeConstraint` (`where T : class`)
    * `HasNullableReferenceTypeConstraint` (`where T : class?`)
    * `HasNotNullConstraint` (`where T : notnull`)
    * `HasValueTypeConstraint` (`where T : struct`)
    * `HasUnmanagedTypeConstraint` (`where T : unmanaged`)
    * `HasConstructorConstraint` (`where T : new()`)
    * `HasAllowsRefStructConstraint` (`where T : allows ref struct`, C# 13 anti-constraint)
    * `HasDefaultConstraint` (`where T : default`)
  * `ConstraintTypes` (`IReadOnlyList<ConstraintType>`): Type constraints specified in the `where T : ...` clause. C# permits only interfaces, non-sealed classes, and other type parameters as type constraints. Value types (structs, enums, inline arrays), static classes, and delegates cannot be used as constraint targets. Each constraint is represented by the `ConstraintType` discriminated union:
    * `ConstraintPlaceholderType(IPlaceholderType Type)`: Represents another generic type parameter used as a constraint (e.g. `where T : U`).
    * `ConstraintDeclaredType(IDeclTypeUsage<IKnownOrNot, IValidConstraint> Type)`: Represents a class or interface type usage used as a constraint (e.g. `where T : class, IComparable<T>`).
    * `IValidConstraint : IDeclaredType`: Marker interface implemented strictly by nominal declared types that can serve as constraint targets (`IInterfaceType`, `IClassType` including regular and abstract classes, and `IRecordClassType`). It is excluded from `IStructType`, `IRefStructType`, `IRecordStructType`, `IEnumType`, `IInlineArrayType`, `IStaticClassType`, and `IDelegateType` because the C# compiler rejects value types and static classes in constraint clauses.
  * `GetAttributes()`

### `IHasOperators.GetOperators()`
* **Specification:** Returns strictly user-declared operators directly declared on this type.
* **Inclusions:**
  * Unary operators (`IUnaryOperator`: `+`, `-`, `!`, `~`, `++`, `--`, `true`, `false`, `^`, `IsChecked`)
  * Binary operators (`IBinaryOperator`: arithmetic, bitwise, shift including `>>>` `UnsignedRightShift`, comparisons, `IsChecked`)
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

### `IHasExtensionsBlocks.GetExtensionsBlocks()`
* **Specification:** Returns strictly the C# 14 extension declaration blocks directly defined within this static class (`IStaticClassType`).
* **Filtering & Separation Guarantee:**
  * Extension members are exposed exclusively within their respective `IExtensionsBlock` instances (`GetMethods()`, `GetProperties()`, `GetIndexers()`).
  * Lowered static methods emitted on the containing static class that correspond to extension block members are filtered out of `IHasMethods.GetMethods()` on `IStaticClassType`.
  * User-authored regular static methods defined on the static class remain accessible via `IHasMethods.GetMethods()`.

---

## 4. Declared Types & Members

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
* **Members:**
  * `UnderlyingType` (`IDeclTypeUsage<IKnownOrNot, IRegularClassType>`): The type usage of the attribute class (either `IPlainType<IRegularClassType>` for non-generic attributes or `IBoundGenericType<IKnownOrNot, IRegularClassType>` for generic attributes), whose `TypeDefinition` exposes the underlying `IRegularClassType` declaration and its constructors, properties, fields, and methods.
  * `UsedConstructor` (`IInstanceConstructor`): The instance constructor invoked at the attribute usage site.
  * `ConstructorArguments` (`IReadOnlyList<AttributeValue>`): The positional arguments passed to `UsedConstructor` at the usage site.
  * `NamedArguments` (`IReadOnlyDictionary<string, AttributeValue>`): The named field and property initializers specified at the usage site.
* **Scoped Distribution:**
  * Type-level attributes -> `IDeclaredType.GetAttributes()`
  * Member-level attributes -> `IMethod`, `IField`, `IProperty`, etc. `GetAttributes()`
  * Return-type attributes (e.g. `[return: NotNull]`) -> `IReturnInfo.GetAttributes()`
  * Parameter attributes (e.g. `[param: NotNull]`) -> `IParameter.GetAttributes()`
  * Accessor attributes (e.g. `[get: AllowNull]`) -> `IAccessor.GetAttributes()`
* **Exclusions (Compiler-Internal Pseudo-Attributes & Synthetic Gates):**
  * Nullability metadata attributes (`[Nullable]`, `[NullableContext]`)
  * Tuple metadata attributes (`[TupleElementNames]`)
  * Struct/Member modifiers (`[IsReadOnly]`, `[IsByRefLike]`, `[IsUnmanaged]`)
  * Parameter modifiers (`[ParamArray]`, `[ScopedRef]`, `[In]`, `[Out]`)
  * Required member metadata (`[RequiredMember]`, `[CompilerFeatureRequired]`)
  * Lowering indicators (`[CompilerGenerated]`, `[RefSafetyRules]`, `[AsyncStateMachine]`, `[IteratorStateMachine]`)
  * Synthetic `[Obsolete]` compatibility gates: Filter out `[Obsolete]` attributes whose message string matches the compiler-injected compatibility gates (`"Types with embedded references are not supported in this version of your compiler."` or `"Constructors of types with required members are not supported in this version of your compiler."`), while preserving user-authored `[Obsolete]` attributes.

### `BaseType` on Classes
* **Specification:** Returns the direct base class type usage if one was explicitly specified, or `null` if the class directly inherits from `System.Object` (i.e. no custom base class was explicitly declared).
* **For `IClassType` (`IRegularClassType`, `IAbstractClassType`):**
  * Returns `IDeclTypeUsage<IKnownOrNot, IClassType>?`
  * Returns `null` for `System.Object` and for any class that does not declare an explicit custom base class.
* **For `IRecordClassType`:**
  * Returns `IDeclTypeUsage<IKnownOrNot, IRecordClassType>?`
  * Returns `null` if the record class does not explicitly inherit from another record class (i.e. roots at `System.Object`).

### `IInlineArrayType`
* **Specification:** Represents an inline array struct definition (C# 12 `[InlineArray(N)]`).
* **Members & Characteristics:**
  * Implements `IValueType`, `IHasMethods`, `IHasProperties`, `IHasIndexers`, `IHasEvents`, `IHasGenericParameters`, `IHasInstanceConstructors`, `IHasStaticConstructor`, `IHasNestedTypes`, `IHasInterfaces`, `IHasOperators`.
  * **Deliberately omits `IHasFields`:** The single private backing field emitted by Roslyn is compiler plumbing, not a user-declared field.
  * `ElementType`: The element type of the repeating buffer (`IComposableType<IKnownOrNot>`).
  * `Length`: The fixed element count specified in `[InlineArray(Length)]`.
  * `IsReadOnly`: Indicates whether the inline array struct is declared `readonly`.

### File-Local Types (`AccessModifier.File`)
* **Specification:** Represents C# 11 file-scoped type declarations (`file class`, `file struct`, `file interface`, `file enum`, `file delegate`).
* **Access Modifier:** `AccessModifier.File`.
* **Clean Name Demangling:**
  * Roslyn compiles file-local types by mangling their metadata name with a file-path hash: `<Context>HASH__OriginalName`.
  * **Rule for Backends:** The Reflection backend must detect this pattern on non-public, top-level types and strip the `<...>HASH__` prefix so that `IDeclaredType.LocalName` returns the clean source identifier (e.g. `"MyFileLocalClass"`).

### Auto-Property Backing Field (`IRegularProperty.BackingField`)
* **Specification:** For compiler-generated auto-properties, returns the compiler-generated backing field as an `IRegularField<TKind>`. For manual/computed properties (properties with custom getter/setter bodies), returns `null`.
* **Reflection Resolution Rule:** The Reflection backend resolves `BackingField` by looking for a private instance field named `<PropName>k__BackingField` on the declaring type. (Note: this backing field is excluded from `IHasFields.GetFields()`).

### `IExtensionsBlock` (C# 14 Extension Declarations)
* **Specification:** Represents an extension declaration block (`extension(...) { ... }`) within a top-level, non-generic static class (`IStaticClassType`).
* **Common Members on `IExtensionsBlock`:**
  * `DeclaringType` (`IStaticClassType`): The static class that declares the extension block.
  * `ExtendedType` (`ITypeUsage<IKnownOrNot>`): The target type usage being extended (matches `Receiver.Type` on `IInstanceExtensionsBlock`, including `IByRefType` when the receiver has `ref`, `in`, or `ref readonly`).
  * `IsGeneric` (`bool`) and `GetGenericParameters()` (`IReadOnlyList<IGenericParameter>`).
  * `GetMethods()`, `GetProperties()`, and `GetIndexers()`.
* **Branching Model:**
  * `IInstanceExtensionsBlock` (Named Receiver):
    * Represents extension blocks with a named receiver parameter (e.g. `extension(ref Point pt)` or `extension<T>(List<T> list) where T : class`).
    * Exposes `Receiver` (`IParameter`) holding the receiver parameter's name, type usage (`IByRefType` when declared with `ref`, `in`, or `ref readonly`), modifiers (`ref`, `in`, `ref readonly`, `scoped`), and parameter attributes.
    * Can declare both instance and static extension members (`GetMethods()`, `GetProperties()`, `GetIndexers()`).
  * `IStaticExtensionsBlock` (Unnamed Receiver):
    * Represents extension blocks with an unnamed receiver parameter (e.g. `extension(string)` or `extension<T>([MyAttr] List<T>) where T : new()`).
    * Shadows `ExtendedType` as `IComposableType<IKnownOrNot>` since an unnamed receiver parameter cannot have `ref`, `in`, or `out` modifiers.
    * Exposes `GetReceiverAttributes()` (`IReadOnlyList<IAttribute>`) for attributes applied to the unnamed receiver parameter.
    * Can only declare static extension members.
* **Block-Level Generics:**
  * Extension block type parameters are exposed via `GetGenericParameters()`.
  * Placeholders within extension member signatures link to `GenericExtensionBlock(IExtensionsBlock)` record.
* **Structural Identity & Equality:**
  * Equality comparer is omitted for now.

---

## 5. Type Usages & Factory Entry Points

### The Type Usage Context Boundary
In the .NET metadata model, rich type usage annotations (calling conventions, nullability, tuple element names, `ref readonly` mutability) **do not exist on a standalone `System.Type` descriptor**. They only exist within the context of a **Member Signature** (`FieldInfo`, `PropertyInfo`, `ParameterInfo`, `MethodInfo.ReturnParameter`) or a **Roslyn Symbol**.

### Factory Entry Points
1. **Declared Type Entry Point:**
   * `Rectaleaf.FromDeclaredType(Type)` / `FromSymbol(INamedTypeSymbol)` -> `IDeclaredType`
   * Only accepts nominal type definitions (`class`, `struct`, `record`, `interface`, `enum`, `delegate`).
   * Throws `ArgumentException` if passed an unanchored type usage (`IsPointer`, `IsArray`, `IsByRef`, `IsFunctionPointer`, or `IsGenericParameter`).
2. **Member Entry Points:**
   * `Rectaleaf.FromField(FieldInfo)` -> `IField`
   * `Rectaleaf.FromProperty(PropertyInfo)` -> `IProperty`
   * `Rectaleaf.FromMethod(MethodInfo)` -> `IMethod`
   * `Rectaleaf.FromParameter(ParameterInfo)` -> `IParameter`
   * Member models expose their full type usage with 100% fidelity.

### Preserving Contextual Modifiers in Reflection (ModifiedType Strategy)
When constructing type usage models for members, the Reflection backend **must not** rely solely on `field.FieldType`, `prop.PropertyType`, or `param.ParameterType` (which return stripped `RuntimeType` instances).
Instead, it must query modified types:
* Fields -> `field.GetModifiedFieldType()`
* Properties -> `property.GetModifiedPropertyType()`
* Parameters -> `param.GetModifiedParameterType()`
* Method returns -> `method.ReturnParameter.GetModifiedParameterType()`

### `IUnmanagedFunctionPointerType`
* **Specification:** Represents an unmanaged function pointer type usage (`delegate* unmanaged[Cdecl]<int, void>`).
* **Calling Convention Properties:**
  * `CallingConventions` (`IReadOnlyList<CallConvKind>`): The ordered collection of calling convention kinds as declared in source code, preserving duplicates and syntax order. Empty if calling conventions were omitted (`delegate* unmanaged<...>`).
  * `CallingConventionFlags` (`CallConvFlags`): The bitmask representation for fast bitwise checking and name formatting. `CallConvFlags.None` if omitted.
* **Backend Extraction Rules:**
  * **Roslyn Backend:** Reads `IFunctionPointerTypeSymbol.Signature.UnmanagedCallingConventionTypes` directly.
  * **Reflection Backend:** Calls `GetModifiedFieldType().GetFunctionPointerCallingConventions()` (or property/parameter modified type equivalent).

### Tuple Resolution
* **Roslyn Backend:**
  * Created via tuple syntax (e.g. `(int, string)` or `(int Id, string Name)`) -> `IRichTupleType`
  * Created via explicit generic struct syntax (e.g. `ValueTuple<int, string>`) -> `IExplicitValueTupleType`
* **Reflection Backend:**
  * Decorated with `[TupleElementNamesAttribute]` -> `IRichTupleType` (with named elements).
  * Without `[TupleElementNamesAttribute]` -> `IObliviousTupleType` (flat collection of element types, as Reflection cannot distinguish whether source code wrote `ValueTuple<int, string>` or `(int, string)`).

### Nullability Resolution
* **Nullable Value Types:**
  Both `Nullable<T>` and `T?` where `T : struct` must be mapped to a type usage of `T` with nullability set to `NullabilityKind.Nullable`. `System.Nullable<T>` is never modeled as a `BoundGenericType`.
* **Oblivious Contexts:**
  When code is compiled with nullability disabled via `#nullable disable`, nullability of reference type models must be set to `NullabilityKind.Oblivious`. Nullability of value type models should not be affected by `#nullable disable` and should still behave as described above.

### By-Ref Parameter Type Usages (`IByRefType`)
* When an `IParameter` has a by-ref modifier (`Ref`, `Out`, `In`, or `RefReadOnly` in `ParameterModifiers`), its `Type` property must be wrapped in `IByRefType<TKind, TMutability>`:
  * `ParameterModifiers.Ref` and `ParameterModifiers.Out` map to `IByRefType<TKind, IMut>` (`LocalName` formatted via `WrapperNameFormatter.RefName`, e.g. `"ref int"`).
  * `ParameterModifiers.In` and `ParameterModifiers.RefReadOnly` map to `IByRefType<TKind, IReadOnly>` (`LocalName` formatted via `WrapperNameFormatter.RefReadOnlyName`, e.g. `"ref readonly int"`).
* `ParameterModifiers.Scoped` applies when the `scoped` modifier is declared on a parameter or when `[ScopedRefAttribute]` is present in metadata. Because Roslyn implicitly sets `IParameterSymbol.ScopedKind` to `ScopedKind.ScopedRef` on all `out` parameters under C# 11 ref safety rules without emitting `[ScopedRefAttribute]`, backends must not treat an `out` parameter as scoped merely because `ScopedKind` is `ScopedRef`. If a parameter has the `out` modifier, `ScopedKind.ScopedRef` is ignored unless `scoped` was explicitly declared in syntax or `[ScopedRefAttribute]` is present in metadata.

### Member Naming for Explicit Interface Implementations
* Across all member models (`IMethod`, `IProperty`, `IIndexer`, `IEvent`), `Name` returns strictly the simple member identifier declared in the interface (e.g. `Dispose`, `PerformWork`, `WorkerName`, `Item`, `WorkCompleted`).
* The implemented interface name is **never** prepended to `Name`.
* Consumers query the target interface and implemented member via `Inheritance`:
  * When `Inheritance` implements `IInheritanceExplicitImplementation<TMember>`, `ExplicitInterfaceMember` returns the interface member model directly, providing the target interface context without name mangling.

### Indexer Naming & Attribute Erasure Rules
* **Indexer Names:**
  * An indexer's `Name` property returns `"Item"` if `[IndexerName("CustomName")]` is absent.
  * An indexer declared with `[IndexerName("CustomName")]` has `Name` set to the custom name (e.g. `"Grid"`).
  * An explicit interface implementation indexer has `Name` set to `"Item"`. Applying `[IndexerName]` directly to an explicit interface member declaration is invalid C# syntax (compiler error CS0415). Even if the target interface indexer was declared with `[IndexerName("CustomName")]`, the implementing type's explicit implementation member is emitted as `Interface.Item` in CLI metadata and reflection, and its `Name` property returns `"Item"`.
* **`IndexerNameAttribute` Filtering:**
  * `IndexerNameAttribute` serves as compiler instruction to rename the metadata property and is not emitted into PE metadata.
  * `IndexerNameAttribute` is compiler plumbing and must never appear in `IIndexer.GetAttributes()`. The chosen name is exposed via `IIndexer.Name`.
* **`DefaultMemberAttribute` Filtering:**
  * `DefaultMemberAttribute` is synthesized by the C# compiler on any type declaring an indexer during the PE emit phase.
  * As compiler plumbing, `DefaultMemberAttribute` is filtered out lazily from `IType.GetAttributes()` if the declaring type contains indexers.
  * If a user manually places `[DefaultMember]` on a type that contains no indexers, the attribute is preserved as a user-authored attribute.

---

## 6. Backend Realization & Reflection Strategy Guide

This reference section documents the low-level metadata signals and strategies used by the Reflection backend to fulfill the semantic specifications deterministically without heuristics or runtime code execution risks.

### Metadata Detection Signals & Reliability Tiers

| Target Concept / Filter | Detection Signal in Reflection | Reliability Tier |
| :--- | :--- | :--- |
| **Accessors, Operators, Ctors** (exclude from `GetMethods`) | `MethodAttributes.SpecialName` (ECMA-335 flag) + method prefix check (`get_`, `set_`, `add_`, `remove_`, `op_`) | **Bulletproof (ECMA-335 Spec)** |
| **Value vs Ref, Sealed, Abstract, Interface, Enum** | `TypeAttributes` (`Type.IsValueType`, `Type.IsSealed`, `Type.IsAbstract`, `Type.IsInterface`, `Type.IsEnum`) | **Bulletproof (ECMA-335 Spec)** |
| **File-Local Types** | `type.IsNotPublic && !type.IsNested && type.Name.StartsWith("<") && type.Name.Contains("__")` | **Bulletproof (Roslyn Name Mangling Spec)** |
| **Fixed-Size Buffers** | `[FixedBufferAttribute]` on the field (exposes `ElementType` and `Length`) | **Bulletproof (Official BCL Type)** |
| **Inline Arrays** | `[InlineArrayAttribute]` on the struct (exposes `Length` and `ElementType` via single field) | **Bulletproof (Official BCL Attribute)** |
| **Indexer Naming** | Read `PropertyInfo.Name` directly; standard indexers are `"Item"`, renamed indexers carry the custom name. | **Bulletproof (CLI Property Spec)** |
| **Synthesized `DefaultMemberAttribute`** | Filter out lazily if the type contains declared indexers. | **Bulletproof Indexer Detection** |
| **`IndexerNameAttribute`** | Never present in PE metadata; Roslyn filters it from source symbols. | **Bulletproof Erasure Parity** |
| **Async / Iterator State Machines** | Nested types referenced by `[AsyncStateMachine]`, `[IteratorStateMachine]`, or `[AsyncIteratorStateMachine]` on containing methods | **Bulletproof (Official BCL Attribute)** |
| **`readonly struct`, `ref struct`** | `[IsReadOnlyAttribute]`, `[IsByRefLikeAttribute]` | **Bulletproof** |
| **`required` Members** | `[RequiredMemberAttribute]` + `[CompilerFeatureRequiredAttribute("RequiredMembers")]` | **Bulletproof (Official BCL Attribute)** |
| **`scoped ref` Parameter Modifiers** | `[ScopedRefAttribute]` decorating the parameter | **Bulletproof (Official BCL Attribute)** |
| **Unmanaged Generic Constraint** | `[IsUnmanagedAttribute]` decorating the generic type parameter representation | **Bulletproof (Attribute Check)** |
| **`allows ref struct` Anti-Constraint** | `[CompilerFeatureRequired("RefStructs")]` / `[AllowsRefStructAttribute]` on the generic parameter | **Bulletproof (Official BCL Attribute)** |
| **Compiler Plumbing (Closures, Backing Fields, Ordinal Local Functions)** | Member metadata name contains non-lexable characters (`<`, `>`, `|`, `$`), e.g. `<>c__DisplayClass`, `<Prop>k__BackingField`, `<M>g__Local\|0_0`, `<Clone>$` | **Bulletproof Guarantee:** `< > \| $` cannot be written in C# source identifiers per the C# lexer grammar. |
| **Extension Declarations (C# 14)** | Nested types named `<G>$<Hash>` decorated with `[ExtensionAttribute]` containing members decorated with `[ExtensionMarkerAttribute]` | **Bulletproof (Official Compiler Lowering Spec)** |
| **Direct Interfaces Subtraction** | `type.GetInterfaces().Except(baseInterfaces).Except(transitiveParentInterfaces)` | **Deterministic Set Subtraction** |
| **Direct Generic Parameters Slicing** | `type.GetGenericArguments().Skip(containingTypeGenericCount)` | **Deterministic Slicing** |

### Critical Implementation Guidelines for Reflection Backend

1. **Attribute Comparison Rule (Always compare `FullName`):**
   * Some compiler-injected marker attributes (like `IsUnmanagedAttribute` or internal `NullableAttribute`) are synthesized locally per-assembly by Roslyn.
   * **Rule:** Never check `attr.GetType() == typeof(...)` identity across assembly boundaries. Always compare via `attr.GetType().FullName == "System.Runtime.CompilerServices.IsUnmanagedAttribute"` to ensure cross-assembly reliability.

2. **Synthetic `[Obsolete]` Compatibility Gates:**
   * When Roslyn emits `ref struct` types or constructors of types with `required` members, it automatically synthesizes `[Obsolete(..., error: true)]` compatibility gates to block legacy C# compilers.
   * **Rule for Parity across Backends:** Both Roslyn and Reflection backends filter out `[Obsolete]` if its message argument matches the compiler's invariant gate strings:
     * `"Types with embedded references are not supported in this version of your compiler."` (on `ref struct` types)
     * `"Constructors of types with required members are not supported in this version of your compiler."` (on constructors of types with `required` members)
   * User-authored `[Obsolete]` attributes with any other message (or parameterless) are preserved.

3. **Loading Strategy: `MetadataLoadContext` vs. Standard Reflection:**
   * **Standard Reflection (`Assembly.Load`):** Loads assemblies into the active runtime process. Can inadvertently trigger static initializers (`.cctor`), locks DLL files on disk, and fails when inspecting assemblies targeted for different runtime versions.
   * **`MetadataLoadContext` (`System.Reflection.MetadataLoadContext`):** An official, high-level, read-only reflection container.
     * Exposes the **exact same `Type`, `MethodInfo`, `FieldInfo`, `PropertyInfo`** APIs as standard Reflection.
     * Runs **purely in data-only mode** (never executes any code or `.cctor`).
     * Inspects assemblies targeting any framework (.NET Framework, .NET Core, .NET Standard) without dependency conflicts.
     * Fully disposable (releases disk file locks immediately).
