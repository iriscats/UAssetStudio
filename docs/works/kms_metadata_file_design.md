# KMS 元数据文件方案 (.kms.meta)

## 概述

作为完整 KMS 语法扩展的轻量级替代方案，本文档定义了 `.kms.meta` JSON 元数据文件格式。该方案将 KMS 脚本保持人类可读，同时在独立的元数据文件中存储编译所需的二进制元数据。

## 设计原则

1. **关注点分离** - KMS 保持可读性，元数据存储在独立文件
2. **最小化** - 只存储无法从 KMS 推断的必要信息
3. **可选性** - 有元数据文件时独立编译，无元数据时仍可配合原始资产使用
4. **易于生成** - 反编译时自动生成，无需修改 KMS 语法

## 文件命名约定

```
BP_MyClass.kms       # KMS 脚本文件
BP_MyClass.kms.meta  # 元数据文件（与 KMS 同目录）
```

## 元数据文件结构

### 完整示例

```json
{
  "$schema": "https://uassetstudio.dev/schemas/kms-meta-v1.json",
  "version": 1,
  "generated": "2024-12-12T10:30:00Z",
  "sourceAsset": "BP_PlayerControllerBase.uasset",

  "package": {
    "name": "/Game/Blueprints/BP_PlayerControllerBase",
    "guid": "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
    "flags": ["PKG_FilterEditorOnly"],
    "legacyFileVersion": -7,
    "usesEventDrivenLoader": true
  },

  "engineVersion": {
    "objectVersion": "VER_UE4_FIX_WIDE_STRING_CRC",
    "objectVersionUE5": "UNKNOWN",
    "customVersions": {
      "FCoreObjectVersion": 3,
      "FEditorObjectVersion": 34,
      "FFrameworkObjectVersion": 35,
      "FSequencerObjectVersion": 11,
      "FAnimPhysObjectVersion": 17,
      "FFortniteMainBranchObjectVersion": 27,
      "FReleaseObjectVersion": 23
    }
  },

  "imports": [
    {
      "index": -1,
      "objectName": "/Script/Engine",
      "className": "Package",
      "classPackage": "/Script/Engine",
      "outerIndex": 0
    },
    {
      "index": -2,
      "objectName": "Actor",
      "className": "Class",
      "classPackage": "/Script/CoreUObject",
      "outerIndex": -1
    },
    {
      "index": -3,
      "objectName": "K2_GetActorLocation",
      "className": "Function",
      "classPackage": "/Script/CoreUObject",
      "outerIndex": -2
    }
  ],

  "exports": [
    {
      "index": 1,
      "objectName": "BP_PlayerControllerBase_C",
      "className": "BlueprintGeneratedClass",
      "outerIndex": 0,
      "superIndex": -2,
      "templateIndex": -5,
      "objectFlags": ["RF_Public", "RF_Transactional"],
      "dependencies": {
        "serializationBeforeSerialization": [-2, -3],
        "createBeforeSerialization": [2, 3],
        "serializationBeforeCreate": [-4, -5],
        "createBeforeCreate": [-2]
      }
    },
    {
      "index": 2,
      "objectName": "ExecuteUbergraph_BP_PlayerControllerBase",
      "className": "Function",
      "outerIndex": 1,
      "superIndex": 0,
      "templateIndex": -10,
      "objectFlags": ["RF_Public"],
      "functionFlags": ["FUNC_UbergraphFunction", "FUNC_HasOutParms"]
    },
    {
      "index": 3,
      "objectName": "Default__BP_PlayerControllerBase_C",
      "className": "BP_PlayerControllerBase_C",
      "outerIndex": 0,
      "templateIndex": -15,
      "objectFlags": ["RF_Public", "RF_ClassDefaultObject"],
      "isCDO": true
    }
  ],

  "fieldPaths": {
    "ExecuteUbergraph_BP_PlayerControllerBase": {
      "CallFunc_IsValid_ReturnValue": {
        "path": ["CallFunc_IsValid_ReturnValue"],
        "resolvedOwner": 2
      },
      "CallFunc_K2_GetPawn_ReturnValue": {
        "path": ["CallFunc_K2_GetPawn_ReturnValue"],
        "resolvedOwner": 2
      },
      "K2Node_DynamicCast_AsBP_Enemy": {
        "path": [],
        "resolvedOwner": 0,
        "emptyPath": true
      }
    }
  },

  "cdoData": {
    "Default__BP_PlayerControllerBase_C": {
      "subObjects": [
        {
          "name": "WidgetEffects",
          "className": "FSBDWidgetEffectsComponent",
          "outerIndex": 3,
          "exportIndex": 4
        },
        {
          "name": "TransformComponent0",
          "className": "SceneComponent",
          "outerIndex": 3,
          "exportIndex": 5
        }
      ],
      "preservedProperties": [
        "SimpleConstructionScript",
        "InheritableComponentHandler",
        "UberGraphFramePointerProperty",
        "UberGraphFunction"
      ]
    }
  },

  "nameMap": [
    "None",
    "BP_PlayerControllerBase_C",
    "ExecuteUbergraph_BP_PlayerControllerBase",
    "CallFunc_IsValid_ReturnValue"
  ]
}
```

## 字段说明

### 顶级字段

| 字段 | 类型 | 必需 | 说明 |
|-----|------|------|------|
| `$schema` | string | 否 | JSON Schema URL，用于编辑器验证 |
| `version` | int | 是 | 元数据格式版本 |
| `generated` | string | 否 | 生成时间 (ISO 8601) |
| `sourceAsset` | string | 否 | 原始资产文件名（用于追溯） |

### package 对象

| 字段 | 类型 | 必需 | 说明 |
|-----|------|------|------|
| `name` | string | 是 | 包完整路径 |
| `guid` | string | 否 | PackageGuid（可新生成） |
| `flags` | string[] | 是 | EPackageFlags 枚举值 |
| `legacyFileVersion` | int | 是 | 通常为 -7 |
| `usesEventDrivenLoader` | bool | 是 | 是否使用事件驱动加载器 |

### engineVersion 对象

| 字段 | 类型 | 必需 | 说明 |
|-----|------|------|------|
| `objectVersion` | string | 是 | ObjectVersion 枚举 |
| `objectVersionUE5` | string | 是 | ObjectVersionUE5 枚举 |
| `customVersions` | object | 是 | CustomVersion 键值对 |

### imports 数组

| 字段 | 类型 | 必需 | 说明 |
|-----|------|------|------|
| `index` | int | 是 | Import 索引（负数） |
| `objectName` | string | 是 | 对象名称 |
| `className` | string | 是 | 类名 |
| `classPackage` | string | 是 | 类所在包 |
| `outerIndex` | int | 是 | 外部对象索引 |
| `bImportOptional` | bool | 否 | 是否可选导入 |

### exports 数组

| 字段 | 类型 | 必需 | 说明 |
|-----|------|------|------|
| `index` | int | 是 | Export 索引（正数） |
| `objectName` | string | 是 | 对象名称 |
| `className` | string | 是 | 类名 |
| `outerIndex` | int | 是 | 外部对象索引 |
| `superIndex` | int | 否 | 父类索引 |
| `templateIndex` | int | 否 | 模板索引 |
| `objectFlags` | string[] | 是 | EObjectFlags 枚举值 |
| `functionFlags` | string[] | 否 | EFunctionFlags（仅函数） |
| `classFlags` | string[] | 否 | EClassFlags（仅类） |
| `isCDO` | bool | 否 | 是否为 CDO |
| `dependencies` | object | 否 | 序列化依赖关系 |

### fieldPaths 对象

按函数名组织的属性路径信息：

```json
{
  "FunctionName": {
    "VariableName": {
      "path": ["segment1", "segment2"],
      "resolvedOwner": 2,
      "emptyPath": false
    }
  }
}
```

### cdoData 对象

CDO 和子对象信息：

```json
{
  "CDOName": {
    "subObjects": [...],
    "preservedProperties": [...]
  }
}
```

## 实现方案

### 1. 反编译器修改

在 `KismetScript.Decompiler` 中添加元数据生成：

```csharp
// KismetDecompiler.cs
public class KismetDecompiler
{
    public void Decompile(UAsset asset, string outputPath)
    {
        // 现有：生成 .kms 文件
        WriteKmsFile(asset, outputPath);

        // 新增：生成 .kms.meta 文件
        WriteMetaFile(asset, outputPath + ".meta");
    }

    private void WriteMetaFile(UAsset asset, string metaPath)
    {
        var metadata = new KmsMetadata
        {
            Version = 1,
            Generated = DateTime.UtcNow,
            SourceAsset = Path.GetFileName(asset.FilePath),
            Package = ExtractPackageInfo(asset),
            EngineVersion = ExtractEngineVersion(asset),
            Imports = ExtractImports(asset),
            Exports = ExtractExports(asset),
            FieldPaths = ExtractFieldPaths(asset),
            CdoData = ExtractCdoData(asset)
        };

        var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(metaPath, json);
    }
}
```

### 2. 元数据模型类

新建 `KismetScript.Utilities/Metadata/` 目录：

```csharp
// KmsMetadata.cs
namespace KismetScript.Utilities.Metadata;

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

public class PackageMetadata
{
    public string Name { get; set; } = "";
    public string? Guid { get; set; }
    public List<string> Flags { get; set; } = new();
    public int LegacyFileVersion { get; set; } = -7;
    public bool UsesEventDrivenLoader { get; set; } = true;
}

public class EngineVersionMetadata
{
    public string ObjectVersion { get; set; } = "VER_UE4_FIX_WIDE_STRING_CRC";
    public string ObjectVersionUE5 { get; set; } = "UNKNOWN";
    public Dictionary<string, int> CustomVersions { get; set; } = new();
}

public class ImportMetadata
{
    public int Index { get; set; }
    public string ObjectName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string ClassPackage { get; set; } = "";
    public int OuterIndex { get; set; }
    public bool? BImportOptional { get; set; }
}

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

public class ExportDependencies
{
    public List<int>? SerializationBeforeSerialization { get; set; }
    public List<int>? CreateBeforeSerialization { get; set; }
    public List<int>? SerializationBeforeCreate { get; set; }
    public List<int>? CreateBeforeCreate { get; set; }
}

public class FieldPathMetadata
{
    public List<string> Path { get; set; } = new();
    public int ResolvedOwner { get; set; }
    public bool? EmptyPath { get; set; }
}

public class CdoMetadata
{
    public List<SubObjectMetadata>? SubObjects { get; set; }
    public List<string>? PreservedProperties { get; set; }
}

public class SubObjectMetadata
{
    public string Name { get; set; } = "";
    public string ClassName { get; set; } = "";
    public int OuterIndex { get; set; }
    public int ExportIndex { get; set; }
}
```

### 3. Linker 修改

修改 `UAssetLinker` 支持从元数据构建：

```csharp
// UAssetLinker.cs
public class UAssetLinker : PackageLinker<UAsset>
{
    private KmsMetadata? _metadata;

    // 新增：从元数据构建
    public UAssetLinker(KmsMetadata metadata)
    {
        _metadata = metadata;
        Package = CreateAssetFromMetadata(metadata);
    }

    private UAsset CreateAssetFromMetadata(KmsMetadata metadata)
    {
        var asset = new UAsset()
        {
            LegacyFileVersion = metadata.Package.LegacyFileVersion,
            UsesEventDrivenLoader = metadata.Package.UsesEventDrivenLoader,
            PackageFlags = ParsePackageFlags(metadata.Package.Flags),
            PackageGuid = metadata.Package.Guid != null
                ? Guid.Parse(metadata.Package.Guid)
                : Guid.NewGuid(),
            ObjectVersion = Enum.Parse<ObjectVersion>(metadata.EngineVersion.ObjectVersion),
            ObjectVersionUE5 = Enum.Parse<ObjectVersionUE5>(metadata.EngineVersion.ObjectVersionUE5),
            CustomVersionContainer = BuildCustomVersions(metadata.EngineVersion.CustomVersions),
            Imports = BuildImports(metadata.Imports),
            Exports = new List<Export>(),
            // ... 其他字段
        };

        // 构建 Export 结构（先创建占位符）
        BuildExportStructure(asset, metadata.Exports);

        return asset;
    }

    // 重写属性指针修复，使用元数据中的 FieldPath
    protected override void FixPropertyPointer(ref KismetPropertyPointer pointer, PropertyPointerKind kind = PropertyPointerKind.Default)
    {
        if (_metadata != null && pointer is IntermediatePropertyPointer iProperty)
        {
            var fieldPath = FindFieldPathInMetadata(iProperty.Symbol.Name);
            if (fieldPath != null)
            {
                pointer = new KismetPropertyPointer()
                {
                    Old = new FPackageIndex(0),
                    New = new FFieldPath()
                    {
                        Path = fieldPath.Path.Select(AddName).ToArray(),
                        ResolvedOwner = new FPackageIndex(fieldPath.ResolvedOwner)
                    }
                };
                return;
            }
        }

        base.FixPropertyPointer(ref pointer, kind);
    }
}
```

### 4. CLI 命令修改

```csharp
// CompileCommand.cs
compile.SetHandler((EngineVersion ver, string? mapPath, string scriptPath, string? assetPath, string? outdir) =>
{
    var metaPath = scriptPath + ".meta";
    var hasMetadata = File.Exists(metaPath);
    var hasOriginalAsset = File.Exists(assetPath ?? Path.ChangeExtension(scriptPath, ".uasset"));

    UAssetLinker linker;

    if (hasMetadata && !hasOriginalAsset)
    {
        // 独立编译模式
        Console.WriteLine($"Using metadata file: {metaPath}");
        var metadata = JsonSerializer.Deserialize<KmsMetadata>(File.ReadAllText(metaPath));
        linker = new UAssetLinker(metadata!);
    }
    else if (hasOriginalAsset)
    {
        // 传统模式：使用原始资产
        var asset = CliHelpers.LoadAsset(ver, mapPath, assetPath);
        linker = new UAssetLinker(asset);
    }
    else
    {
        Console.WriteLine("Error: Neither metadata file nor original asset found");
        return;
    }

    var script = CliHelpers.CompileKms(scriptPath, ver);
    var newAsset = linker.LinkCompiledScript(script).Build();
    // ... 保存
});
```

## 工作流程

### 反编译流程

```
原始资产                    输出文件
    │                          │
    ▼                          ▼
┌─────────────────┐    ┌───────────────────┐
│ BP_MyClass.uasset│ ─► │ BP_MyClass.kms    │  (可读脚本)
└─────────────────┘    │ BP_MyClass.kms.meta│  (元数据)
                       └───────────────────┘
```

### 独立编译流程

```
输入文件                     输出文件
    │                          │
    ▼                          ▼
┌───────────────────┐    ┌─────────────────┐
│ BP_MyClass.kms    │ ─► │ BP_MyClass.uasset│
│ BP_MyClass.kms.meta│    │ BP_MyClass.uexp  │
└───────────────────┘    └─────────────────┘
```

### 混合模式流程（有原始资产）

```
输入文件                     输出文件
    │                          │
    ▼                          ▼
┌───────────────────┐    ┌─────────────────┐
│ BP_MyClass.kms    │    │                 │
│ BP_MyClass.uasset │ ─► │ BP_MyClass.new  │
│                   │    │   .uasset       │
└───────────────────┘    └─────────────────┘
  (元数据文件可选，
   优先使用原始资产)
```

## 优势与局限

### 优势

1. **无需修改 KMS 语法** - 保持向后兼容
2. **KMS 保持可读性** - 元数据分离存储
3. **渐进式采用** - 可选生成/使用元数据
4. **易于版本控制** - JSON 格式便于 diff
5. **实现成本较低** - 主要是序列化/反序列化

### 局限

1. **文件管理** - 需要同时管理两个文件
2. **同步问题** - KMS 修改后元数据可能过期
3. **部分信息冗余** - 某些信息在 KMS 和元数据中都存在

## 元数据验证

### 校验和机制

可在元数据中添加 KMS 文件校验和：

```json
{
  "sourceKms": {
    "sha256": "a1b2c3d4...",
    "lastModified": "2024-12-12T10:30:00Z"
  }
}
```

编译时检查：
```csharp
if (metadata.SourceKms?.Sha256 != ComputeKmsSha256(kmsPath))
{
    Console.WriteLine("Warning: KMS file has been modified since metadata was generated");
    Console.WriteLine("Consider regenerating metadata or using original asset");
}
```

## 实施计划

| 阶段 | 任务 | 工作量 |
|-----|------|-------|
| 1 | 定义元数据模型类 | 1 天 |
| 2 | 反编译器生成元数据 | 2 天 |
| 3 | Linker 读取元数据构建资产 | 3 天 |
| 4 | CLI 命令集成 | 1 天 |
| 5 | 测试和验证 | 2 天 |

**总计：约 9 个工作日**

## 参考

- [UAssetAPI CustomVersions](../UAssetAPI/CustomVersions/)
- [Unity .meta 文件设计](https://docs.unity3d.com/Manual/AssetMetadata.html)
- [JSON Schema](https://json-schema.org/)
