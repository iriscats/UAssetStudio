using KismetScript.Compiler.Compiler;
using KismetScript.Compiler.Compiler.Context;
using KismetScript.Compiler.Compiler.Intermediate;
using KismetScript.Utilities;
using System.Text.RegularExpressions;
using UAssetAPI;
using UAssetAPI.CustomVersions;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.UnrealTypes;

namespace KismetScript.Linker;

public abstract partial class PackageLinker<T> where T : UnrealPackage
{
    protected T Package { get; }

    protected bool SerializeLoadedProperties
        => Package.GetCustomVersion<FCoreObjectVersion>() >= FCoreObjectVersion.FProperties;

    private readonly record struct PreservedFieldPath(string[] PathSegments, FPackageIndex ResolvedOwner);

    protected enum PropertyPointerKind
    {
        Default,
        ArrayInner
    }

    // Dictionary to preserve original field path metadata from existing bytecode
    // Maps property name -> original path segments/resolved owner
    private Dictionary<(PropertyPointerKind Kind, string PropertyName), PreservedFieldPath> _originalPropertyFieldPaths = new();

    // Set of variable names whose property pointers had empty Path/owner in the original bytecode
    // (e.g. certain K2Node temporaries used as EX_Let Value pointers)
    private HashSet<string> _k2NodeVariablesWithEmptyPath = new();

    protected PackageLinker()
    {
        Package = CreateDefaultAsset();
    }

    public PackageLinker(T package)
    {
        Package = package;
    }

    protected FName AddName(string name)
    {
        var idSuffixMatch = NameIdSuffix().Match(name);
        if (idSuffixMatch?.Success ?? false)
        {
            var nameWithoutSuffix = name[..^idSuffixMatch.Length];
            if (Package.ContainsNameReference(new(nameWithoutSuffix)) &&
                int.TryParse(idSuffixMatch.Groups[1].Value, out var id))
            {
                return new FName(Package, nameWithoutSuffix, id + 1);
            }
        }

        return new FName(Package, name);
    }

    protected static Dictionary<string, string> _typeToPropertySerializedType = new()
    {
        { "byte", "ByteProperty" },
        { "bool", "BoolProperty" },
        { "int", "IntProperty" },
        { "string", "StrProperty" },
        { "float", "FloatProperty" },
        { "double", "DoubleProperty" },
        { "Interface", "InterfaceProperty" },
        { "Struct", "StructProperty" },
        { "Array", "ArrayProperty" },
        { "Enum", "IntProperty" },
        { "Object", "ObjectProperty" },
        { "Delegate", "DelegateProperty" },
        { "Class", "ClassProperty" }
    };

    protected static string GetPropertySerializedType(VariableSymbol symbol)
    {
        var type = symbol.Declaration?.Type.Text ?? "";
        if (_typeToPropertySerializedType.Values.Contains(type))
            return type;
        if (!_typeToPropertySerializedType.TryGetValue(type, out var serializedType))
            throw new NotImplementedException($"Creating new property of type {type} is not implemented");
        return serializedType;
    }

    protected static EPropertyFlags GetPropertyFlags(VariableSymbol symbol)
    {
        EPropertyFlags flags = 0;
        if (symbol.IsParameter)
        {
            flags = EPropertyFlags.CPF_BlueprintVisible | EPropertyFlags.CPF_BlueprintReadOnly | EPropertyFlags.CPF_Parm;
            if (symbol.IsOutParameter)
            {
                flags |= EPropertyFlags.CPF_OutParm;
            }
            if (symbol.IsReturnParameter)
            {
                flags |= EPropertyFlags.CPF_OutParm;
                flags |= EPropertyFlags.CPF_ReturnParm;
            }
        }
        return flags;
    }

    protected UProperty CreateUProperty(VariableSymbol symbol)
    {
        var serializedType = GetPropertySerializedType(symbol);
        var propertyFlags = GetPropertyFlags(symbol);

        return serializedType switch
        {
            "ByteProperty" => new UByteProperty()
            {
                Enum = new FPackageIndex(0),
                ArrayDim = EArrayDim.TArray,
                ElementSize = 0,
                PropertyFlags = propertyFlags,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
                Next = null,
            },
            "BoolProperty" => new UBoolProperty()
            {
                NativeBool = true,
                ArrayDim = EArrayDim.TArray,
                ElementSize = 1,
                PropertyFlags = propertyFlags,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
                Next = null,
            },
            "IntProperty" => new UIntProperty()
            {
                ArrayDim = EArrayDim.TArray,
                ElementSize = 0,
                PropertyFlags = propertyFlags,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
                Next = null,
            },
            "StrProperty" => new UStrProperty()
            {
                ArrayDim = EArrayDim.TArray,
                ElementSize = 0,
                PropertyFlags = propertyFlags,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
                Next = null,
            },
            "FloatProperty" => new UFloatProperty()
            {
                ArrayDim = EArrayDim.TArray,
                ElementSize = 0,
                PropertyFlags = propertyFlags,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
                Next = null,
            },
            "DoubleProperty" => new UDoubleProperty()
            {
                ArrayDim = EArrayDim.TArray,
                ElementSize = 0,
                PropertyFlags = propertyFlags,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
                Next = null,
            },
            "InterfaceProperty" => new UInterfaceProperty()
            {
                InterfaceClass = FindPackageIndexInAsset(symbol.InnerSymbol!),
                ArrayDim = EArrayDim.TArray,
                ElementSize = 0,
                PropertyFlags = propertyFlags,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
                Next = null
            },
            "StructProperty" => new UStructProperty()
            {
                Struct = FindPackageIndexInAsset(symbol.InnerSymbol!),
                ArrayDim = EArrayDim.TArray,
                ElementSize = 0,
                PropertyFlags = propertyFlags,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
                Next = null
            },
            "ArrayProperty" => new UArrayProperty()
            {
                Inner = FindPackageIndexInAsset(symbol.InnerSymbol!),
                ArrayDim = EArrayDim.TArray,
                ElementSize = 0,
                PropertyFlags = propertyFlags,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
                Next = null,
            },
            "ObjectProperty" => new UObjectProperty()
            {
                PropertyClass = FindPackageIndexInAsset(symbol.InnerSymbol!),
                ArrayDim = EArrayDim.TArray,
                ElementSize = 0,
                PropertyFlags = propertyFlags,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
                Next = null,
            },
            "DelegateProperty" => new UDelegateProperty()
            {
                SignatureFunction = FindPackageIndexInAsset(symbol.InnerSymbol!),
                ArrayDim = EArrayDim.TArray,
                ElementSize = 0,
                PropertyFlags = propertyFlags,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
                Next = null,
            },
            "ClassProperty" => new UClassProperty()
            {
                MetaClass = FindPackageIndexInAsset(symbol.InnerSymbol!),
                //PropertyClass = FindPack
                ArrayDim = EArrayDim.TArray,
                ElementSize = 0,
                PropertyFlags = propertyFlags,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
                Next = null,
            },
            _ => throw new NotImplementedException(serializedType),
        };
    }

    protected FProperty CreateFProperty(VariableSymbol symbol)
    {
        var serializedType = GetPropertySerializedType(symbol);
        var propertyFlags = GetPropertyFlags(symbol);
        return serializedType switch
        {
            "ByteProperty" => new FByteProperty()
            {
                // FField values
                SerializedType = AddName(serializedType),
                Name = AddName(symbol.Name),
                Flags = EObjectFlags.RF_Public,

                // FProperty values
                ArrayDim = EArrayDim.TArray,
                ElementSize = 1,
                PropertyFlags = propertyFlags,
                RepIndex = 0,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,

                Enum = new FPackageIndex(0),
            },
            "BoolProperty" => new FBoolProperty()
            {
                // FField values
                SerializedType = AddName(serializedType),
                Name = AddName(symbol.Name),
                Flags = EObjectFlags.RF_Public,

                // FProperty values
                ArrayDim = EArrayDim.TArray,
                ElementSize = 1,
                PropertyFlags = propertyFlags,
                RepIndex = 0,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,

                FieldSize = 1,
                ByteOffset = 0,
                ByteMask = 0x1,
                FieldMask = 0xFF,
                NativeBool = true,
                Value = true, // TODO: is this correct?
            },
            "IntProperty" => new FGenericProperty()
            {
                // FField values
                SerializedType = AddName(serializedType),
                Name = AddName(symbol.Name),
                Flags = EObjectFlags.RF_Public,

                // FProperty values
                ArrayDim = EArrayDim.TArray,
                ElementSize = 4,
                PropertyFlags = propertyFlags,
                RepIndex = 0,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
            },
            "StrProperty" => new FGenericProperty()
            {
                // FField values
                SerializedType = AddName(serializedType),
                Name = AddName(symbol.Name),
                Flags = EObjectFlags.RF_Public,

                // FProperty values
                ArrayDim = EArrayDim.TArray,
                ElementSize = 8,
                PropertyFlags = propertyFlags,
                RepIndex = 0,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
            },
            "FloatProperty" => new FGenericProperty()
            {
                // FField values
                SerializedType = AddName(serializedType),
                Name = AddName(symbol.Name),
                Flags = EObjectFlags.RF_Public,

                // FProperty values
                ArrayDim = EArrayDim.TArray,
                ElementSize = 4,
                PropertyFlags = propertyFlags,
                RepIndex = 0,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
            },
            "DoubleProperty" => new FGenericProperty()
            {
                // FField values
                SerializedType = AddName(serializedType),
                Name = AddName(symbol.Name),
                Flags = EObjectFlags.RF_Public,

                // FProperty values
                ArrayDim = EArrayDim.TArray,
                ElementSize = 8,
                PropertyFlags = propertyFlags,
                RepIndex = 0,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,
            },
            "InterfaceProperty" => new FInterfaceProperty()
            {
                // FField values
                SerializedType = AddName(serializedType),
                Name = AddName(symbol.Name),
                Flags = EObjectFlags.RF_Public,

                // FProperty values
                ArrayDim = EArrayDim.TArray,
                ElementSize = 8,
                PropertyFlags = propertyFlags,
                RepIndex = 0,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,

                InterfaceClass = FindPackageIndexInAsset(symbol.InnerSymbol!),
            },
            "StructProperty" => new FStructProperty()
            {
                // FField values
                SerializedType = AddName(serializedType),
                Name = AddName(symbol.Name),
                Flags = EObjectFlags.RF_Public,

                // FProperty values
                ArrayDim = EArrayDim.TArray,
                ElementSize = 0, // TODO: depends on the actual size of the struct
                PropertyFlags = propertyFlags,
                RepIndex = 0,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,

                Struct = FindPackageIndexInAsset(symbol.InnerSymbol!),
            },
            "ArrayProperty" => new FArrayProperty()
            {
                // FField values
                SerializedType = AddName(serializedType),
                Name = AddName(symbol.Name),
                Flags = EObjectFlags.RF_Public,

                // FProperty values
                ArrayDim = EArrayDim.TArray,
                ElementSize = 16,
                PropertyFlags = propertyFlags,
                RepIndex = 0,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,

                // TODO verify if this works
                Inner = CreateFProperty((VariableSymbol)symbol.InnerSymbol!),
            },
            "DelegateProperty" => new FDelegateProperty()
            {
                // FField values
                SerializedType = AddName(serializedType),
                Name = AddName(symbol.Name),
                Flags = EObjectFlags.RF_Public,

                // FProperty values
                ArrayDim = EArrayDim.TArray,
                ElementSize = 20,
                PropertyFlags = propertyFlags,
                RepIndex = 0,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,

                // TODO verify if this works
                SignatureFunction = FindPackageIndexInAsset(symbol.InnerSymbol!),
            },
            "ObjectProperty" => new FObjectProperty()
            {
                // FField values
                SerializedType = AddName(serializedType),
                Name = AddName(symbol.Name),
                Flags = EObjectFlags.RF_Public,

                // FProperty values
                ArrayDim = EArrayDim.TArray,
                ElementSize = 8,
                PropertyFlags = propertyFlags,
                RepIndex = 0,
                RepNotifyFunc = AddName("None"),
                BlueprintReplicationCondition = UAssetAPI.FieldTypes.ELifetimeCondition.COND_None,
                RawValue = null,

                PropertyClass = FindPackageIndexInAsset(symbol.InnerSymbol!),
            },
            _ => throw new NotImplementedException(serializedType),
        };
    }

    protected (FProperty Property, FPackageIndex ExportIndex, PropertyExport? Export) CreatePropertyAsFProperty(VariableSymbol symbol)
    {
        var property = CreateFProperty(symbol);
        // TODO: create _GEN_VARIABLE if necessary
        var exportIndex = new FPackageIndex(0);
        PropertyExport? export = null;
        return (property, exportIndex, export);
    }

    protected (FPackageIndex Index, PropertyExport Export) CreatePropertyAsPropertyExport(VariableSymbol symbol)
    {
        var serializedType = GetPropertySerializedType(symbol);
        var property = CreateUProperty(symbol);

        var serializationBeforeSerializationDependencies = new List<FPackageIndex>();
        var createBeforeSerializationDependencies = new List<FPackageIndex>();
        var createBeforeCreateDependencies = new List<FPackageIndex>();

        var className = symbol.DeclaringClass?.Name;
        var classExport = className != null ? Package.FindClassExportByName(className) : null;
        var functionName = symbol?.DeclaringProcedure?.Name;
        var functionExport = functionName != null ? Package.FindFunctionExportByName(functionName) : null;
        var coreUObjectImport = Package.FindImportIndexByObjectName("/Script/CoreUObject") ?? throw new NotImplementedException();
        var propertyClassImportIndex = EnsureObjectImported(coreUObjectImport, serializedType, "Class");
        var propertyTemplateImportIndex = EnsureObjectImported(coreUObjectImport, $"Default__{serializedType}", serializedType);

        var functionOwnerIndex = functionExport != null ? FPackageIndex.FromExport(Package.Exports.IndexOf(functionExport)) : new FPackageIndex(0);
        var classOwnerIndex = classExport != null ? FPackageIndex.FromExport(Package.Exports.IndexOf(classExport)) : new FPackageIndex(0);
        var propertyOwnerIndex =
            functionExport != null ?
                functionOwnerIndex :
            classExport != null ?
                classOwnerIndex :
                new FPackageIndex(0);

        if (!propertyOwnerIndex.IsNull())
        {
            createBeforeCreateDependencies.Insert(0, propertyOwnerIndex);
        }

        var propertyExport = new PropertyExport()
        {
            Asset = Package,
            Property = property,
            Data = new(),
            ObjectName = AddName(symbol?.Name ?? string.Empty),
            ObjectFlags = EObjectFlags.RF_Public,
            SerialSize = 0, // Filled by serializer
            SerialOffset = 0, // Filled by serializer
            bForcedExport = false,
            bNotForClient = false,
            bNotForServer = false,
            PackageGuid = Guid.Empty,
            IsInheritedInstance = false,
            PackageFlags = EPackageFlags.PKG_None,
            bNotAlwaysLoadedForEditorGame = false,
            bIsAsset = false,
            GeneratePublicHash = false,
            SerializationBeforeSerializationDependencies = new(serializationBeforeSerializationDependencies),
            CreateBeforeSerializationDependencies = new(createBeforeSerializationDependencies),
            SerializationBeforeCreateDependencies = new(),
            CreateBeforeCreateDependencies = new(createBeforeCreateDependencies),
            Extras = new byte[0],
            OuterIndex = propertyOwnerIndex,
            ClassIndex = propertyClassImportIndex,
            SuperIndex = new FPackageIndex(0),
            TemplateIndex = propertyTemplateImportIndex,
        };

        Package.Exports.Add(propertyExport);
        var packageIndex = FPackageIndex.FromExport(Package.Exports.Count - 1);
        return (packageIndex, propertyExport);
    }

    protected FPackageIndex EnsurePackageIndexForSymbolCreated(Symbol? symbol)
    {
        if (symbol == null)
            return new FPackageIndex(0);

        if (TryFindPackageIndexInAsset(symbol, out var packageIndex))
            return packageIndex ?? new FPackageIndex(0);

        return CreatePackageIndexForSymbol(symbol);
    }

    protected FPackageIndex CreatePackageIndexForSymbol(Symbol symbol)
    {
        // FIXME: remove this hack
        if (symbol.Name.StartsWith("AnonymousClass_"))
            return new FPackageIndex(0);

        if (symbol is VariableSymbol variableSymbol)
        {
            return CreatePropertyAsPropertyExport(variableSymbol).Index;
        }
        else if (symbol is ProcedureSymbol procedureSymbol)
        {
            if (procedureSymbol.IsExternal)
            {
                return CreateProcedureImport(procedureSymbol);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else if (symbol is UnknownSymbol)
        {
            // For UnknownSymbol, we cannot resolve the actual package index
            // This will be handled specially in FixPropertyPointer to preserve the name
            return new FPackageIndex(0);
        }
        else
        {
            Console.WriteLine($"CreatePackageIndexForSymbol: {symbol.Name}");
            throw new NotImplementedException();
        }
    }

    protected void FixPropertyPointer(ref KismetPropertyPointer pointer, PropertyPointerKind kind = PropertyPointerKind.Default)
    {
        if (pointer is IntermediatePropertyPointer iProperty)
        {
            if (SerializeLoadedProperties)
            {
                var propertyName = iProperty.Symbol.Name;

                // Reuse preserved path metadata when available to match the original asset exactly.
                if (_originalPropertyFieldPaths.TryGetValue((kind, propertyName), out var preservedFieldPath))
                {
                    pointer = new KismetPropertyPointer()
                    {
                        Old = new FPackageIndex(0),
                        New = RehydrateFieldPath(preservedFieldPath),
                    };
                    return;
                }

                FPackageIndex resolvedOwner;
                if (iProperty.Symbol is UnknownSymbol)
                {
                    resolvedOwner = TryFindPropertyOwner(propertyName);

                    if (resolvedOwner.IsNull() && iProperty.OriginalResolvedOwner != null)
                    {
                        resolvedOwner = iProperty.OriginalResolvedOwner;
                    }

                    if (resolvedOwner.IsNull())
                    {
                        resolvedOwner = new FPackageIndex(-1);
                    }
                }
                else
                {
                    resolvedOwner = EnsurePackageIndexForSymbolCreated(iProperty.Symbol.DeclaringSymbol);
                }

                pointer = new KismetPropertyPointer()
                {
                    Old = new FPackageIndex(0),
                    New = new()
                    {
                        Path = new[] { AddName(propertyName) },
                        ResolvedOwner = resolvedOwner,
                    }
                };
            }
            else
            {
                var packageIndex = EnsurePackageIndexForSymbolCreated(iProperty.Symbol);
                pointer = new KismetPropertyPointer()
                {
                    Old = packageIndex,
                    New = new FFieldPath()
                };
            }
        }
    }

    private FFieldPath RehydrateFieldPath(PreservedFieldPath preservedFieldPath)
    {
        return new FFieldPath()
        {
            Path = preservedFieldPath.PathSegments.Select(AddName).ToArray(),
            ResolvedOwner = preservedFieldPath.ResolvedOwner,
        };
    }

    private void FixLetValuePropertyPointer(ref KismetPropertyPointer pointer)
    {
        if (pointer is IntermediatePropertyPointer iProperty &&
            SerializeLoadedProperties &&
            _k2NodeVariablesWithEmptyPath.Contains(iProperty.Symbol.Name))
        {
            pointer = new KismetPropertyPointer()
            {
                Old = new FPackageIndex(0),
                New = new()
                {
                    Path = Array.Empty<FName>(),
                    ResolvedOwner = new FPackageIndex(0),
                }
            };
            return;
        }

        FixPropertyPointer(ref pointer);
    }

    /// <summary>
    /// Try to find the package index of the owner class/struct that contains a property with the given name.
    /// This is used when the property symbol cannot be fully resolved during compilation.
    /// </summary>
    protected virtual FPackageIndex TryFindPropertyOwner(string propertyName)
    {
        // First, search through exports for structs/classes with LoadedProperties
        foreach (var export in Package.Exports)
        {
            if (export is StructExport structExport)
            {
                // Check if this struct has a property with the given name
                foreach (var fprop in structExport.LoadedProperties ?? Array.Empty<FProperty>())
                {
                    if (fprop.Name.ToString() == propertyName)
                    {
                        // Found the property in this struct/class, return its export index
                        return FPackageIndex.FromExport(Package.Exports.IndexOf(export));
                    }
                }

                // Also check UProperty children for older UE versions
                foreach (var child in structExport.Children ?? Array.Empty<FPackageIndex>())
                {
                    if (child.IsExport())
                    {
                        var childExport = child.ToExport(Package);
                        if (childExport is PropertyExport propExport &&
                            propExport.ObjectName.ToString() == propertyName)
                        {
                            return FPackageIndex.FromExport(Package.Exports.IndexOf(export));
                        }
                    }
                }
            }
        }

        // Next, search through imports for classes/structs that might have this property as a child
        for (int i = 0; i < Package.Imports.Count; i++)
        {
            var import = Package.Imports[i];
            var importIndex = FPackageIndex.FromImport(i);

            // Check if this is a class/struct type import
            if (import.ClassName.ToString() == "Class" || import.ClassName.ToString() == "WidgetBlueprintGeneratedClass" ||
                import.ClassName.ToString() == "BlueprintGeneratedClass" || import.ClassName.ToString() == "ScriptStruct")
            {
                // Try to find a property with this name as a child import
                for (int j = 0; j < Package.Imports.Count; j++)
                {
                    var candidateImport = Package.Imports[j];
                    if (candidateImport.OuterIndex == importIndex &&
                        candidateImport.ObjectName.ToString() == propertyName)
                    {
                        // Found a property import with this name that belongs to this class
                        return importIndex;
                    }
                }
            }
        }

        // Not found
        return new FPackageIndex(0);
    }

    protected void FixPackageIndex(ref FPackageIndex packageIndex)
    {
        if (packageIndex is IntermediatePackageIndex iPackageIndex)
        {
            packageIndex = EnsurePackageIndexForSymbolCreated(iPackageIndex.Symbol);
        }
    }

    protected FPackageIndex FixPackageIndex(FPackageIndex packageIndex)
    {
        FixPackageIndex(ref packageIndex);
        return packageIndex ?? new FPackageIndex(0);
    }

    protected void FixName(ref FName name)
    {
        if (name is IntermediateName iName)
        {
            name = AddName(iName.TextValue);
        }
    }

    protected FName FixName(FName name)
    {
        FixName(ref name);
        return name;
    }

    protected string? GetFullName(object? obj)
    {
        if (obj is Import import)
        {
            if (import.OuterIndex.Index != 0)
            {
                var parent = GetFullName(import.OuterIndex) ?? string.Empty;
                return parent + "." + import.ObjectName.ToString();
            }
            else
            {
                return import.ObjectName.ToString();
            }
        }
        else if (obj is Export export)
        {
            if (export.OuterIndex.Index != 0)
            {
                var parent = GetFullName(export.OuterIndex) ?? string.Empty;
                return parent + "." + export.ObjectName.ToString();
            }
            else
            {
                return export.ObjectName.ToString();
            }
        }
        else if (obj is FField field)
            return field.Name.ToString();
        else if (obj is FName fname)
            return fname.ToString();
        else if (obj is FPackageIndex packageIndex)
        {
            if (packageIndex.IsImport())
                return GetFullName(packageIndex.ToImport(Package));
            else if (packageIndex.IsExport())
                return GetFullName(packageIndex.ToExport(Package));
            else if (packageIndex.IsNull())
                return null;
            else
                throw new NotImplementedException();
        }
        else
        {
            return null;
        }
    }

    protected FPackageIndex FindPackageIndexInAsset(Symbol? symbol)
    {
        if (symbol == null)
            return new FPackageIndex(0);

        if (!TryFindPackageIndexInAsset(symbol, out var packageIndex))
        {
            var pkgName = symbol.DeclaringPackage?.Name;
            if (string.IsNullOrWhiteSpace(pkgName))
            {
                packageIndex = new FPackageIndex(0);
            }
            else
            {
                packageIndex = EnsurePackageImported(pkgName);
            }
            var objName = symbol.Name;
            if (!string.IsNullOrWhiteSpace(objName))
            {
                packageIndex = EnsureObjectImported(packageIndex, objName, "Class"); // TODO classname
            }
        }

        return packageIndex ?? new FPackageIndex(0);
    }

    protected bool TryFindPackageIndexInAsset(Symbol? symbol, out FPackageIndex? index)
    {
        index = null;

        if (symbol == null)
            return false;

        var packageName = symbol.DeclaringPackage?.Name;
        var className = symbol.DeclaringClass?.Name;
        var functionName = symbol.DeclaringProcedure?.Name;
        var name = symbol.Name;

        // TODO fix
        if (name == "<null>")
        {
            index = new FPackageIndex(0);
            return true;
        }

        var packageClassFunctionLocalName = string.Join(".", new[] { packageName, className, functionName, name }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var classFunctionLocalName = string.Join(".", new[] { className, functionName, name }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var classLocalName = string.Join(".", new[] { className, name }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var localName = name;

        var packageClassFunctionLocalCandidates = GetPackageIndexByFullName(packageClassFunctionLocalName).ToList();
        if (packageClassFunctionLocalCandidates.Count == 1)
        {
            index = packageClassFunctionLocalCandidates[0].PackageIndex;
            return true;
        }

        var classFunctionLocalCandidates = GetPackageIndexByFullName(classFunctionLocalName).ToList();
        if (classFunctionLocalCandidates.Count == 1)
        {
            index = classFunctionLocalCandidates[0].PackageIndex;
            return true;
        }

        var classLocalCandidates = GetPackageIndexByFullName(classLocalName).ToList();
        if (classLocalCandidates.Count == 1)
        {
            index = classLocalCandidates[0].PackageIndex;
            return true;
        }

        var localCandidates = GetPackageIndexByLocalName(localName).ToList();
        if (localCandidates.Count == 1)
        {
            index = localCandidates[0].PackageIndex;
            return true;
        }

        return false;
    }

    protected KismetExpression[] GetFixedBytecode(IEnumerable<KismetExpression> expressions)
    {
        var bytecode = expressions.ToArray();
        foreach (var baseExpr in bytecode.Flatten())
        {
            switch (baseExpr)
            {
                case EX_BindDelegate expr:
                    FixName(ref expr.FunctionName);
                    break;

                case EX_InstanceDelegate expr:
                    FixName(ref expr.FunctionName);
                    break;

                case EX_InstrumentationEvent expr:
                    FixName(ref expr.EventName);
                    break;

                case EX_NameConst expr:
                    expr.Value = FixName(expr.Value);
                    break;

                case EX_VirtualFunction expr:
                    FixName(ref expr.VirtualFunctionName);
                    break;

                case EX_CrossInterfaceCast expr:
                    FixPackageIndex(ref expr.ClassPtr);
                    break;

                case EX_DynamicCast expr:
                    FixPackageIndex(ref expr.ClassPtr);
                    break;

                case EX_FinalFunction expr:
                    FixPackageIndex(ref expr.StackNode);
                    break;

                case EX_InterfaceToObjCast expr:
                    FixPackageIndex(ref expr.ClassPtr);
                    break;

                case EX_MetaCast expr:
                    FixPackageIndex(ref expr.ClassPtr);
                    break;

                case EX_ObjectConst expr:
                    expr.Value = FixPackageIndex(expr.Value);
                    break;

                case EX_ObjToInterfaceCast expr:
                    FixPackageIndex(ref expr.ClassPtr);
                    break;

                case EX_SetArray expr:
                    if (Package.ObjectVersion < ObjectVersion.VER_UE4_CHANGE_SETARRAY_BYTECODE)
                        FixPackageIndex(ref expr.ArrayInnerProp);
                    break;

                case EX_StructConst expr:
                    FixPackageIndex(ref expr.Struct);
                    break;

                case EX_TextConst expr:
                    if (expr.Value?.StringTableAsset != null)
                    {
                        expr.Value.StringTableAsset = FixPackageIndex(expr.Value.StringTableAsset);
                    }
                    break;

                case EX_ArrayConst expr:
                    FixPropertyPointer(ref expr.InnerProperty, PropertyPointerKind.ArrayInner);
                    break;

                case EX_ClassSparseDataVariable expr:
                    FixPropertyPointer(ref expr.Variable);
                    break;

                case EX_Context expr:
                    FixPropertyPointer(ref expr.RValuePointer);
                    break;

                case EX_DefaultVariable expr:
                    FixPropertyPointer(ref expr.Variable);
                    break;

                case EX_InstanceVariable expr:
                    FixPropertyPointer(ref expr.Variable);
                    break;

                case EX_Let expr:
                    FixLetValuePropertyPointer(ref expr.Value);
                    break;

                case EX_LetValueOnPersistentFrame expr:
                    FixPropertyPointer(ref expr.DestinationProperty);
                    break;

                case EX_LocalOutVariable expr:
                    FixPropertyPointer(ref expr.Variable);
                    break;

                case EX_LocalVariable expr:
                    FixPropertyPointer(ref expr.Variable);
                    break;

                case EX_MapConst expr:
                    FixPropertyPointer(ref expr.KeyProperty);
                    FixPropertyPointer(ref expr.ValueProperty);
                    break;

                case EX_PropertyConst expr:
                    FixPropertyPointer(ref expr.Property);
                    break;

                case EX_SetConst expr:
                    FixPropertyPointer(ref expr.InnerProperty);
                    break;

                case EX_StructMemberContext expr:
                    FixPropertyPointer(ref expr.StructMemberExpression);
                    break;

                default:
                    break;
            }
        }
        return bytecode;
    }

    protected FunctionExport CreateFunctionExport(CompiledFunctionContext context)
    {
        var coreUObjectImport = EnsurePackageImported("/Script/CoreUObject");
        var functionClassImport = EnsureObjectImported(coreUObjectImport, "Function", "Class");
        var functionDefaultObjectImport = EnsureObjectImported(coreUObjectImport, "Default__Function", "Function");
        var className2 = context.Symbol?.DeclaringClass?.Name;
        var classExport = className2 != null ? Package.FindClassExportByName(className2) : null;

        var ownerIndex = classExport != null ?
            FPackageIndex.FromExport(Package.Exports.IndexOf(classExport)) :
            new FPackageIndex(0);

        var createBeforeCreateDependencies = new List<FPackageIndex>();
        if (!ownerIndex.IsNull())
            createBeforeCreateDependencies.Add(ownerIndex);

        // TODO: override
        var baseFunctionClassIndex = new FPackageIndex(0);
        var baseFunctionIndex = new FPackageIndex(0);
        var ubergraphFunction = Package.GetUbergraphFunction();
        var ubergraphFunctionIndex = ubergraphFunction != null ?
            FPackageIndex.FromExport(Package.Exports.IndexOf(ubergraphFunction)) :
            new FPackageIndex(0);

        var export = new FunctionExport()
        {
            FunctionFlags = context.Symbol?.Flags ?? default,
            SuperStruct = baseFunctionIndex,
            Children = Array.Empty<FPackageIndex>(),
            LoadedProperties = Array.Empty<FProperty>(),
            ScriptBytecode = null,
            ScriptBytecodeSize = 0,
            ScriptBytecodeRaw = null,
            Field = new() { Next = null },
            Data = new(),
            ObjectName = new(Package, context.Symbol?.Name ?? string.Empty),
            ObjectFlags = EObjectFlags.RF_Public,
            SerialSize = 0xDEADBEEF,
            SerialOffset = 0xDEADBEEF,
            bForcedExport = false,
            bNotForClient = false,
            bNotForServer = false,
            PackageGuid = Guid.Empty,
            IsInheritedInstance = false,
            PackageFlags = EPackageFlags.PKG_None,
            bNotAlwaysLoadedForEditorGame = false,
            bIsAsset = false,
            GeneratePublicHash = false,
            SerializationBeforeSerializationDependencies = new(),
            CreateBeforeSerializationDependencies = new() { /*ubergraphFunctionIndex*/ },
            SerializationBeforeCreateDependencies = new(),
            CreateBeforeCreateDependencies = createBeforeCreateDependencies,
            Extras = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            OuterIndex = ownerIndex,
            ClassIndex = functionClassImport,
            SuperIndex = baseFunctionIndex,
            TemplateIndex = functionDefaultObjectImport,
        };
        Package.Exports.Add(export);

        if (Package.GetCustomVersion<FCoreObjectVersion>() >= FCoreObjectVersion.FProperties)
        {
            var properties = context.Variables
                .Select(x => CreatePropertyAsFProperty(x.Symbol))
                .ToList();
            var propertyData = properties
                .Select(x => x.Property)
                .ToArray();
            var exportProperties = properties
                .Where(x => !x.ExportIndex.IsNull())
                .Select(x => x.ExportIndex)
                .ToArray();

            export.LoadedProperties = (export.LoadedProperties ?? Array.Empty<FProperty>())
                .Concat(propertyData)
                .ToArray();
            export.Children = (export.Children ?? Array.Empty<FPackageIndex>())
                .Concat(exportProperties)
                .ToArray();
            export.SerializationBeforeSerializationDependencies.AddRange(exportProperties);
        }
        else
        {
            var children = context.Variables
                .Select(x => CreatePackageIndexForSymbol(x.Symbol))
                .ToArray();
            export.Children = (export.Children ?? Array.Empty<FPackageIndex>())
                .Concat(children)
                .ToArray();
            export.SerializationBeforeSerializationDependencies.AddRange(children);
        }

        if (!baseFunctionClassIndex.IsNull())
        {
            export.CreateBeforeSerializationDependencies.Insert(0, baseFunctionClassIndex);
        }
        if (!baseFunctionIndex.IsNull())
        {
            export.SerializationBeforeSerializationDependencies.Insert(0, baseFunctionIndex);
            export.CreateBeforeCreateDependencies.Add(baseFunctionIndex);
        }

        var exportIndex = FPackageIndex.FromExport(Package.Exports.IndexOf(export));
        if (classExport != null)
        {
            classExport.FuncMap[export.ObjectName] = exportIndex;
            classExport.Children = (classExport.Children ?? Array.Empty<FPackageIndex>())
                .Concat(new[] { exportIndex })
                .ToArray();
            classExport.CreateBeforeSerializationDependencies.Add(exportIndex);
        }

        return export;
    }

    protected ClassExport CreateClassExport(CompiledClassContext classContext)
    {
        var scriptEnginePackageIndex = EnsurePackageImported("/Script/Engine");
        var blueprintGeneratedClassObjectIndex = EnsureObjectImported(scriptEnginePackageIndex, "BlueprintGeneratedClass", "Class");
        var blueprintGeneratedClassDefaultObjectIndex = EnsureObjectImported(blueprintGeneratedClassObjectIndex, "Default__BlueprintGeneratedClass", "BlueprintGeneratedClass");

        var scriptCoreUObjectPackageIndex = EnsurePackageImported("/Script/CoreUObject");
        var objectObjectIndex = EnsureObjectImported(scriptCoreUObjectPackageIndex, "Object", "Class");
        var objectDefaultObjectIndex = EnsureObjectImported(objectObjectIndex, "Default__Object", "Object");

        var classDefaultObjectIndex = new FPackageIndex(0);
        var baseClassObjectIndex = objectObjectIndex;
        var baseClassDefaultObjectIndex = objectDefaultObjectIndex;

        var serializationBeforeSerializationDependencies = new List<FPackageIndex>();
        if (!baseClassObjectIndex.IsNull())
        {
            serializationBeforeSerializationDependencies.Add(baseClassObjectIndex);
            serializationBeforeSerializationDependencies.Add(baseClassDefaultObjectIndex);
        }

        var createBeforeCreateDependencies = new List<FPackageIndex>();
        if (!baseClassObjectIndex.IsNull())
            createBeforeCreateDependencies.Add(baseClassObjectIndex);

        var classExport = new ClassExport()
        {
            FuncMap = new(),
            ClassFlags = EClassFlags.CLASS_Parsed | EClassFlags.CLASS_ReplicationDataIsSetUp | EClassFlags.CLASS_CompiledFromBlueprint | EClassFlags.CLASS_HasInstancedReference,
            ClassWithin = objectObjectIndex, // -11
            ClassConfigName = AddName("Engine"),
            Interfaces = Array.Empty<SerializedInterfaceReference>(),
            ClassGeneratedBy = new FPackageIndex(0),
            bDeprecatedForceScriptOrder = false,
            bCooked = true,
            ClassDefaultObject = classDefaultObjectIndex,
            SuperStruct = baseClassObjectIndex,
            Children = Array.Empty<FPackageIndex>(),
            LoadedProperties = Array.Empty<FProperty>(),
            ScriptBytecode = Array.Empty<KismetExpression>(),
            ScriptBytecodeSize = 0,
            ScriptBytecodeRaw = null,
            Field = new() { Next = null },
            /*
             * TODO
             *       "Data": [
                    {
                      "$type": "UAssetAPI.PropertyTypes.Objects.ObjectPropertyData, UAssetAPI",
                      "Name": "SimpleConstructionScript",
                      "DuplicationIndex": 0,
                      "Value": 382
                    },
                    {
                      "$type": "UAssetAPI.PropertyTypes.Objects.ObjectPropertyData, UAssetAPI",
                      "Name": "InheritableComponentHandler",
                      "DuplicationIndex": 0,
                      "Value": 184
                    },
                    {
                      "$type": "UAssetAPI.PropertyTypes.Objects.ObjectPropertyData, UAssetAPI",
                      "Name": "UberGraphFramePointerProperty",
                      "DuplicationIndex": 0,
                      "Value": 1
                    },
                    {
                      "$type": "UAssetAPI.PropertyTypes.Objects.ObjectPropertyData, UAssetAPI",
                      "Name": "UberGraphFunction",
                      "DuplicationIndex": 0,
                      "Value": 6
                    }
                  ],
             */
            Data = new(),
            ObjectName = AddName(classContext.Symbol.Name),
            ObjectFlags = EObjectFlags.RF_Public | EObjectFlags.RF_Transactional,
            SerialSize = 0xDEADBEEF,
            SerialOffset = 0xDEADBEEF,
            bForcedExport = false,
            bNotForClient = false,
            bNotForServer = false,
            PackageGuid = Guid.Empty,
            IsInheritedInstance = false,
            PackageFlags = EPackageFlags.PKG_None,
            bNotAlwaysLoadedForEditorGame = false,
            bIsAsset = false,
            GeneratePublicHash = false,
            /* 
             * TODO
             * - Base class
             * - Base class default object
             * - Class properties
             */
            SerializationBeforeSerializationDependencies = serializationBeforeSerializationDependencies,
            /*
             * TODO
             * - Function exports
             */
            CreateBeforeSerializationDependencies = new(),
            SerializationBeforeCreateDependencies = new() { blueprintGeneratedClassObjectIndex, blueprintGeneratedClassDefaultObjectIndex },
            CreateBeforeCreateDependencies = createBeforeCreateDependencies,
            Extras = Array.Empty<byte>(),
            OuterIndex = new FPackageIndex(0),
            ClassIndex = blueprintGeneratedClassObjectIndex, // -13
            SuperIndex = new FPackageIndex(0), // -2
            TemplateIndex = blueprintGeneratedClassDefaultObjectIndex,
        };
        Package.Exports.Add(classExport);
        return classExport;
    }

    protected TExport? FindChildExport<TExport>(StructExport? parent, string name) where TExport : Export
    {
        var selection = parent?.Children
            .Where(x => x.IsExport())
            .Select(x => x.ToExport(Package)) ??
            Package.Exports;
        return selection
            .Where(x => x is TExport && x.ObjectName.ToString() == name)
            .Cast<TExport>()
            .SingleOrDefault();
    }

    protected virtual void LinkCompiledFunction(CompiledFunctionContext functionContext)
    {
        var classExport = functionContext.Symbol.DeclaringClass != null ?
            FindChildExport<ClassExport>(null, functionContext.Symbol!.DeclaringClass!.Name) :
            null;

        var functionExport = FindChildExport<FunctionExport>(classExport, functionContext.Symbol.Name);

        // Preserve original property owners from existing bytecode before modifying
        if (functionExport != null && functionExport.ScriptBytecode != null)
        {
            PreserveOriginalPropertyOwners(functionExport.ScriptBytecode);
        }

        if (functionExport == null)
            functionExport = CreateFunctionExport(functionContext);

        foreach (var variableContext in functionContext.Variables)
        {
            if (SerializeLoadedProperties)
            {
                if (!functionExport.LoadedProperties.Any(x => x.Name.ToString() == variableContext.Symbol.Name))
                    functionExport.LoadedProperties = (functionExport.LoadedProperties ?? Array.Empty<FProperty>())
                        .Concat(new[] { CreateFProperty(variableContext.Symbol) })
                        .ToArray();
            }
            else
            {
                var export = FindChildExport<PropertyExport>(functionExport, variableContext.Symbol.Name);
                (var index, var propExport) = CreatePropertyAsPropertyExport(variableContext.Symbol);
                functionExport!.Children = (functionExport.Children ?? Array.Empty<FPackageIndex>())
                    .Concat(new[] { index })
                    .ToArray();
            }
        }

        functionExport!.ScriptBytecode = GetFixedBytecode(functionContext.Bytecode);

        // Clear the preserved owners after linking this function
        ClearPreservedPropertyOwners();
    }

    /// <summary>
    /// Clears the preserved property owners dictionary and K2Node tracking
    /// </summary>
    protected void ClearPreservedPropertyOwners()
    {
        _originalPropertyFieldPaths.Clear();
        _k2NodeVariablesWithEmptyPath.Clear();
    }

    /// <summary>
    /// Extracts and preserves ResolvedOwner values from original bytecode before it's replaced.
    /// This allows us to maintain correct property ownership when recompiling.
    /// </summary>
    protected void PreserveOriginalPropertyOwners(KismetExpression[] originalBytecode)
    {
        foreach (var expr in originalBytecode.Flatten())
        {
            KismetPropertyPointer? pointer = expr switch
            {
                EX_ArrayConst arrayConst => arrayConst.InnerProperty,
                EX_ClassSparseDataVariable classSparseDataVariable => classSparseDataVariable.Variable,
                EX_Context ctx => ctx.RValuePointer,
                EX_DefaultVariable defaultVariable => defaultVariable.Variable,
                EX_InstanceVariable instanceVariable => instanceVariable.Variable,
                EX_Let let => let.Value,
                EX_LetValueOnPersistentFrame letValueOnPersistentFrame => letValueOnPersistentFrame.DestinationProperty,
                EX_LocalOutVariable localOutVariable => localOutVariable.Variable,
                EX_LocalVariable localVariable => localVariable.Variable,
                EX_PropertyConst propertyConst => propertyConst.Property,
                EX_SetConst setConst => setConst.InnerProperty,
                EX_StructMemberContext structMemberContext => structMemberContext.StructMemberExpression,
                EX_MapConst mapConst => mapConst.KeyProperty, // Handle both key and value separately
                _ => null
            };
            var pointerKind = expr switch
            {
                EX_ArrayConst => PropertyPointerKind.ArrayInner,
                _ => PropertyPointerKind.Default
            };

            // Preserve properties with non-empty Path
            if (pointer?.New != null && pointer.New.Path.Length > 0)
            {
                var propertyName = pointer.New.Path[0].ToString();
                TryPreserveFieldPath(propertyName, pointer.New, pointerKind);
            }
            // Track variables whose property pointers had empty Path/owner in the original bytecode.
            // These typically appear as EX_Let.Value where the matching Variable property
            // pointer (EX_LocalVariable / EX_InstanceVariable) carries the actual name.
            else if (pointer?.New != null && pointer.New.Path.Length == 0 && pointer.New.ResolvedOwner.Index == 0)
            {
                switch (expr)
                {
                    case EX_Let letExpr:
                        if (letExpr.Variable is EX_LocalVariable localVar &&
                            localVar.Variable?.New != null &&
                            localVar.Variable.New.Path.Length > 0)
                        {
                            var name = localVar.Variable.New.Path[0].ToString();
                            _k2NodeVariablesWithEmptyPath.Add(name);
                        }
                        else if (letExpr.Variable is EX_InstanceVariable instVar &&
                                 instVar.Variable?.New != null &&
                                 instVar.Variable.New.Path.Length > 0)
                        {
                            var name = instVar.Variable.New.Path[0].ToString();
                            _k2NodeVariablesWithEmptyPath.Add(name);
                        }
                        break;
                }
            }

            // Handle MapConst value property separately
            if (expr is EX_MapConst mapConstValue && mapConstValue.ValueProperty?.New != null && mapConstValue.ValueProperty.New.Path.Length > 0)
            {
                var propertyName = mapConstValue.ValueProperty.New.Path[0].ToString();
                TryPreserveFieldPath(propertyName, mapConstValue.ValueProperty.New, PropertyPointerKind.Default);
            }
        }
    }

    private void TryPreserveFieldPath(string propertyName, FFieldPath fieldPath, PropertyPointerKind kind)
    {
        if (_originalPropertyFieldPaths.ContainsKey((kind, propertyName)))
        {
            return;
        }

        var pathSegments = fieldPath.Path.Select(name => name.ToString()).ToArray();
        _originalPropertyFieldPaths[(kind, propertyName)] = new PreservedFieldPath(pathSegments, fieldPath.ResolvedOwner);
    }

    protected abstract FPackageIndex EnsurePackageImported(string objectName, bool bImportOptional = false);

    protected abstract FPackageIndex EnsureObjectImported(FPackageIndex parent, string objectName, string className, bool bImportOptional = false);

    protected abstract FPackageIndex CreateProcedureImport(ProcedureSymbol symbol);

    protected abstract IEnumerable<(object ImportOrExport, FPackageIndex PackageIndex)> GetPackageIndexByLocalName(string name);

    protected abstract IEnumerable<(object ImportOrExport, FPackageIndex PackageIndex)> GetPackageIndexByFullName(string name);

    protected abstract T CreateDefaultAsset();

    public virtual PackageLinker<T> LinkCompiledScript(CompiledScriptContext scriptContext)
    {
        foreach (var functionContext in scriptContext.Functions)
        {
            LinkCompiledFunction(functionContext);
        }

        foreach (var classContext in scriptContext.Classes)
        {
            var classExport = FindChildExport<ClassExport>(null, classContext.Symbol.Name);
            if (classExport == null)
                classExport = CreateClassExport(classContext);

            foreach (var variableContext in classContext.Variables)
            {
                if (SerializeLoadedProperties)
                {
                    if (!classExport.LoadedProperties.Any(x => x.Name.ToString() == variableContext.Symbol.Name))
                        classExport.LoadedProperties = (classExport.LoadedProperties ?? Array.Empty<FProperty>())
                            .Concat(new[] { CreateFProperty(variableContext.Symbol) })
                            .ToArray();
                }
                else
                {
                    var export = FindChildExport<PropertyExport>(classExport, variableContext.Symbol.Name);
                    (var index, var propExport) = CreatePropertyAsPropertyExport(variableContext.Symbol);
                    classExport!.Children = (classExport.Children ?? Array.Empty<FPackageIndex>())
                        .Concat(new[] { index })
                        .ToArray();
                }
            }

            foreach (var functionContext in classContext.Functions)
            {
                LinkCompiledFunction(functionContext);
            }
        }

        return this;
    }

    public virtual T Build()
    {
        return Package;
    }

    [GeneratedRegex("_(\\d+)")]
    private static partial Regex NameIdSuffix();
}
