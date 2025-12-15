namespace KismetScript.Utilities.Metadata;

/// <summary>
/// Root metadata class for .kms.meta files.
/// Contains all information needed to compile a .kms script independently.
/// </summary>
public class KmsMetadata
{
    public int Version { get; set; } = 1;
    public DateTime? Generated { get; set; }
    public string? SourceAsset { get; set; }
    public PackageMetadata Package { get; set; } = new();
    public EngineVersionMetadata EngineVersion { get; set; } = new();
    public List<ImportMetadata> Imports { get; set; } = new();
    public List<ExportMetadata> Exports { get; set; } = new();
    public Dictionary<string, Dictionary<string, FieldPathMetadata>> FieldPaths { get; set; } = new();
    public Dictionary<string, CdoMetadata> CdoData { get; set; } = new();
    public List<string>? NameMap { get; set; }
}

/// <summary>
/// Package-level metadata including name, GUID, and flags.
/// </summary>
public class PackageMetadata
{
    public string Name { get; set; } = "";
    public string? Guid { get; set; }
    public List<string> Flags { get; set; } = new();
    public int LegacyFileVersion { get; set; } = -7;
    public bool UsesEventDrivenLoader { get; set; } = true;
    public bool IsUnversioned { get; set; } = true;
}

/// <summary>
/// Engine version information for serialization compatibility.
/// </summary>
public class EngineVersionMetadata
{
    public string ObjectVersion { get; set; } = "VER_UE4_FIX_WIDE_STRING_CRC";
    public string ObjectVersionUE5 { get; set; } = "UNKNOWN";
    public Dictionary<string, int> CustomVersions { get; set; } = new();
}

/// <summary>
/// Import table entry metadata.
/// </summary>
public class ImportMetadata
{
    public int Index { get; set; }
    public string ObjectName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string ClassPackage { get; set; } = "";
    public int OuterIndex { get; set; }
    public bool? BImportOptional { get; set; }
}

/// <summary>
/// Export table entry metadata.
/// </summary>
public class ExportMetadata
{
    public int Index { get; set; }
    public string ObjectName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public int OuterIndex { get; set; }
    public int? SuperIndex { get; set; }
    public int? TemplateIndex { get; set; }
    public List<string> ObjectFlags { get; set; } = new();
    public List<string>? FunctionFlags { get; set; }
    public List<string>? ClassFlags { get; set; }
    public bool? IsCDO { get; set; }
    public ExportDependencies? Dependencies { get; set; }
    /// <summary>
    /// Base64 encoded extra binary data for the export.
    /// </summary>
    public string? Extras { get; set; }
    /// <summary>
    /// Loaded properties for FunctionExport/StructExport.
    /// </summary>
    public List<FPropertyMetadata>? LoadedProperties { get; set; }
}

/// <summary>
/// FProperty metadata for function/struct local variables.
/// </summary>
public class FPropertyMetadata
{
    public string SerializedType { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string>? Flags { get; set; }
    public string? ArrayDim { get; set; }
    public int ElementSize { get; set; }
    public List<string>? PropertyFlags { get; set; }
    public int RepIndex { get; set; }
    public string? RepNotifyFunc { get; set; }
    public string? BlueprintReplicationCondition { get; set; }

    // Type-specific fields
    public int? PropertyClass { get; set; }  // FObjectProperty
    public int? MetaClass { get; set; }      // FClassProperty
    public int? SignatureFunction { get; set; }  // FDelegateProperty
    public int? InterfaceClass { get; set; }     // FInterfaceProperty
    public int? Struct { get; set; }         // FStructProperty
    public int? Enum { get; set; }           // FByteProperty, FEnumProperty

    // FBoolProperty
    public int? FieldSize { get; set; }
    public int? ByteOffset { get; set; }
    public int? ByteMask { get; set; }
    public int? FieldMask { get; set; }
    public bool? NativeBool { get; set; }
    public bool? BoolValue { get; set; }

    // Nested properties
    public FPropertyMetadata? Inner { get; set; }          // FArrayProperty
    public FPropertyMetadata? ElementProp { get; set; }    // FSetProperty
    public FPropertyMetadata? KeyProp { get; set; }        // FMapProperty
    public FPropertyMetadata? ValueProp { get; set; }      // FMapProperty
    public FPropertyMetadata? UnderlyingProp { get; set; } // FEnumProperty
}

/// <summary>
/// Export serialization dependencies.
/// </summary>
public class ExportDependencies
{
    public List<int>? SerializationBeforeSerialization { get; set; }
    public List<int>? CreateBeforeSerialization { get; set; }
    public List<int>? SerializationBeforeCreate { get; set; }
    public List<int>? CreateBeforeCreate { get; set; }
}

/// <summary>
/// Field path metadata for property pointer resolution.
/// </summary>
public class FieldPathMetadata
{
    public List<string> Path { get; set; } = new();
    public int ResolvedOwner { get; set; }
    public bool? EmptyPath { get; set; }
}

/// <summary>
/// Class Default Object (CDO) metadata.
/// </summary>
public class CdoMetadata
{
    public List<SubObjectMetadata>? SubObjects { get; set; }
    public List<string>? PreservedProperties { get; set; }
}

/// <summary>
/// Sub-object metadata within a CDO.
/// </summary>
public class SubObjectMetadata
{
    public string Name { get; set; } = "";
    public string ClassName { get; set; } = "";
    public int OuterIndex { get; set; }
    public int ExportIndex { get; set; }
}
