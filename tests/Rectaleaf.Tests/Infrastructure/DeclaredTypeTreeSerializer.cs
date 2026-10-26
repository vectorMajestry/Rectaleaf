using Rectaleaf.Components;
using Rectaleaf.Metadata;
using Rectaleaf.Metadata.DeclaredTypes;
using Rectaleaf.Metadata.Members;
using Rectaleaf.Metadata.Members.PropertyFamily;
using Rectaleaf.Metadata.Members.PropertyFamily.Events;
using Rectaleaf.Metadata.Members.PropertyFamily.Indexers;
using Rectaleaf.Metadata.Members.PropertyFamily.Properties;
using Rectaleaf.Metadata.TypeUsages.Interfaces;
using System.Text.Json.Nodes;

namespace Rectaleaf.Tests.Infrastructure;

public static class DeclaredTypeTreeSerializer
{
    public static JsonObject Serialize(IDeclaredType type)
    {
        ArgumentNullException.ThrowIfNull(type, nameof(type));

        JsonObject node = new()
        {
            ["kind"] = GetTypeKind(type),
            ["localName"] = type.LocalName,
            ["fullyQualifiedName"] = type.FullyQualifiedName,
            ["namespace"] = type.Namespace,
            ["accessModifier"] = type.AccessModifier.ToString(),
            ["containingType"] = type.ContainingType?.FullyQualifiedName
        };

        // Leaf specific properties

        switch (type)
        {
            case IInlineArrayType inlineArr:
                node["length"] = inlineArr.Length;
                node["isReadOnly"] = inlineArr.IsReadOnly;
                node["elementType"] = SerializeTypeUsage(inlineArr.ElementType);
                break;

            case IStructType structType:
                node["isReadOnly"] = structType.IsReadOnly;
                break;

            case IRefStructType refStructType:
                node["isReadOnly"] = refStructType.IsReadOnly;
                break;

            case IRecordStructType recordStructType:
                node["isReadOnly"] = recordStructType.IsReadOnly;
                break;

            case IRegularClassType regularClassType:
                node["isSealed"] = regularClassType.IsSealed;
                node["baseType"] = regularClassType.BaseType is not null ? SerializeTypeUsage(regularClassType.BaseType) : null;
                break;

            case IAbstractClassType abstractClassType:
                node["baseType"] = abstractClassType.BaseType is not null ? SerializeTypeUsage(abstractClassType.BaseType) : null;
                break;

            case IRecordClassType recordClassType:
                node["isSealed"] = recordClassType.IsSealed;
                node["baseType"] = recordClassType.BaseType is not null ? SerializeTypeUsage(recordClassType.BaseType) : null;
                break;

            case IDelegateType delegateType:
                node["returnInfo"] = SerializeReturnInfo(delegateType.ReturnInfo);
                node["parameters"] = SerializeParameters(delegateType.GetParameters());
                break;

            case IEnumType enumType:
                node["underlyingType"] = SerializeTypeUsage(enumType.GetUnderlyingType());
                JsonArray membersArr = new();

                foreach (IEnumMember m in enumType.Members)
                    membersArr.Add(new JsonObject
                    {
                        ["name"] = m.Name,
                        ["value"] = m.Value?.ToString(),
                        ["attributes"] = SerializeAttributes(m.GetAttributes())
                    });

                node["enumMembers"] = membersArr;
                break;
        }

        // Capabilities

        if (type is IHasGenericParameters hasGen)
            node["genericParameters"] = SerializeGenericParameters(hasGen.GetGenericParameters());

        if (type is IHasInterfaces hasInterfaces)
            node["directInterfaces"] = hasInterfaces.GetDirectInterfaces().Select(SerializeTypeUsage).ToJsonArray();

        if (type is IHasFields hasFields)
            node["fields"] = hasFields.GetFields().Select(SerializeField).ToJsonArray();

        if (type is IHasMethods hasMethods)
            node["methods"] = hasMethods.GetMethods().Select(SerializeMethod).ToJsonArray();

        if (type is IHasProperties hasProps)
            node["properties"] = hasProps.GetProperties().Select(SerializeProperty).ToJsonArray();

        if (type is IHasIndexers hasIndexers)
            node["indexers"] = hasIndexers.GetIndexers().Select(SerializeIndexer).ToJsonArray();

        if (type is IHasEvents hasEvents)
            node["events"] = hasEvents.GetEvents().Select(SerializeEvent).ToJsonArray();

        if (type is IHasInstanceConstructors hasCtors)
            node["instanceConstructors"] = hasCtors.GetInstanceConstructors().Select(SerializeConstructor).ToJsonArray();

        if (type is IHasStaticConstructor hasStaticCtor)
            node["staticConstructor"] = hasStaticCtor.StaticConstructor is not null
                ? SerializeStaticConstructor(hasStaticCtor.StaticConstructor)
                : null;

        if (type is IHasOperators hasOps)
            node["operators"] = hasOps.GetOperators().Select(SerializeOperator).ToJsonArray();

        if (type is IHasExtensionsBlocks hasExts)
            node["extensionsBlocks"] = hasExts.GetExtensionsBlocks().Select(SerializeExtensionBlock).ToJsonArray();

        // Attributes
        node["attributes"] = SerializeAttributes(type.GetAttributes());

        // Nested types (recursive traversal)
        if (type is IHasNestedTypes hasNested)
            node["nestedTypes"] = hasNested.GetNestedTypes().Select(Serialize).ToJsonArray();

        return node;
    }

    static string GetTypeKind(IDeclaredType type) => type switch
    {
        IInlineArrayType => "InlineArray",
        IRefStructType => "RefStruct",
        IRecordStructType => "RecordStruct",
        IStructType => "Struct",
        IRegularClassType => "RegularClass",
        IAbstractClassType => "AbstractClass",
        IRecordClassType => "RecordClass",
        IStaticClassType => "StaticClass",
        IInterfaceType => "Interface",
        IDelegateType => "Delegate",
        IEnumType => "Enum",
        _ => throw new InvalidOperationException("Unreachable case.")
    };

    static JsonObject SerializeField(IField<IKnownOrNot> field)
    {
        JsonObject node = new()
        {
            ["name"] = field.Name,
            ["declaringType"] = field.DeclaringType.FullyQualifiedName,
            ["accessModifier"] = field.AccessModifier.ToString(),
            ["attributes"] = SerializeAttributes(field.GetAttributes())
        };

        if (field is IRegularField<IKnownOrNot> regField)
        {
            node["fieldKind"] = "Regular";
            node["type"] = SerializeTypeUsage(regField.Type);
            node["isReadOnly"] = regField.IsReadOnly;
            node["isVolatile"] = regField.IsVolatile;
            node["isStatic"] = regField is IStaticField<IKnownOrNot>;
            if (regField is IInstanceField<IKnownOrNot> instField)
            {
                node["isRequired"] = instField.IsRequired;
            }
        }
        else if (field is ConstField constField)
        {
            node["fieldKind"] = "Const";
            node["type"] = SerializeTypeUsage(constField.Type);
            node["isConst"] = true;
            node["constValue"] = constField.GetConstantValue()?.ToString();
        }
        else if (field is FixedSizeBufferField fixedField)
        {
            node["fieldKind"] = "FixedSizeBuffer";
            node["type"] = SerializeTypeUsage(fixedField.Type);
            node["length"] = fixedField.Length;
        }
        else if (field is IRefField<IKnownOrNot, IMutOrNot> refField)
        {
            node["fieldKind"] = "Ref";
            node["type"] = SerializeTypeUsage(refField.Type);
        }
        else
        {
            throw new InvalidOperationException($"Unrecognized field kind: {field.GetType().FullName}");
        }

        return node;
    }

    static JsonObject SerializeMethod(IMethod method)
    {
        JsonObject node = new()
        {
            ["name"] = method.Name,
            ["declaringType"] = method.DeclaringType.FullyQualifiedName,
            ["returnInfo"] = SerializeReturnInfo(method.ReturnInfo),
            ["isStatic"] = method is IStaticMethod,
            ["isGeneric"] = method.IsGeneric,
            ["isReadOnly"] = method.IsReadOnly,
            ["hasArglistParameter"] = method.HasArglistParameter,
            ["accessModifier"] = method.AccessModifier.ToString(),
            ["inheritance"] = SerializeInheritance(method.Inheritance),
            ["parameters"] = SerializeParameters(method.GetParameters()),
            ["genericParameters"] = SerializeGenericParameters(method.GetGenericParameters()),
            ["attributes"] = SerializeAttributes(method.GetAttributes())
        };

        node["localFunctions"] = method.GetLocalFunctions().Select(SerializeLocalFunction).ToJsonArray();
        return node;
    }

    static string FormatDeclaringEntity(IHasLocalFunctions entity) => entity switch
    {
        IMethod m => m.Name,
        ILocalFunction lf => lf.Name,
        IInstanceConstructor => ".ctor",
        IStaticConstructor => ".cctor",
        IOperator op => op switch
        {
            IUnaryOperator u => $"op_{u.Kind}",
            IBinaryOperator b => $"op_{b.Kind}",
            IConversionOperator c => $"op_{c.Kind}",
            _ => "operator"
        },
        IPropertyAccessor<IKnownOrNot> propAcc => propAcc switch
        {
            IPropertyGetAccessor<IKnownOrNot> => $"get_{propAcc.DeclaringProperty?.Name ?? "property"}",
            IPropertySetAccessor<IKnownOrNot> => $"set_{propAcc.DeclaringProperty?.Name ?? "property"}",
            IPropertyInitAccessor<IKnownOrNot> => $"init_{propAcc.DeclaringProperty?.Name ?? "property"}",
            _ => $"{propAcc.DeclaringProperty?.Name ?? "property"}.accessor"
        },
        IIndexerAccessor<IKnownOrNot> idxAcc => idxAcc switch
        {
            IIndexerGetAccessor<IKnownOrNot> => $"get_{idxAcc.DeclaringIndexer?.Name ?? "indexer"}",
            IIndexerSetAccessor<IKnownOrNot> => $"set_{idxAcc.DeclaringIndexer?.Name ?? "indexer"}",
            IIndexerInitAccessor<IKnownOrNot> => $"init_{idxAcc.DeclaringIndexer?.Name ?? "indexer"}",
            _ => $"{idxAcc.DeclaringIndexer?.Name ?? "indexer"}.accessor"
        },
        IEventAccessor<IKnownOrNot> evtAcc => evtAcc switch
        {
            IEventAddAccessor<IKnownOrNot> => $"add_{evtAcc.DeclaringEvent?.Name ?? "event"}",
            IEventRemoveAccessor<IKnownOrNot> => $"remove_{evtAcc.DeclaringEvent?.Name ?? "event"}",
            _ => $"{evtAcc.DeclaringEvent?.Name ?? "event"}.accessor"
        },
        _ => throw new InvalidOperationException($"Unrecognized declaring entity: {entity.GetType().FullName}")
    };

    static JsonObject SerializeLocalFunction(ILocalFunction lf) => new()
    {
        ["name"] = lf.Name,
        ["ordinal"] = lf.Ordinal,
        ["isStatic"] = lf.IsStatic,
        ["isGeneric"] = lf.IsGeneric,
        ["declaringEntity"] = FormatDeclaringEntity(lf.DeclaringEntity),
        ["returnInfo"] = SerializeReturnInfo(lf.ReturnInfo),
        ["parameters"] = SerializeParameters(lf.GetParameters()),
        ["genericParameters"] = SerializeGenericParameters(lf.GetGenericParameters()),
        ["attributes"] = SerializeAttributes(lf.GetAttributes()),
        ["localFunctions"] = lf.GetLocalFunctions().Select(SerializeLocalFunction).ToJsonArray()
    };

    static JsonObject SerializeProperty(IProperty<IKnownOrNot, IAccess> prop)
    {
        ITypeUsage<IKnownOrNot>? typeUsage = prop switch
        {
            IRegularProperty<IKnownOrNot, IAccess> reg => reg.Type,
            IRefProperty<IKnownOrNot, IMutOrNot> rf => rf.Type,
            _ => null
        };

        bool isStatic = prop is IStaticProperty<IKnownOrNot, IAccess>
            || prop is IStaticRefProperty<IKnownOrNot, IMutOrNot>;

        JsonObject node = new()
        {
            ["name"] = prop.Name,
            ["declaringType"] = prop.DeclaringType.FullyQualifiedName,
            ["type"] = typeUsage is not null ? SerializeTypeUsage(typeUsage) : null,
            ["accessModifier"] = prop.AccessModifier.ToString(),
            ["isStatic"] = isStatic,
            ["inheritance"] = SerializeInheritance(prop.Inheritance),
            ["attributes"] = SerializeAttributes(prop.GetAttributes())
        };

        List<JsonNode?> accessors = [];
#pragma warning disable CS0612
        if (prop.Accessors is IGet g) accessors.Add(SerializeAccessor("get", g.Getter));
        if (prop.Accessors is ISet s) accessors.Add(SerializeAccessor("set", s.Setter));
        if (prop.Accessors is IInit i) accessors.Add(SerializeAccessor("init", i.InitAccessor));
#pragma warning restore CS0612
        node["accessors"] = accessors.ToJsonArray();

        if (prop is IRegularProperty<IKnownOrNot, IAccess> regProp && regProp.BackingField is not null)
        {
            node["backingFieldName"] = regProp.BackingField.Name;
        }

        return node;
    }

    static JsonObject SerializeIndexer(IIndexer<IKnownOrNot, IAccess> idx)
    {
        ITypeUsage<IKnownOrNot>? typeUsage = idx switch
        {
            IRegularIndexer<IKnownOrNot, IAccess> reg => reg.Type,
            IRefIndexer<IKnownOrNot, IMutOrNot> rf => rf.Type,
            _ => null
        };

        JsonObject node = new()
        {
            ["name"] = idx.Name,
            ["declaringType"] = idx.DeclaringType.FullyQualifiedName,
            ["type"] = typeUsage is not null ? SerializeTypeUsage(typeUsage) : null,
            ["accessModifier"] = idx.AccessModifier.ToString(),
            ["inheritance"] = SerializeInheritance(idx.Inheritance),
            ["parameters"] = SerializeParameters(idx.GetParameters()),
            ["attributes"] = SerializeAttributes(idx.GetAttributes())
        };

        List<JsonNode?> accessors = [];
#pragma warning disable CS0612
        if (idx.Accessors is IGet g) accessors.Add(SerializeAccessor("get", g.Getter));
        if (idx.Accessors is ISet s) accessors.Add(SerializeAccessor("set", s.Setter));
        if (idx.Accessors is IInit i) accessors.Add(SerializeAccessor("init", i.InitAccessor));
#pragma warning restore CS0612
        node["accessors"] = accessors.ToJsonArray();

        return node;
    }

    static JsonObject SerializeEvent(IEvent<IKnownOrNot> evt)
    {
        JsonObject node = new()
        {
            ["name"] = evt.Name,
            ["declaringType"] = evt.DeclaringType.FullyQualifiedName,
            ["type"] = SerializeTypeUsage(evt.Type),
            ["accessModifier"] = evt.AccessModifier.ToString(),
            ["isStatic"] = evt is IStaticEvent<IKnownOrNot>,
            ["inheritance"] = SerializeInheritance(evt.Inheritance),
            ["attributes"] = SerializeAttributes(evt.GetAttributes())
        };

        List<JsonNode?> accessors = [];
        if (evt.AddAccessor is not null) accessors.Add(SerializeAccessor("add", evt.AddAccessor));
        if (evt.RemoveAccessor is not null) accessors.Add(SerializeAccessor("remove", evt.RemoveAccessor));
        node["accessors"] = accessors.ToJsonArray();

        return node;
    }

    static JsonObject SerializeAccessor(string kind, IAccessor<IKnownOrNot> accessor)
    {
        JsonObject node = new()
        {
            ["kind"] = kind,
            ["isReadOnly"] = accessor.IsReadOnly,
            ["returnInfo"] = SerializeReturnInfo(accessor.ReturnInfo),
            ["attributes"] = SerializeAttributes(accessor.GetAttributes())
        };

        if (accessor is IPropertyAccessor<IKnownOrNot> propAcc)
        {
            node["accessModifier"] = propAcc.AccessModifier.ToString();
        }

        if (accessor is IPropertyWriteAccessor<IKnownOrNot> propWrite)
        {
            node["valueParameter"] = SerializeParameter(propWrite.ValueParameter, 0);
        }

        if (accessor is IIndexerAccessor<IKnownOrNot> idxAcc)
        {
            node["accessModifier"] = idxAcc.AccessModifier.ToString();
            node["parameters"] = SerializeParameters(idxAcc.GetParameters());
        }

        if (accessor is IEventAccessor<IKnownOrNot> evtAcc)
        {
            node["valueParameter"] = SerializeParameter(evtAcc.ValueParameter, 0);
        }

        node["localFunctions"] = accessor.GetLocalFunctions().Select(SerializeLocalFunction).ToJsonArray();
        return node;
    }

    static JsonObject SerializeConstructor(IInstanceConstructor ctor)
    {
        return new JsonObject
        {
            ["declaringType"] = ctor.DeclaringType.FullyQualifiedName,
            ["accessModifier"] = ctor.AccessModifier.ToString(),
            ["isPrimary"] = ctor.IsPrimary,
            ["hasArglistParameter"] = ctor.HasArglistParameter,
            ["parameters"] = SerializeParameters(ctor.GetParameters()),
            ["attributes"] = SerializeAttributes(ctor.GetAttributes()),
            ["localFunctions"] = ctor.GetLocalFunctions().Select(SerializeLocalFunction).ToJsonArray()
        };
    }

    static JsonObject SerializeStaticConstructor(IStaticConstructor sctor)
    {
        return new JsonObject
        {
            ["declaringType"] = sctor.DeclaringType.FullyQualifiedName,
            ["attributes"] = SerializeAttributes(sctor.GetAttributes()),
            ["localFunctions"] = sctor.GetLocalFunctions().Select(SerializeLocalFunction).ToJsonArray()
        };
    }

    static JsonObject SerializeOperator(IOperator op)
    {
        JsonObject node = new()
        {
            ["declaringType"] = op.DeclaringType.FullyQualifiedName,
            ["accessModifier"] = op.AccessModifier.ToString(),
            ["inheritance"] = SerializeInheritance(op.Inheritance)
        };

        if (op is IUnaryOperator uop)
        {
            node["operatorKind"] = "Unary";
            node["unaryKind"] = uop.Kind.ToString();
            node["isChecked"] = uop.IsChecked;
            node["operand"] = SerializeParameter(uop.Operand, 0);
            node["returnInfo"] = SerializeReturnInfo(uop.ReturnInfo);
        }
        else if (op is IBinaryOperator bop)
        {
            node["operatorKind"] = "Binary";
            node["binaryKind"] = bop.Kind.ToString();
            node["isChecked"] = bop.IsChecked;
            node["leftOperand"] = SerializeParameter(bop.LeftOperand, 0);
            node["rightOperand"] = SerializeParameter(bop.RightOperand, 1);
            node["returnInfo"] = SerializeReturnInfo(bop.ReturnInfo);
        }
        else if (op is IConversionOperator cop)
        {
            node["operatorKind"] = "Conversion";
            node["conversionKind"] = cop.Kind.ToString();
            node["operand"] = SerializeParameter(cop.Operand, 0);
            node["returnInfo"] = SerializeReturnInfo(cop.ReturnInfo);
        }
        else
        {
            throw new InvalidOperationException($"Unrecognized operator kind: {op.GetType().FullName}");
        }

        node["attributes"] = SerializeAttributes(op.GetAttributes());
        node["localFunctions"] = op.GetLocalFunctions().Select(SerializeLocalFunction).ToJsonArray();
        return node;
    }

    static JsonObject SerializeExtensionBlock(IExtensionsBlock ext)
    {
        JsonObject node = new()
        {
            ["declaringType"] = ext.DeclaringType.FullyQualifiedName,
            ["isStatic"] = ext is IStaticExtensionsBlock,
            ["isGeneric"] = ext.IsGeneric,
            ["extendedType"] = SerializeTypeUsage(ext.ExtendedType),
            ["genericParameters"] = SerializeGenericParameters(ext.GetGenericParameters())
        };

        if (ext is IInstanceExtensionsBlock instExt)
        {
            node["receiver"] = SerializeParameter(instExt.Receiver, 0);
        }
        else if (ext is IStaticExtensionsBlock statExt)
        {
            node["receiverAttributes"] = SerializeAttributes(statExt.GetReceiverAttributes());
        }

        node["methods"] = ext.GetMethods().Select(SerializeMethod).ToJsonArray();
        node["properties"] = ext.GetProperties().Select(SerializeProperty).ToJsonArray();
        node["indexers"] = ext.GetIndexers().Select(SerializeIndexer).ToJsonArray();

        return node;
    }

    static JsonObject SerializeReturnInfo(IReturnInfo<IKnownOrNot> returnInfo)
    {
        return new JsonObject
        {
            ["type"] = SerializeTypeUsage(returnInfo.Type),
            ["attributes"] = SerializeAttributes(returnInfo.GetAttributes())
        };
    }

    static JsonArray SerializeParameters(IReadOnlyList<IParameter> parameters)
        => parameters.Select(SerializeParameter).ToJsonArray();

    static JsonObject SerializeParameter(IParameter param, int position)
    {
        bool hasDef = param.TryGetDefaultValue(out object? defVal);
        return new JsonObject
        {
            ["name"] = param.Name,
            ["position"] = position,
            ["modifiers"] = param.Modifiers.ToString(),
            ["type"] = SerializeTypeUsage(param.Type),
            ["hasDefaultValue"] = hasDef,
            ["defaultValue"] = defVal?.ToString(),
            ["attributes"] = SerializeAttributes(param.GetAttributes())
        };
    }

    static string FormatGenericParameterSource(GenericParameterSource source) => source switch
    {
        GenericType gt => gt.Type.FullyQualifiedName,
        GenericMethod gm => gm.Method.Name,
        GenericLocalFunction glf => glf.LocalFunction.Name,
        GenericExtensionBlock => "ExtensionBlock",
        _ => throw new InvalidOperationException($"Unrecognized generic parameter source: {source.GetType().FullName}")
    };

    static JsonArray SerializeGenericParameters(IReadOnlyList<IGenericParameter> genericParams)
        => genericParams.Select(gp => (JsonNode)new JsonObject
        {
            ["name"] = gp.Name,
            ["position"] = gp.Position,
            ["variance"] = gp.Variance.ToString(),
            ["source"] = FormatGenericParameterSource(gp.Source),
            ["hasReferenceTypeConstraint"] = gp.HasReferenceTypeConstraint,
            ["hasNullableReferenceTypeConstraint"] = gp.HasNullableReferenceTypeConstraint,
            ["hasNotNullConstraint"] = gp.HasNotNullConstraint,
            ["hasValueTypeConstraint"] = gp.HasValueTypeConstraint,
            ["hasUnmanagedTypeConstraint"] = gp.HasUnmanagedTypeConstraint,
            ["hasConstructorConstraint"] = gp.HasConstructorConstraint,
            ["hasAllowsRefStructConstraint"] = gp.HasAllowsRefStructConstraint,
            ["hasDefaultConstraint"] = gp.HasDefaultConstraint,
            ["constraintTypes"] = gp.ConstraintTypes.Select(c => c switch
            {
                ConstraintPlaceholderType placeholder => (JsonNode)SerializeTypeUsage(placeholder.Type),
                ConstraintDeclaredType decl => (JsonNode)SerializeTypeUsage(decl.Type),
                _ => throw new InvalidOperationException($"Unrecognized constraint type: {c.GetType().FullName}")
            }).ToJsonArray(),
            ["attributes"] = SerializeAttributes(gp.GetAttributes())
        }).ToJsonArray();

    public static JsonObject SerializeTypeUsage(ITypeUsage<IKnownOrNot> usage)
    {
        JsonObject node = new()
        {
            ["localName"] = usage.LocalName
        };

        if (usage is IHasNullability hasNull)
        {
            node["nullability"] = hasNull.Nullability.ToString();
        }

        switch (usage)
        {
            case IPredefinedType predefined:
                node["usageKind"] = "Predefined";
                node["predefinedKind"] = predefined.Kind.ToString();
                break;

            case IFixedSizeBufferType fsb:
                node["usageKind"] = "FixedSizeBuffer";
                node["bufferKind"] = fsb.Kind.ToString();
                break;

            case IEnumUnderlyingType eut:
                node["usageKind"] = "EnumUnderlying";
                node["underlyingKind"] = eut.Kind.ToString();
                break;

            case IByRefType<IKnownOrNot, IMutOrNot> byRef:
                node["usageKind"] = "ByRef";
                node["isReadOnly"] = byRef is IByRefType<IKnownOrNot, IReadOnly>;
                node["elementType"] = SerializeTypeUsage(byRef.ElementType);
                break;

            case IPointerType<IKnownOrNot> ptr:
                node["usageKind"] = "Pointer";
                node["elementType"] = SerializeTypeUsage(ptr.ElementType);
                break;

            case IArrayType<IKnownOrNot, IFlatOrNot> arr:
                node["usageKind"] = "Array";
                node["rank"] = arr.Rank;
                node["elementType"] = SerializeTypeUsage(arr.ElementType);
                break;

            case IPlainType<IDeclaredType> plain:
                node["usageKind"] = "Plain";
                node["typeDefinition"] = plain.TypeDefinition.FullyQualifiedName;
                break;

            case IBoundGenericType<IKnownOrNot, IHasGenericParameters> bg:
                node["usageKind"] = "BoundGeneric";
                node["typeDefinition"] = bg.TypeDefinition.FullyQualifiedName;
#pragma warning disable CS0618
                node["typeArguments"] = bg.TypeArgumentsMixed.Select(SerializeTypeUsage).ToJsonArray();
#pragma warning restore CS0618
                break;

            case IPlaceholderType placeholder:
                node["usageKind"] = "Placeholder";
                node["placeholderName"] = placeholder.Info.Name;
                node["placeholderPosition"] = placeholder.Info.Position;
                node["placeholderSource"] = FormatGenericParameterSource(placeholder.Info.Source);
                break;

            case IManagedFunctionPointerType<IKnownOrNot> manFp:
                node["usageKind"] = "ManagedFunctionPointer";
                node["returnType"] = SerializeTypeUsage(manFp.ReturnType);
                node["parameters"] = SerializeParameters(manFp.Parameters);
                break;

            case IUnmanagedFunctionPointerType<IKnownOrNot> unmanFp:
                node["usageKind"] = "UnmanagedFunctionPointer";
                node["callingConventionFlags"] = unmanFp.CallingConventionFlags.ToString();
                node["callingConventions"] = unmanFp.CallingConventions.Select(c => (JsonNode)c.ToString()).ToJsonArray();
                node["returnType"] = SerializeTypeUsage(unmanFp.ReturnType);
                node["parameters"] = SerializeParameters(unmanFp.Parameters);
                break;

            case IRichTupleType<IKnownOrNot> richTuple:
                node["usageKind"] = "RichTuple";
                node["elements"] = richTuple.Elements.Select(elem => (JsonNode)new JsonObject
                {
                    ["name"] = elem.Name,
                    ["type"] = SerializeTypeUsage(elem.Type)
                }).ToJsonArray();
                break;

            case IExplicitValueTupleType<IKnownOrNot> explicitTuple:
                node["usageKind"] = "ExplicitValueTuple";
#pragma warning disable CS0618
                node["elementTypes"] = explicitTuple.ElementTypesMixed.Select(SerializeTypeUsage).ToJsonArray();
#pragma warning restore CS0618
                break;

            case IObliviousTupleType<IKnownOrNot> obliviousTuple:
                node["usageKind"] = "ObliviousTuple";
#pragma warning disable CS0618
                node["elementTypes"] = obliviousTuple.ElementTypesMixed.Select(SerializeTypeUsage).ToJsonArray();
#pragma warning restore CS0618
                break;

            default:
                throw new InvalidOperationException($"Unrecognized type usage kind: {usage.GetType().FullName}");
        }

        return node;
    }

    static JsonArray SerializeAttributes(IReadOnlyList<IAttribute> attributes)
        => attributes.Select(attr => (JsonNode)SerializeAttribute(attr)).ToJsonArray();

    static JsonObject SerializeAttribute(IAttribute attr)
    {
        IReadOnlyList<IParameter> ctorParams = attr.UsedConstructor.GetParameters();
        JsonObject ctorArgs = new();
        for (int i = 0; i < attr.ConstructorArguments.Count; i++)
        {
            string paramName = i < ctorParams.Count ? ctorParams[i].Name : $"{i}";
            ctorArgs[paramName] = FormatAttributeValue(attr.ConstructorArguments[i]);
        }

        return new JsonObject
        {
            ["underlyingType"] = SerializeTypeUsage(attr.UnderlyingType),
            ["constructorArguments"] = ctorArgs,
            ["namedArguments"] = new JsonObject(
                attr.NamedArguments.Select(kv => KeyValuePair.Create<string, JsonNode?>(kv.Key, FormatAttributeValue(kv.Value))))
        };
    }

    static string FormatAttributeValue(AttributeValue val) => val switch
    {
        AttributeValuePrimitive prim => prim.Value?.ToString() ?? "null",
        AttributeValueType typeVal => typeVal.TargetType?.LocalName ?? "null",
        AttributeValueEnum enumVal => FormatEnumAttributeValue(enumVal),
        AttributeValueArray arr => $"[{string.Join(", ", arr.Items.Select(FormatAttributeValue))}]",
        _ => throw new InvalidOperationException($"Unrecognized attribute value kind: {val.GetType().FullName}")
    };

    static string FormatEnumAttributeValue(AttributeValueEnum enumVal)
    {
        if (enumVal.MemberValue is null)
            return $"{enumVal.EnumType.LocalName}.null";

        IEnumMember? matchingMember = enumVal.EnumType.Members.FirstOrDefault(m => Equals(m.Value, enumVal.MemberValue));
        if (matchingMember is not null)
            return $"{enumVal.EnumType.LocalName}.{matchingMember.Name}";

        try
        {
            ulong numericVal = Convert.ToUInt64(enumVal.MemberValue);
            if (numericVal != 0)
            {
                List<string> flagNames = [];
                ulong accumulated = 0;
                foreach (IEnumMember m in enumVal.EnumType.Members)
                {
                    ulong memberVal = Convert.ToUInt64(m.Value);
                    if (memberVal != 0 && (numericVal & memberVal) == memberVal && (memberVal & (memberVal - 1)) == 0)
                    {
                        flagNames.Add($"{enumVal.EnumType.LocalName}.{m.Name}");
                        accumulated |= memberVal;
                    }
                }

                if (accumulated == numericVal && flagNames.Count > 0)
                    return string.Join(" | ", flagNames);
            }
        }
        catch
        {
            // Fallback for non-standard enum types
        }

        return $"{enumVal.EnumType.LocalName}.{enumVal.MemberValue}";
    }

    static JsonObject SerializeInheritance<T>(IInheritanceKind<T> inheritance) where T : class
        => new()
        {
            ["kind"] = inheritance switch
            {
                IInheritanceNone<T> => "None",
                IInheritanceAbstract<T> => "Abstract",
                IInheritanceVirtual<T> => "Virtual",
                IInheritanceSealedOverride<T> => "Sealed Override",
                IInheritanceOverride<T> => "Override",
                IInheritanceExplicitImplementation<T> => "ExplicitInterfaceImplementation",
                _ => throw new InvalidOperationException("Unreachable case.")
            }
        };

    extension(IEnumerable<JsonNode?> items)
    {
        public JsonArray ToJsonArray() => new([.. items]);
    }
}
