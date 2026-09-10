namespace TestCases;

using System;
using System.Collections.Generic;

// Generic interface with self-referential and class constraints
public interface IGenericTree<TNode>
    where TNode : class, IComparable<TNode>, new()
{
    TNode Root { get; set; }
}

// C# 13 anti-constraint: allows ref struct
public ref struct RefStructContainer<TVal>
    where TVal : allows ref struct
{
    public TVal Value;

    public RefStructContainer(TVal value)
    {
        Value = value;
    }
}

// Nullable reference type constraint distinction: class vs class?
public class NullableConstraintContainer<TStrict, TNullable>
    where TStrict : class
    where TNullable : class?
{
    public TStrict StrictRef = default!;
    public TNullable NullableRef = default!;
}

// Unmanaged, struct, and notnull constraints
public class UnmanagedContainer<TUnmanaged, TValueType, TNotNull>
    where TUnmanaged : unmanaged
    where TValueType : struct
    where TNotNull : notnull
{
    public TUnmanaged UnmanagedField;
    public TValueType ValueTypeField;
    public TNotNull NotNullField = default!;
}

// Naked type parameter constraints: T : U, and U : V
public class HierarchyConstraintContainer<TChild, TParent, TGrandParent>
    where TGrandParent : class
    where TParent : TGrandParent
    where TChild : TParent, new()
{
    public TChild CreateInstance() => new();
}

// Default constraint in unconstrained nullable contexts (valid on override and explicit interface methods)
public interface IDefaultConstraintInterface<T>
{
    void HandleItem<TItem>(TItem item);
}

public class BaseDefaultConstraintClass
{
    public virtual void ProcessItem<TItem>(TItem item)
    {
    }
}

public class DefaultConstraintContainer : BaseDefaultConstraintClass, IDefaultConstraintInterface<string>
{
    // Explicit interface implementation with default constraint
    void IDefaultConstraintInterface<string>.HandleItem<TItem>(TItem item) where TItem : default
    {
    }

    // Override method with default constraint
    public override void ProcessItem<TItem>(TItem item) where TItem : default
    {
    }
}

// Multi-constraint with new(), interface, self-reference
public class MultiConstraintNode<TKey, TItem>
    where TKey : struct, IComparable<TKey>, IEquatable<TKey>
    where TItem : class, IEnumerable<TKey>, new()
{
    public TKey Key;
    public TItem Item = new();

    // Method with its own distinct generic parameters and constraints
    public TResult ExecuteTransform<TInput, TResult>(TInput input)
        where TInput : allows ref struct
        where TResult : class?, new()
    {
        return new TResult();
    }

    // Nested generic class sharing outer and introducing inner type parameters
    public class NestedNode<TNested>
        where TNested : TItem
    {
        public TNested NestedField = default!;

        public void Process<TLocalMethodParam>(TLocalMethodParam param)
            where TLocalMethodParam : unmanaged
        {
        }
    }
}
