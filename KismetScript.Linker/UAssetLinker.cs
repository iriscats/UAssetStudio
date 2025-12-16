using KismetScript.Compiler.Compiler;
using KismetScript.Compiler.Compiler.Context;
using KismetScript.Utilities;
using KismetScript.Utilities.Metadata;
using System.Text.RegularExpressions;
using UAssetAPI;
using UAssetAPI.CustomVersions;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace KismetScript.Linker;

public partial class UAssetLinker : PackageLinker<UAsset>
{
    private KmsMetadata? _metadata;
    private static KmsMetadata? _pendingMetadata;

    public UAssetLinker()
    {
    }

    public UAssetLinker(UAsset asset) : base(asset)
    {
    }

    /// <summary>
    /// Creates a new UAssetLinker from metadata for standalone compilation.
    /// </summary>
    public UAssetLinker(KmsMetadata metadata) : base()
    {
        _metadata = metadata;
    }

    /// <summary>
    /// Creates a UAssetLinker from metadata (factory method).
    /// </summary>
    public static UAssetLinker FromMetadata(KmsMetadata metadata)
    {
        _pendingMetadata = metadata;
        var linker = new UAssetLinker(metadata);
        _pendingMetadata = null;
        return linker;
    }

    /// <summary>
    /// Creates a UAsset from metadata.
    /// </summary>
    private UAsset CreateAssetFromMetadata(KmsMetadata metadata)
    {
        var asset = new UAsset()
        {
            LegacyFileVersion = metadata.Package.LegacyFileVersion,
            UsesEventDrivenLoader = metadata.Package.UsesEventDrivenLoader,
            Imports = new(),
            DependsMap = new(),
            SoftPackageReferenceList = new(),
            AssetRegistryData = new byte[] { 0, 0, 0, 0 },
            ValorantGarbageData = null,
            Generations = new(),
            PackageGuid = metadata.Package.Guid != null ? Guid.Parse(metadata.Package.Guid) : Guid.NewGuid(),
            RecordedEngineVersion = new()
            {
                Major = 0,
                Minor = 0,
                Patch = 0,
                Changelist = 0,
                Branch = null
            },
            RecordedCompatibleWithEngineVersion = new()
            {
                Major = 0,
                Minor = 0,
                Patch = 0,
                Changelist = 0,
                Branch = null
            },
            ChunkIDs = Array.Empty<int>(),
            PackageSource = 4048401688,
            FolderName = new("None"),
            IsUnversioned = metadata.Package.IsUnversioned,
            FileVersionLicenseeUE = 0,
            ObjectVersion = ParseObjectVersion(metadata.EngineVersion.ObjectVersion),
            ObjectVersionUE5 = ParseObjectVersionUE5(metadata.EngineVersion.ObjectVersionUE5),
            CustomVersionContainer = BuildCustomVersions(metadata.EngineVersion.CustomVersions),
            Exports = new(),
            WorldTileInfo = null,
            PackageFlags = ParsePackageFlags(metadata.Package.Flags),
            BulkData = Array.Empty<byte>(),
        };

        // Use reflection to set internal fields
        var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Set AdditionalPackagesToCook
        var additionalPackagesField = typeof(UAsset).GetField("AdditionalPackagesToCook", bindingFlags);
        additionalPackagesField?.SetValue(asset, new List<FString>());

        // Set doWeHaveWorldTileInfo to false since WorldTileInfo is null
        var worldTileInfoField = typeof(UAsset).GetField("doWeHaveWorldTileInfo", bindingFlags);
        worldTileInfoField?.SetValue(asset, false);

        // Initialize name map from metadata BEFORE creating imports/exports
        // This is critical - names must exist before they can be referenced
        if (metadata.NameMap != null && metadata.NameMap.Count > 0)
        {
            asset.ClearNameIndexList();
            foreach (var name in metadata.NameMap)
            {
                asset.AddNameReference(new FString(name), forceAddDuplicates: true);
            }
        }

        // Build imports from metadata
        BuildImports(asset, metadata.Imports);

        // Build exports from metadata
        BuildExports(asset, metadata.Exports);

        // Initialize DependsMap for each export
        for (int i = 0; i < asset.Exports.Count; i++)
        {
            asset.DependsMap.Add(Array.Empty<int>());
        }

        return asset;
    }

    private static ObjectVersion ParseObjectVersion(string version)
    {
        if (Enum.TryParse<ObjectVersion>(version, out var result))
            return result;
        return ObjectVersion.VER_UE4_FIX_WIDE_STRING_CRC;
    }

    private static ObjectVersionUE5 ParseObjectVersionUE5(string version)
    {
        if (Enum.TryParse<ObjectVersionUE5>(version, out var result))
            return result;
        return ObjectVersionUE5.UNKNOWN;
    }

    private static EPackageFlags ParsePackageFlags(List<string> flags)
    {
        EPackageFlags result = EPackageFlags.PKG_None;
        foreach (var flag in flags)
        {
            if (Enum.TryParse<EPackageFlags>(flag, out var f))
                result |= f;
        }
        return result;
    }

    private static List<CustomVersion> BuildCustomVersions(Dictionary<string, int> customVersions)
    {
        var result = new List<CustomVersion>();
        var knownVersions = new Dictionary<string, Guid>
        {
            { "FCoreObjectVersion", Guid.Parse("{375EC13C-06E4-48FB-B500-84F0262A717E}") },
            { "FEditorObjectVersion", Guid.Parse("{E4B068ED-F494-42E9-A231-DA0B2E46BB41}") },
            { "FFrameworkObjectVersion", Guid.Parse("{CFFC743F-43B0-4480-9391-14DF171D2073}") },
            { "FSequencerObjectVersion", Guid.Parse("{7B5AE74C-D270-4C10-A958-57980B212A5A}") },
            { "FAnimPhysObjectVersion", Guid.Parse("{29E575DD-E0A3-4627-9D10-D276232CDCEA}") },
            { "FFortniteMainBranchObjectVersion", Guid.Parse("{601D1886-AC64-4F84-AA16-D3DE0DEAC7D6}") },
            { "FReleaseObjectVersion", Guid.Parse("{9C54D522-A826-4FBE-9421-074661B482D0}") },
        };

        foreach (var cv in customVersions)
        {
            if (knownVersions.TryGetValue(cv.Key, out var key))
            {
                result.Add(new CustomVersion
                {
                    Key = key,
                    FriendlyName = cv.Key,
                    Version = cv.Value,
                    IsSerialized = false
                });
            }
        }

        return result;
    }

    private static void BuildImports(UAsset asset, List<ImportMetadata> imports)
    {
        foreach (var importMeta in imports.OrderBy(x => -x.Index))  // Order by index (most negative first)
        {
            var import = new Import
            {
                ObjectName = GetExistingFName(asset, importMeta.ObjectName),
                ClassName = GetExistingFName(asset, importMeta.ClassName),
                ClassPackage = GetExistingFName(asset, importMeta.ClassPackage),
                OuterIndex = new FPackageIndex(importMeta.OuterIndex),
                bImportOptional = importMeta.BImportOptional ?? false
            };
            asset.Imports.Add(import);
        }
    }

    private static void BuildExports(UAsset asset, List<ExportMetadata> exports)
    {
        foreach (var exportMeta in exports.OrderBy(x => x.Index))  // Order by index
        {
            Export export = CreateExportFromMetadata(asset, exportMeta);
            asset.Exports.Add(export);
        }
    }

    private static Export CreateExportFromMetadata(UAsset asset, ExportMetadata exportMeta)
    {
        Export export;

        // Determine export type based on className and flags
        if (exportMeta.ClassName.EndsWith("GeneratedClass") ||
            exportMeta.ClassName == "Class" ||
            exportMeta.ClassFlags != null)
        {
            var classExport = new ClassExport(asset, Array.Empty<byte>())
            {
                ClassFlags = ParseClassFlags(exportMeta.ClassFlags),
                LoadedProperties = RestoreLoadedProperties(asset, exportMeta.LoadedProperties),
                Data = new List<PropertyData>(),
                FuncMap = new(),
                Children = Array.Empty<FPackageIndex>(),
                Interfaces = Array.Empty<SerializedInterfaceReference>(),
                ClassConfigName = GetExistingFName(asset, "Engine"),
                ClassWithin = new FPackageIndex(0),
                ClassGeneratedBy = new FPackageIndex(0),
                ClassDefaultObject = new FPackageIndex(0),
                bCooked = true,
                Field = new UField { Next = new FPackageIndex(0) },
                SuperStruct = new FPackageIndex(exportMeta.SuperIndex ?? 0),
                // StructExport fields - required for serialization
                ScriptBytecode = Array.Empty<KismetExpression>(),
            };
            export = classExport;
        }
        else if (exportMeta.ClassName == "Function" || exportMeta.FunctionFlags != null)
        {
            var funcExport = new FunctionExport(asset, Array.Empty<byte>())
            {
                FunctionFlags = ParseFunctionFlags(exportMeta.FunctionFlags),
                ScriptBytecode = Array.Empty<KismetExpression>(),
                LoadedProperties = RestoreLoadedProperties(asset, exportMeta.LoadedProperties),
                Data = new List<PropertyData>(),
                Children = Array.Empty<FPackageIndex>(),
                Field = new UField { Next = new FPackageIndex(0) },
                SuperStruct = new FPackageIndex(exportMeta.SuperIndex ?? 0),
            };
            export = funcExport;
        }
        else
        {
            export = new NormalExport(asset, Array.Empty<byte>())
            {
                Data = new List<PropertyData>()
            };
        }

        export.ObjectName = GetExistingFName(asset, exportMeta.ObjectName);
        export.OuterIndex = new FPackageIndex(exportMeta.OuterIndex);
        export.SuperIndex = new FPackageIndex(exportMeta.SuperIndex ?? 0);
        export.TemplateIndex = new FPackageIndex(exportMeta.TemplateIndex ?? 0);
        export.ObjectFlags = ParseObjectFlags(exportMeta.ObjectFlags);

        // Set ClassIndex based on className
        export.ClassIndex = FindOrCreateClassImport(asset, exportMeta.ClassName);

        // Restore Extras data
        if (!string.IsNullOrEmpty(exportMeta.Extras))
        {
            export.Extras = Convert.FromBase64String(exportMeta.Extras);
        }

        // Restore dependencies
        if (exportMeta.Dependencies != null)
        {
            if (exportMeta.Dependencies.SerializationBeforeSerialization != null)
            {
                export.SerializationBeforeSerializationDependencies = exportMeta.Dependencies.SerializationBeforeSerialization
                    .Select(i => new FPackageIndex(i)).ToList();
            }
            if (exportMeta.Dependencies.CreateBeforeSerialization != null)
            {
                export.CreateBeforeSerializationDependencies = exportMeta.Dependencies.CreateBeforeSerialization
                    .Select(i => new FPackageIndex(i)).ToList();
            }
            if (exportMeta.Dependencies.SerializationBeforeCreate != null)
            {
                export.SerializationBeforeCreateDependencies = exportMeta.Dependencies.SerializationBeforeCreate
                    .Select(i => new FPackageIndex(i)).ToList();
            }
            if (exportMeta.Dependencies.CreateBeforeCreate != null)
            {
                export.CreateBeforeCreateDependencies = exportMeta.Dependencies.CreateBeforeCreate
                    .Select(i => new FPackageIndex(i)).ToList();
            }
        }

        return export;
    }

    private static FPackageIndex FindOrCreateClassImport(UAsset asset, string className)
    {
        // First, look for existing import
        for (int i = 0; i < asset.Imports.Count; i++)
        {
            if (asset.Imports[i].ObjectName.ToString() == className)
            {
                return FPackageIndex.FromImport(i);
            }
        }

        // Also check exports (for self-referencing classes)
        for (int i = 0; i < asset.Exports.Count; i++)
        {
            if (asset.Exports[i].ObjectName.ToString() == className)
            {
                return FPackageIndex.FromExport(i);
            }
        }

        // Return null index if not found (will be resolved later during linking)
        return new FPackageIndex(0);
    }

    private static EObjectFlags ParseObjectFlags(List<string> flags)
    {
        EObjectFlags result = EObjectFlags.RF_NoFlags;
        foreach (var flag in flags)
        {
            if (Enum.TryParse<EObjectFlags>(flag, out var f))
                result |= f;
        }
        return result;
    }

    private static EFunctionFlags ParseFunctionFlags(List<string>? flags)
    {
        if (flags == null) return EFunctionFlags.FUNC_None;

        EFunctionFlags result = EFunctionFlags.FUNC_None;
        foreach (var flag in flags)
        {
            if (Enum.TryParse<EFunctionFlags>(flag, out var f))
                result |= f;
        }
        return result;
    }

    private static EClassFlags ParseClassFlags(List<string>? flags)
    {
        if (flags == null) return EClassFlags.CLASS_None;

        EClassFlags result = EClassFlags.CLASS_None;
        foreach (var flag in flags)
        {
            if (Enum.TryParse<EClassFlags>(flag, out var f))
                result |= f;
        }
        return result;
    }

    private static FProperty[] RestoreLoadedProperties(UAsset asset, List<FPropertyMetadata>? properties)
    {
        if (properties == null || properties.Count == 0)
            return Array.Empty<FProperty>();

        return properties.Select(p => RestoreFProperty(asset, p)).ToArray();
    }

    private static FProperty RestoreFProperty(UAsset asset, FPropertyMetadata meta)
    {
        FProperty prop = meta.SerializedType switch
        {
            "ClassProperty" => new FClassProperty
            {
                PropertyClass = new FPackageIndex(meta.PropertyClass ?? 0),
                MetaClass = new FPackageIndex(meta.MetaClass ?? 0)
            },
            "SoftClassProperty" => new FSoftClassProperty
            {
                PropertyClass = new FPackageIndex(meta.PropertyClass ?? 0),
                MetaClass = new FPackageIndex(meta.MetaClass ?? 0)
            },
            "ObjectProperty" => new FObjectProperty
            {
                PropertyClass = new FPackageIndex(meta.PropertyClass ?? 0)
            },
            "WeakObjectProperty" => new FWeakObjectProperty
            {
                PropertyClass = new FPackageIndex(meta.PropertyClass ?? 0)
            },
            "SoftObjectProperty" => new FSoftObjectProperty
            {
                PropertyClass = new FPackageIndex(meta.PropertyClass ?? 0)
            },
            "DelegateProperty" => new FDelegateProperty
            {
                SignatureFunction = new FPackageIndex(meta.SignatureFunction ?? 0)
            },
            "MulticastDelegateProperty" => new FMulticastDelegateProperty
            {
                SignatureFunction = new FPackageIndex(meta.SignatureFunction ?? 0)
            },
            "MulticastInlineDelegateProperty" => new FMulticastInlineDelegateProperty
            {
                SignatureFunction = new FPackageIndex(meta.SignatureFunction ?? 0)
            },
            "InterfaceProperty" => new FInterfaceProperty
            {
                InterfaceClass = new FPackageIndex(meta.InterfaceClass ?? 0)
            },
            "StructProperty" => new FStructProperty
            {
                Struct = new FPackageIndex(meta.Struct ?? 0)
            },
            "EnumProperty" => new FEnumProperty
            {
                Enum = new FPackageIndex(meta.Enum ?? 0),
                UnderlyingProp = meta.UnderlyingProp != null ? RestoreFProperty(asset, meta.UnderlyingProp) : null!
            },
            "ByteProperty" => new FByteProperty
            {
                Enum = new FPackageIndex(meta.Enum ?? 0)
            },
            "BoolProperty" => new FBoolProperty
            {
                FieldSize = (byte)(meta.FieldSize ?? 0),
                ByteOffset = (byte)(meta.ByteOffset ?? 0),
                ByteMask = (byte)(meta.ByteMask ?? 0),
                FieldMask = (byte)(meta.FieldMask ?? 0),
                NativeBool = meta.NativeBool ?? false,
                Value = meta.BoolValue ?? false
            },
            "ArrayProperty" => new FArrayProperty
            {
                Inner = meta.Inner != null ? RestoreFProperty(asset, meta.Inner) : null!
            },
            "SetProperty" => new FSetProperty
            {
                ElementProp = meta.ElementProp != null ? RestoreFProperty(asset, meta.ElementProp) : null!
            },
            "MapProperty" => new FMapProperty
            {
                KeyProp = meta.KeyProp != null ? RestoreFProperty(asset, meta.KeyProp) : null!,
                ValueProp = meta.ValueProp != null ? RestoreFProperty(asset, meta.ValueProp) : null!
            },
            _ => new FGenericProperty()
        };

        // Set base FField properties - use GetExistingFName to avoid adding to NameMap
        prop.SerializedType = GetExistingFName(asset, meta.SerializedType);
        prop.Name = GetExistingFName(asset, meta.Name);
        prop.Flags = ParseObjectFlags(meta.Flags ?? new List<string>());

        // Set base FProperty properties
        prop.ArrayDim = Enum.TryParse<EArrayDim>(meta.ArrayDim, out var arrDim) ? arrDim : EArrayDim.TArray;
        prop.ElementSize = meta.ElementSize;
        prop.PropertyFlags = ParsePropertyFlags(meta.PropertyFlags);
        prop.RepIndex = (ushort)meta.RepIndex;
        prop.RepNotifyFunc = GetExistingFName(asset, meta.RepNotifyFunc ?? "None");
        prop.BlueprintReplicationCondition = Enum.TryParse<ELifetimeCondition>(meta.BlueprintReplicationCondition, out var replCond)
            ? replCond : ELifetimeCondition.COND_None;

        return prop;
    }

    private static EPropertyFlags ParsePropertyFlags(List<string>? flags)
    {
        if (flags == null) return EPropertyFlags.CPF_None;

        EPropertyFlags result = EPropertyFlags.CPF_None;
        foreach (var flag in flags)
        {
            if (Enum.TryParse<EPropertyFlags>(flag, out var f))
                result |= f;
        }
        return result;
    }

    /// <summary>
    /// Finds a field path from metadata for the given function and property name.
    /// </summary>
    protected FieldPathMetadata? FindFieldPathInMetadata(string functionName, string propertyName)
    {
        if (_metadata?.FieldPaths == null)
            return null;

        if (_metadata.FieldPaths.TryGetValue(functionName, out var functionPaths))
        {
            if (functionPaths.TryGetValue(propertyName, out var fieldPath))
            {
                return fieldPath;
            }
        }

        return null;
    }

    protected override UAsset CreateDefaultAsset()
    {
        // If we have pending metadata, create asset from it
        if (_pendingMetadata != null)
        {
            return CreateAssetFromMetadata(_pendingMetadata);
        }

        var asset = new UAsset()
        {
            LegacyFileVersion = -7,
            UsesEventDrivenLoader = true,
            Imports = new(),
            DependsMap = new(),
            SoftPackageReferenceList = new(),
            AssetRegistryData = new byte[] { 0, 0, 0, 0 },
            ValorantGarbageData = null,
            Generations = new(),
            PackageGuid = Guid.NewGuid(),
            RecordedEngineVersion = new()
            {
                Major = 0,
                Minor = 0,
                Patch = 0,
                Changelist = 0,
                Branch = null
            },
            RecordedCompatibleWithEngineVersion = new()
            {
                Major = 0,
                Minor = 0,
                Patch = 0,
                Changelist = 0,
                Branch = null
            },
            ChunkIDs = Array.Empty<int>(),
            PackageSource = 4048401688,
            FolderName = new("None"),
            IsUnversioned = true,
            FileVersionLicenseeUE = 0,
            ObjectVersion = ObjectVersion.VER_UE4_FIX_WIDE_STRING_CRC,
            ObjectVersionUE5 = ObjectVersionUE5.UNKNOWN,
            CustomVersionContainer = new()
            {
                new(){ Key = Guid.Parse("{375EC13C-06E4-48FB-B500-84F0262A717E}"), FriendlyName = "FCoreObjectVersion", Version = 3, IsSerialized = false },
                new(){ Key = Guid.Parse("{E4B068ED-F494-42E9-A231-DA0B2E46BB41}"), FriendlyName = "FEditorObjectVersion", Version = 34, IsSerialized = false },
                new(){ Key = Guid.Parse("{CFFC743F-43B0-4480-9391-14DF171D2073}"), FriendlyName = "FFrameworkObjectVersion", Version = 35, IsSerialized = false },
                new(){ Key = Guid.Parse("{7B5AE74C-D270-4C10-A958-57980B212A5A}"), FriendlyName = "FSequencerObjectVersion", Version = 11, IsSerialized = false },
                new(){ Key = Guid.Parse("{29E575DD-E0A3-4627-9D10-D276232CDCEA}"), FriendlyName = "FAnimPhysObjectVersion", Version = 17, IsSerialized = false },
                new(){ Key = Guid.Parse("{601D1886-AC64-4F84-AA16-D3DE0DEAC7D6}"), FriendlyName = "FFortniteMainBranchObjectVersion", Version = 27, IsSerialized = false },
                new(){ Key = Guid.Parse("{9C54D522-A826-4FBE-9421-074661B482D0}"), FriendlyName = "FReleaseObjectVersion", Version = 23, IsSerialized = false },
            },
            Exports = new(),
            WorldTileInfo = null,
            PackageFlags = EPackageFlags.PKG_FilterEditorOnly,
        };
        asset.ClearNameIndexList();
        return asset;
    }

    protected override FPackageIndex EnsurePackageImported(string objectName, bool bImportOptional = false)
    {
        if (objectName == null)
        return new FPackageIndex(0);

        var import = Package.FindImportByObjectName(objectName);
        if (import == null)
        {
            import = new Import()
            {
                ObjectName = new(Package, objectName),
            OuterIndex = new FPackageIndex(0),
                ClassPackage = new(Package, objectName),
                ClassName = new(Package, "Package"),
                bImportOptional = bImportOptional
            };
            Package.Imports.Add(import);
        }

        return FPackageIndex.FromImport(Package.Imports.IndexOf(import));
    }

    protected override FPackageIndex EnsureObjectImported(FPackageIndex parent, string objectName, string className, bool bImportOptional = false)
    {
        var import = Package.FindImportByObjectName(objectName);
        if (import == null)
        {
            var parentImport = parent.ToImport(Package);
            import = new Import()
            {
                ObjectName = new(Package, objectName),
                OuterIndex = parent,
                ClassPackage = parentImport.ObjectName,
                ClassName = new(Package, className),
                bImportOptional = bImportOptional
            };
            Package.Imports.Add(import);
        }

        return FPackageIndex.FromImport(Package.Imports.IndexOf(import));
    }

    public override UAssetLinker LinkCompiledScript(CompiledScriptContext scriptContext)
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
                // Skip variables that only have initializers (CDO property values)
                // These are inherited properties - we don't need to create new property definitions
                // The values will be linked in LinkClassCDO
                if (variableContext.Initializer != null &&
                    !HasExistingPropertyDefinition(classExport, variableContext.Symbol.Name))
                {
                    continue;
                }

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

            // Link CDO (Class Default Object) with property values
            LinkClassCDO(classExport, classContext, scriptContext.Objects);
        }

        // Link top-level objects (non-CDO objects)
        foreach (var objectContext in scriptContext.Objects)
        {
            LinkTopLevelObject(objectContext);
        }

        return this;
    }

    /// <summary>
    /// Links the CDO (Class Default Object) for a class, populating its Data with property values.
    /// </summary>
    private void LinkClassCDO(ClassExport classExport, CompiledClassContext classContext, List<CompiledObjectContext> objects)
    {
        var cdoName = $"Default__{classContext.Symbol.Name}";
        var cdoExport = Package.Exports
            .OfType<NormalExport>()
            .FirstOrDefault(x => x.ObjectName.ToString() == cdoName);

        if (cdoExport == null)
            return; // CDO doesn't exist, nothing to update

        // Initialize Data for new CDOs (or reuse existing list)
        cdoExport.Data ??= new List<PropertyData>();

        // First, link sub-objects referenced by this CDO
        foreach (var objectContext in objects)
        {
            LinkSubObject(objectContext, cdoExport);
        }

        // Then populate CDO Data with property values from compiled class variables
        foreach (var variableContext in classContext.Variables)
        {
            if (variableContext.Initializer != null)
            {
                var existingPropIndex = cdoExport.Data.FindIndex(p => p.Name.ToString() == variableContext.Symbol.Name);
                var propData = CreatePropertyDataFromValue(
                    variableContext.Symbol.Name,
                    variableContext.Symbol.Declaration?.Type?.Text ?? "Object",
                    variableContext.Initializer);

                if (propData != null)
                {
                    if (existingPropIndex >= 0)
                        cdoExport.Data[existingPropIndex] = propData;
                    else
                        cdoExport.Data.Add(propData);
                }
            }
        }
    }

    /// <summary>
    /// Links a sub-object export, populating its Data with property values.
    /// </summary>
    private void LinkSubObject(CompiledObjectContext objectContext, NormalExport outerExport)
    {
        // Find existing sub-object export
        var subObjectExport = Package.Exports
            .OfType<NormalExport>()
            .FirstOrDefault(x => x.ObjectName.ToString() == objectContext.Name &&
                                 !x.OuterIndex.IsNull() &&
                                 x.OuterIndex.ToExport(Package) == outerExport);

        if (subObjectExport == null)
            return; // Sub-object doesn't exist, nothing to update

        // Initialize Data for new sub-objects (or reuse existing list)
        subObjectExport.Data ??= new List<PropertyData>();

        // Populate Data from compiled properties
        foreach (var prop in objectContext.Properties)
        {
            var existingPropIndex = subObjectExport.Data.FindIndex(p => p.Name.ToString() == prop.Name);
            var propData = CreatePropertyDataFromValue(prop.Name, prop.Type, prop.Value);

            if (propData != null)
            {
                if (existingPropIndex >= 0)
                    subObjectExport.Data[existingPropIndex] = propData;
                else
                    subObjectExport.Data.Add(propData);
            }
        }
    }

    /// <summary>
    /// Links a top-level object export, populating its Data with property values.
    /// </summary>
    private void LinkTopLevelObject(CompiledObjectContext objectContext)
    {
        // Find existing top-level object export
        var objectExport = Package.Exports
            .OfType<NormalExport>()
            .FirstOrDefault(x => x.ObjectName.ToString() == objectContext.Name);

        if (objectExport == null)
            return; // Object doesn't exist, nothing to update

        // Initialize Data (or reuse existing list)
        objectExport.Data ??= new List<PropertyData>();

        // Populate Data from compiled properties
        foreach (var prop in objectContext.Properties)
        {
            var existingPropIndex = objectExport.Data.FindIndex(p => p.Name.ToString() == prop.Name);
            var propData = CreatePropertyDataFromValue(prop.Name, prop.Type, prop.Value);

            if (propData != null)
            {
                if (existingPropIndex >= 0)
                    objectExport.Data[existingPropIndex] = propData;
                else
                    objectExport.Data.Add(propData);
            }
        }
    }

    /// <summary>
    /// Creates a PropertyData instance from a compiled property value.
    /// </summary>
    private PropertyData? CreatePropertyDataFromValue(string name, string typeHint, CompiledPropertyValue? value)
    {
        if (value == null)
            return null;

        var fname = new FName(Package, name);

        if (value.FloatValue.HasValue)
        {
            return new FloatPropertyData(fname) { Value = value.FloatValue.Value };
        }
        if (value.IntValue.HasValue)
        {
            return new IntPropertyData(fname) { Value = value.IntValue.Value };
        }
        if (value.BoolValue.HasValue)
        {
            return new BoolPropertyData(fname) { Value = value.BoolValue.Value };
        }
        if (value.StringValue != null)
        {
            // Check if this is a Text type based on typeHint
            if (typeHint == "Text")
            {
                return new TextPropertyData(fname) { Value = new FString(value.StringValue) };
            }
            // Check if this is a SoftObject path string
            if (typeHint == "SoftObject")
            {
                var assetPath = new FName(Package, value.StringValue);
                var softPath = new FSoftObjectPath(null, assetPath, new FString(""));
                return new SoftObjectPropertyData(fname) { Value = softPath };
            }
            return new StrPropertyData(fname) { Value = new FString(value.StringValue) };
        }
        if (value.ObjectReference != null)
        {
            var results = GetPackageIndexByLocalName(value.ObjectReference);
            if (results == null || !results.Any())
            {
                // Object not found - try to find it in the compiled script context and create an import
                // This handles cases where user adds new object references not in the original asset
                var packageIndex = TryCreateImportForReference(value.ObjectReference, typeHint);
                if (packageIndex.Index == 0)
                {
                    Console.WriteLine($"Warning: Object reference '{value.ObjectReference}' not found and could not be imported");
                    return new ObjectPropertyData(fname) { Value = new FPackageIndex(0) };
                }
                return new ObjectPropertyData(fname) { Value = packageIndex };
            }
            var result = results.First();
            return new ObjectPropertyData(fname) { Value = result.PackageIndex };
        }
        if (value.ArrayValue != null)
        {
            var arrayProp = new ArrayPropertyData(fname);
            var elements = new List<PropertyData>();

            // Extract inner type from Array<...>
            string innerTypeHint = typeHint;
            if (typeHint.StartsWith("Array<") && typeHint.EndsWith(">"))
            {
                innerTypeHint = typeHint.Substring(6, typeHint.Length - 7);
            }

            foreach (var element in value.ArrayValue)
            {
                // For array elements, we need to infer the element type
                var elementProp = CreatePropertyDataFromValue("0", innerTypeHint, element);
                if (elementProp != null)
                    elements.Add(elementProp);
            }

            arrayProp.Value = elements.ToArray();

            // Determine array type from first element or type hint
            if (elements.Any())
            {
                arrayProp.ArrayType = new FName(Package, elements[0].PropertyType.ToString());
            }
            else if (typeHint.StartsWith("Array<"))
            {
                var innerType = typeHint.Substring(6, typeHint.Length - 7);
                arrayProp.ArrayType = new FName(Package, InferArrayTypeFromTypeHint(innerType));
            }

            return arrayProp;
        }

        return null;
    }

    /// <summary>
    /// Infers the UE property type name from a KMS type hint.
    /// </summary>
    private static string InferArrayTypeFromTypeHint(string typeHint)
    {
        if (typeHint.StartsWith("Object<"))
            return "ObjectProperty";
        if (typeHint.StartsWith("Struct<"))
            return "StructProperty";
        if (typeHint == "SoftObject")
            return "SoftObjectProperty";

        return typeHint switch
        {
            "float" => "FloatProperty",
            "int" => "IntProperty",
            "bool" => "BoolProperty",
            "string" => "StrProperty",
            "byte" => "ByteProperty",
            "Text" => "TextProperty",
            _ => "ObjectProperty"
        };
    }

    protected override void LinkCompiledFunction(CompiledFunctionContext functionContext)
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

    protected override FPackageIndex CreateProcedureImport(ProcedureSymbol symbol)
    {
        var import = new Import()
        {
            ObjectName = new(Package, symbol.Name),
            OuterIndex = FindPackageIndexInAsset(symbol?.DeclaringClass),
            ClassPackage = new(Package, symbol?.DeclaringPackage?.Name),
            ClassName = new(Package, "Function"),
            bImportOptional = false
        };
        Package.Imports.Add(import);
        return FPackageIndex.FromImport(Package.Imports.IndexOf(import));
    }

    protected override IEnumerable<(object ImportOrExport, FPackageIndex PackageIndex)> GetPackageIndexByLocalName(string name)
    {
        if (Package is UAsset uasset)
        {
            foreach (var import in uasset.Imports)
            {
                if (import.ObjectName.ToString() == name)
                {
                    yield return (import, new FPackageIndex(-(uasset.Imports.IndexOf(import) + 1)));
                }
            }
        }
        else
        {
            throw new NotImplementedException("Zen import");
        }
        foreach (var export in Package.Exports)
        {
            if (export.ObjectName.ToString() == name)
            {
                yield return (export, new FPackageIndex(+(Package.Exports.IndexOf(export) + 1)));
            }
        }
    }

    protected override IEnumerable<(object ImportOrExport, FPackageIndex PackageIndex)> GetPackageIndexByFullName(string name)
    {
        if (Package is UAsset uasset)
        {
            foreach (var import in uasset.Imports)
            {
                var importFullName = GetFullName(import);
                if (importFullName == name)
                {
                    yield return (import, new FPackageIndex(-(uasset.Imports.IndexOf(import) + 1)));
                }
            }
        }
        else
        {
            throw new NotImplementedException("Zen import");
        }
        foreach (var export in Package.Exports)
        {
            var exportFullName = GetFullName(export);
            if (exportFullName == name)
            {
                yield return (export, new FPackageIndex(+(Package.Exports.IndexOf(export) + 1)));
            }
        }
    }

    public override UAsset Build()
    {
        return Package;
    }

    /// <summary>
    /// Creates an FName that looks up the existing name in NameMap without adding a new entry.
    /// Used in metadata mode where NameMap is pre-populated.
    /// Falls back to standard FName creation if name is not found (for safety).
    /// </summary>
    private static FName GetExistingFName(UAsset asset, string name)
    {
        var fstring = new FString(name);
        if (asset.ContainsNameReference(fstring))
        {
            var index = asset.SearchNameReference(fstring);
            return new FName(asset, index, 0);
        }
        // Fallback - name doesn't exist, use normal creation
        return new FName(asset, name);
    }

    /// <summary>
    /// Checks if a class export already has a property definition for the given name.
    /// This checks both LoadedProperties (UE5+) and Children PropertyExports (UE4).
    /// </summary>
    private bool HasExistingPropertyDefinition(ClassExport classExport, string propertyName)
    {
        // Check LoadedProperties (UE5 FProperties)
        if (classExport.LoadedProperties?.Any(x => x.Name.ToString() == propertyName) == true)
            return true;

        // Check Children exports (UE4 PropertyExports)
        if (classExport.Children != null)
        {
            foreach (var childIndex in classExport.Children)
            {
                if (childIndex.IsExport())
                {
                    var child = childIndex.ToExport(Package);
                    if (child?.ObjectName.ToString() == propertyName)
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Tries to create an import for an object reference that doesn't exist in the package.
    /// Looks for the import declaration in the KMS file to get the full path.
    /// </summary>
    private FPackageIndex TryCreateImportForReference(string objectName, string typeHint)
    {
        // Check if this object is already imported through KMS imports
        // The imports should have been processed during script compilation
        // We need to find the full path for this object from the compiled context

        // For now, try to infer the import path from KMS file structure
        // Object names in KMS that reference game content follow pattern: ObjectName
        // Full paths follow pattern: /Game/Path/To/ObjectName.ObjectName

        // Try to use EnsurePackageImported and EnsureObjectImported to create the import
        // First, we need to extract the package path from context

        // Since we don't have the full path here, we'll need to look it up from the original KMS imports
        // For objects like UPC_ChargeModeSpread, check if there's a declaration in the source

        // Extract class name from typeHint like "Object<ItemUpgradeCategory>"
        string className = "Package";
        if (typeHint.StartsWith("Object<") && typeHint.EndsWith(">"))
        {
            className = typeHint.Substring(7, typeHint.Length - 8);
        }

        // Try to find the import path by checking existing similar imports
        // This is a heuristic approach: look for similar named imports
        if (Package is UAsset uasset)
        {
            foreach (var import in uasset.Imports)
            {
                if (import.ObjectName.ToString().Contains(objectName.Split('_')[0]))
                {
                    // Found a similar import, use its package structure
                    var packagePath = GetImportPackagePath(import);
                    if (!string.IsNullOrEmpty(packagePath))
                    {
                        // Create new import with similar structure
                        return EnsureObjectImported(
                            EnsurePackageImported(packagePath),
                            objectName,
                            className
                        );
                    }
                }
            }
        }

        return new FPackageIndex(0);
    }

    /// <summary>
    /// Gets the package path for an import by traversing its outer chain.
    /// </summary>
    private string GetImportPackagePath(Import import)
    {
        if (Package is not UAsset uasset)
            return string.Empty;

        // Traverse to the root package
        var current = import;
        while (!current.OuterIndex.IsNull() && current.OuterIndex.IsImport())
        {
            current = current.OuterIndex.ToImport(uasset);
        }

        // The root should be a package
        return current.ObjectName.ToString();
    }

    [GeneratedRegex("_(\\d+)")]
    private static partial Regex NameIdSuffix();
}
