namespace TestCases;

using System;
using System.Collections.Generic;

[Flags]
public enum CustomColorFlags : short
{
    None = 0,
    Red = 1,
    Green = 2,
    Blue = 4,
    Alpha = 8
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class TypeArgAttribute : Attribute
{
    public Type TargetType { get; }

    public TypeArgAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}

[AttributeUsage(AttributeTargets.All)]
public class PrimitiveArrayAttribute : Attribute
{
    public int[] Numbers { get; }

    public PrimitiveArrayAttribute(int[] numbers)
    {
        Numbers = numbers;
    }
}

[AttributeUsage(AttributeTargets.All)]
public class HeterogeneousArrayAttribute : Attribute
{
    public object[] Items { get; }

    public HeterogeneousArrayAttribute(object[] items)
    {
        Items = items;
    }
}

[AttributeUsage(AttributeTargets.All)]
public class EnumFlagAttribute : Attribute
{
    public AttributeTargets Targets { get; }
    public CustomColorFlags Colors { get; set; }

    public EnumFlagAttribute(AttributeTargets targets)
    {
        Targets = targets;
    }
}

[AttributeUsage(AttributeTargets.All)]
public class UndefinedBitFlagAttribute : Attribute
{
    public CustomColorFlags Flags { get; }

    public UndefinedBitFlagAttribute(CustomColorFlags flags)
    {
        Flags = flags;
    }
}

[AttributeUsage(AttributeTargets.All)]
public class ParamsConstructorAttribute : Attribute
{
    public int Priority { get; }
    public string[] Tags { get; }

    public ParamsConstructorAttribute(int priority, params string[] tags)
    {
        Priority = priority;
        Tags = tags;
    }
}

[AttributeUsage(AttributeTargets.All)]
public class NamedPropertiesAttribute : Attribute
{
    public int Id { get; }
    public string Title { get; set; } = string.Empty;
    public double Weight { get; set; }
    public bool IsActive;
    public object? ExtraData { get; set; }

    public NamedPropertiesAttribute(int id)
    {
        Id = id;
    }
}

[AttributeUsage(AttributeTargets.GenericParameter)]
public class TypeParamTargetAttribute : Attribute
{
    public string Description { get; }

    public TypeParamTargetAttribute(string description)
    {
        Description = description;
    }
}

// Applying custom attributes across targets and argument forms
[TypeArg(typeof(Dictionary<,>))]
[TypeArg(typeof(List<string>))]
[PrimitiveArray(new int[] { 10, 20, 30, 40 })]
[HeterogeneousArray(new object[] { 1, "test_string", typeof(void), true, 3.14 })]
[EnumFlag(AttributeTargets.Class | AttributeTargets.Struct, Colors = CustomColorFlags.Red | CustomColorFlags.Blue)]
[UndefinedBitFlag((CustomColorFlags)0x1234)]
[ParamsConstructor(1, "alpha", "beta", "gamma")]
[NamedProperties(42, Title = "PrimaryClass", Weight = 9.5, IsActive = true, ExtraData = typeof(int[]))]
public class AttributeDecoratedClass<[TypeParamTarget("Generic parameter marker")] TKey, TValue>
{
    [NamedProperties(100, Title = "FieldMarker", IsActive = false)]
    public int AnnotatedField;

    [ParamsConstructor(5, "get_tag", "prop_tag")]
    public string AnnotatedProperty { get; set; } = string.Empty;

    [return: TypeArg(typeof(IEnumerable<>))]
    [NamedProperties(200, Title = "MethodReturn")]
    public List<TValue> AnnotatedMethod(
        [TypeArg(typeof(KeyValuePair<,>))]
        [NamedProperties(300, Title = "ParameterMarker")]
        TKey key,
        [ParamsConstructor(99, "inline_tag")]
        int flag)
    {
        return new List<TValue>();
    }
}
