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
