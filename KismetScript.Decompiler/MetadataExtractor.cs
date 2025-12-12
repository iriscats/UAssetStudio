using KismetScript.Utilities;
using KismetScript.Utilities.Metadata;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.UnrealTypes;

namespace KismetScript.Decompiler;

/// <summary>
/// Extracts metadata from a UAsset for independent compilation.
/// </summary>
public class MetadataExtractor
{
    private UnrealPackage _asset = default!;

    /// <summary>
    /// Extract metadata from an asset.
    /// </summary>
    public KmsMetadata Extract(UnrealPackage asset)
    {
        _asset = asset;

        return new KmsMetadata
        {
            Version = 1,
            Generated = DateTime.UtcNow,
            SourceAsset = Path.GetFileName(asset.FilePath ?? ""),
            Package = ExtractPackageInfo(),
            EngineVersion = ExtractEngineVersion(),
            Imports = ExtractImports(),
            Exports = ExtractExports(),
            FieldPaths = ExtractFieldPaths(),
            CdoData = ExtractCdoData(),
            NameMap = ExtractNameMap()
        };
    }

    private PackageMetadata ExtractPackageInfo()
    {
        return new PackageMetadata
        {
            Name = GetPackageName(),
            Guid = _asset.PackageGuid.ToString(),
            Flags = GetPackageFlags(),
            LegacyFileVersion = _asset.LegacyFileVersion,
            UsesEventDrivenLoader = _asset.UsesEventDrivenLoader
        };
    }

    private string GetPackageName()
    {
        // Try to get the package name from the asset
        // Usually it's the first export's outer chain root or can be derived from FilePath
        if (_asset.FilePath != null)
        {
            var fileName = Path.GetFileNameWithoutExtension(_asset.FilePath);
            return $"/Game/{fileName}";
        }
        return "/Game/Unknown";
    }

    private List<string> GetPackageFlags()
    {
        var flags = new List<string>();
        var packageFlags = _asset.PackageFlags;

        foreach (EPackageFlags flag in Enum.GetValues(typeof(EPackageFlags)))
        {
            if (flag != EPackageFlags.PKG_None && (packageFlags & flag) != 0)
            {
                flags.Add(flag.ToString());
            }
        }

        return flags;
    }

    private EngineVersionMetadata ExtractEngineVersion()
    {
        var metadata = new EngineVersionMetadata
        {
            ObjectVersion = _asset.ObjectVersion.ToString(),
            ObjectVersionUE5 = _asset.ObjectVersionUE5.ToString(),
            CustomVersions = new Dictionary<string, int>()
        };

        if (_asset.CustomVersionContainer != null)
        {
            foreach (var cv in _asset.CustomVersionContainer)
            {
                if (cv.FriendlyName != null)
                {
                    metadata.CustomVersions[cv.FriendlyName] = cv.Version;
                }
            }
        }

        return metadata;
    }

    private List<ImportMetadata> ExtractImports()
    {
        var imports = new List<ImportMetadata>();

        for (int i = 0; i < _asset.Imports.Count; i++)
        {
            var import = _asset.Imports[i];
            imports.Add(new ImportMetadata
            {
                Index = -(i + 1),  // Import indices are negative
                ObjectName = import.ObjectName.ToString(),
                ClassName = import.ClassName.ToString(),
                ClassPackage = import.ClassPackage.ToString(),
                OuterIndex = import.OuterIndex.Index,
                BImportOptional = import.bImportOptional ? true : null
            });
        }

        return imports;
    }

    private List<ExportMetadata> ExtractExports()
    {
        var exports = new List<ExportMetadata>();

        for (int i = 0; i < _asset.Exports.Count; i++)
        {
            var export = _asset.Exports[i];
            var exportMeta = new ExportMetadata
            {
                Index = i + 1,  // Export indices are 1-based positive
                ObjectName = export.ObjectName.ToString(),
                ClassName = GetExportClassName(export),
                OuterIndex = export.OuterIndex.Index,
                SuperIndex = export.SuperIndex.IsNull() ? null : export.SuperIndex.Index,
                TemplateIndex = export.TemplateIndex.IsNull() ? null : export.TemplateIndex.Index,
                ObjectFlags = GetObjectFlags(export.ObjectFlags),
                IsCDO = export.ObjectFlags.HasFlag(EObjectFlags.RF_ClassDefaultObject) ? true : null
            };

            // Add function-specific flags
            if (export is FunctionExport funcExport)
            {
                exportMeta.FunctionFlags = GetFunctionFlags(funcExport.FunctionFlags);
            }

            // Add class-specific flags
            if (export is ClassExport classExport)
            {
                exportMeta.ClassFlags = GetClassFlags(classExport.ClassFlags);
            }

            // Add dependencies if available
            exportMeta.Dependencies = ExtractExportDependencies(export);

            exports.Add(exportMeta);
        }

        return exports;
    }

    private string GetExportClassName(Export export)
    {
        if (export.ClassIndex.IsImport())
            return _asset.Imports[-export.ClassIndex.Index - 1].ObjectName.ToString();
        if (export.ClassIndex.IsExport())
            return _asset.Exports[export.ClassIndex.Index - 1].ObjectName.ToString();
        return "Object";
    }

    private List<string> GetObjectFlags(EObjectFlags flags)
    {
        var result = new List<string>();

        foreach (EObjectFlags flag in Enum.GetValues(typeof(EObjectFlags)))
        {
            if (flag != EObjectFlags.RF_NoFlags && (flags & flag) != 0)
            {
                result.Add(flag.ToString());
            }
        }

        return result;
    }

    private List<string> GetFunctionFlags(EFunctionFlags flags)
    {
        var result = new List<string>();

        foreach (EFunctionFlags flag in Enum.GetValues(typeof(EFunctionFlags)))
        {
            // Skip FUNC_None and FUNC_AllFlags (composite flags)
            if (flag == EFunctionFlags.FUNC_None || flag == EFunctionFlags.FUNC_AllFlags)
                continue;

            if (((uint)flags & (uint)flag) != 0)
            {
                result.Add(flag.ToString());
            }
        }

        return result.Count > 0 ? result : null!;
    }

    private List<string>? GetClassFlags(EClassFlags flags)
    {
        var result = new List<string>();

        foreach (EClassFlags flag in Enum.GetValues(typeof(EClassFlags)))
        {
            if (flag != EClassFlags.CLASS_None && (flags & flag) != 0)
            {
                result.Add(flag.ToString());
            }
        }

        return result.Count > 0 ? result : null;
    }

    private ExportDependencies? ExtractExportDependencies(Export export)
    {
        // Check if the asset has dependency maps
        if (_asset.DependsMap == null || _asset.DependsMap.Count == 0)
            return null;

        var exportIndex = _asset.Exports.IndexOf(export);
        if (exportIndex < 0 || exportIndex >= _asset.DependsMap.Count)
            return null;

        var deps = _asset.DependsMap[exportIndex];
        if (deps == null || deps.Length == 0)
            return null;

        // The DependsMap contains int values (FPackageIndex indices)
        var depsList = deps.ToList();

        return new ExportDependencies
        {
            SerializationBeforeSerialization = depsList.Count > 0 ? depsList : null
        };
    }

    private Dictionary<string, Dictionary<string, FieldPathMetadata>> ExtractFieldPaths()
    {
        var result = new Dictionary<string, Dictionary<string, FieldPathMetadata>>();

        foreach (var export in _asset.Exports)
        {
            if (export is not FunctionExport funcExport)
                continue;

            var funcName = funcExport.ObjectName.ToString();
            var fieldPaths = new Dictionary<string, FieldPathMetadata>();

            var bytecode = funcExport.ScriptBytecode;
            if (bytecode == null)
                continue;

            // Traverse all expressions and extract property pointers
            foreach (var expr in bytecode.Flatten())
            {
                ExtractPropertyPointersFromExpression(expr, fieldPaths);
            }

            if (fieldPaths.Count > 0)
            {
                result[funcName] = fieldPaths;
            }
        }

        return result;
    }

    private void ExtractPropertyPointersFromExpression(KismetExpression expr, Dictionary<string, FieldPathMetadata> fieldPaths)
    {
        KismetPropertyPointer? pointer = expr switch
        {
            EX_ArrayConst arrayConst => arrayConst.InnerProperty,
            EX_Context ctx => ctx.RValuePointer,
            EX_DefaultVariable defaultVariable => defaultVariable.Variable,
            EX_InstanceVariable instanceVariable => instanceVariable.Variable,
            EX_Let let => let.Value,
            EX_LetValueOnPersistentFrame letPersistent => letPersistent.DestinationProperty,
            EX_LocalVariable localVariable => localVariable.Variable,
            EX_LocalOutVariable localOutVariable => localOutVariable.Variable,
            EX_PropertyConst propertyConst => propertyConst.Property,
            EX_StructMemberContext structMember => structMember.StructMemberExpression,
            EX_MapConst mapConst => mapConst.KeyProperty, // Also has ValueProperty
            EX_SetConst setConst => setConst.InnerProperty,
            EX_ClassSparseDataVariable sparseData => sparseData.Variable,
            _ => null
        };

        if (pointer?.New != null && pointer.New.Path.Length > 0)
        {
            var propertyName = pointer.New.Path[0].ToString();
            if (!fieldPaths.ContainsKey(propertyName))
            {
                fieldPaths[propertyName] = new FieldPathMetadata
                {
                    Path = pointer.New.Path.Select(p => p.ToString()).ToList(),
                    ResolvedOwner = pointer.New.ResolvedOwner.Index,
                    EmptyPath = null
                };
            }
        }
        else if (pointer?.New != null && pointer.New.Path.Length == 0)
        {
            // Handle empty path case for K2Node variables
            // We need to find the associated variable name from the context
        }

        // Handle MapConst's ValueProperty separately
        if (expr is EX_MapConst mapConst2 && mapConst2.ValueProperty?.New != null && mapConst2.ValueProperty.New.Path.Length > 0)
        {
            var propertyName = mapConst2.ValueProperty.New.Path[0].ToString();
            if (!fieldPaths.ContainsKey(propertyName))
            {
                fieldPaths[propertyName] = new FieldPathMetadata
                {
                    Path = mapConst2.ValueProperty.New.Path.Select(p => p.ToString()).ToList(),
                    ResolvedOwner = mapConst2.ValueProperty.New.ResolvedOwner.Index,
                    EmptyPath = null
                };
            }
        }
    }

    private Dictionary<string, CdoMetadata> ExtractCdoData()
    {
        var result = new Dictionary<string, CdoMetadata>();

        // Find all CDO exports
        var cdoExports = _asset.Exports
            .OfType<NormalExport>()
            .Where(x => x.ObjectFlags.HasFlag(EObjectFlags.RF_ClassDefaultObject))
            .ToList();

        foreach (var cdo in cdoExports)
        {
            var cdoName = cdo.ObjectName.ToString();
            var cdoMeta = new CdoMetadata
            {
                SubObjects = ExtractSubObjects(cdo),
                PreservedProperties = ExtractPreservedProperties(cdo)
            };

            result[cdoName] = cdoMeta;
        }

        return result;
    }

    private List<SubObjectMetadata>? ExtractSubObjects(NormalExport cdoExport)
    {
        var cdoIndex = _asset.Exports.IndexOf(cdoExport);
        var subObjects = _asset.Exports
            .OfType<NormalExport>()
            .Where(x => !x.OuterIndex.IsNull() &&
                        x.OuterIndex.Index == cdoIndex + 1 &&  // Export indices are 1-based
                        !x.ObjectName.ToString().StartsWith("Default__"))
            .Select(subObj => new SubObjectMetadata
            {
                Name = subObj.ObjectName.ToString(),
                ClassName = GetExportClassName(subObj),
                OuterIndex = cdoIndex + 1,
                ExportIndex = _asset.Exports.IndexOf(subObj) + 1
            })
            .ToList();

        return subObjects.Count > 0 ? subObjects : null;
    }

    private List<string>? ExtractPreservedProperties(NormalExport cdoExport)
    {
        // These are properties that should be preserved during compilation
        // They are typically special blueprint properties
        var preservedProps = new List<string>();

        if (cdoExport.Data != null)
        {
            foreach (var prop in cdoExport.Data)
            {
                var propName = prop.Name.ToString();
                // Preserve special blueprint properties
                if (propName == "SimpleConstructionScript" ||
                    propName == "InheritableComponentHandler" ||
                    propName == "UberGraphFramePointerProperty" ||
                    propName == "UberGraphFunction" ||
                    propName == "bLegacyNeedToPurgeSkelRefs" ||
                    propName.StartsWith("GeneratedClass") ||
                    propName.StartsWith("Skeleton"))
                {
                    preservedProps.Add(propName);
                }
            }
        }

        return preservedProps.Count > 0 ? preservedProps : null;
    }

    private List<string>? ExtractNameMap()
    {
        // Extract the full name map - required for standalone compilation
        var nameMap = new List<string>();

        // Get all names from the asset
        for (int i = 0; i < _asset.GetNameMapIndexList().Count; i++)
        {
            var name = _asset.GetNameReference(i);
            nameMap.Add(name.ToString());
        }

        return nameMap.Count > 0 ? nameMap : null;
    }
}
