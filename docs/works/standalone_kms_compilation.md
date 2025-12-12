# 独立 KMS 编译方案

## 背景

当前 `KismetScript.Linker` 必须依赖原始 `.uasset` 文件才能完成编译。本文档分析了脱离原始资产、直接从 `.kms` 脚本生成 `.uasset` 的可行性和改造方案。

## 现状分析

### Linker 对原始资产的依赖

| 依赖类型 | 说明 | 当前来源 |
|---------|------|---------|
| `CustomVersionContainer` | 引擎版本信息，决定序列化格式 | 原始资产 |
| `Import` 列表 | 外部包/类/函数的引用结构 | 原始资产 |
| `Export` 层次结构 | 导出对象的父子关系和依赖 | 原始资产 |
| CDO (Class Default Object) | 类默认对象及属性值 | 原始资产 |
| `FFieldPath.ResolvedOwner` | 属性指针的所有者信息 | 原始资产字节码 |
| Sub-object exports | 子对象导出结构 | 原始资产 |
| `DependsMap` | 依赖映射 | 原始资产 |
| `SoftPackageReferenceList` | 软引用列表 | 原始资产 |
| `PackageGuid` | 包唯一标识 | 原始资产 |

### 关键代码依赖点

```csharp
// 1. 版本检测 - 决定使用 FProperty (UE5+) 还是 UProperty (UE4)
protected bool SerializeLoadedProperties
    => Package.GetCustomVersion<FCoreObjectVersion>() >= FCoreObjectVersion.FProperties;

// 2. 查找现有 Export 进行修改
var functionExport = FindChildExport<FunctionExport>(classExport, functionContext.Symbol.Name);

// 3. 保留原始字节码中的属性所有者信息
PreserveOriginalPropertyOwners(functionExport.ScriptBytecode);

// 4. 符号解析依赖现有 Import/Export 列表
TryFindPackageIndexInAsset(symbol, out var packageIndex);
```

### KMS 当前输出内容

反编译器 (`KismetScript.Decompiler`) 目前输出：

- `[Import("PackagePath")]` - 包导入声明
- 类定义和继承关系
- 函数签名和实现
- 属性声明和初始化器
- 控制流 (if/while/goto)

**不包含的关键信息：**
- 引擎版本和 CustomVersion
- Import 的完整结构 (ClassName, OuterIndex)
- Export 依赖关系
- FFieldPath 的 ResolvedOwner
- CDO/Sub-object 完整结构

## 改造方案

### Phase 1: 扩展 KMS 语法

#### 1.1 添加 Pragma 指令

```kms
// 引擎版本
#pragma engine_version VER_UE5_6
#pragma object_version VER_UE4_FIX_WIDE_STRING_CRC
#pragma object_version_ue5 UNKNOWN

// CustomVersion 信息
#pragma custom_version FCoreObjectVersion = 3
#pragma custom_version FEditorObjectVersion = 34
#pragma custom_version FFrameworkObjectVersion = 35
#pragma custom_version FSequencerObjectVersion = 11

// 包信息
#pragma package_flags PKG_FilterEditorOnly
#pragma uses_event_driven_loader true
```

#### 1.2 扩展 Import 属性

```kms
// 当前格式
[Import("/Script/Engine")]
public class Actor { }

// 扩展格式
[Import("/Script/Engine", ClassName = "Package", OuterIndex = null)]
[Import("/Script/Engine.Actor", ClassName = "Class", OuterIndex = "/Script/Engine")]
public class Actor {
    [Import("/Script/Engine.Actor.K2_GetActorLocation", ClassName = "Function")]
    public virtual void K2_GetActorLocation();
}
```

#### 1.3 添加 Export 属性

```kms
[Export(
    OuterIndex = null,
    ClassIndex = "BlueprintGeneratedClass",
    SuperIndex = "Actor",
    TemplateIndex = "Default__BlueprintGeneratedClass"
)]
[ExportDependencies(
    SerializationBeforeSerializationDependencies = ["Actor", "Default__Actor"],
    CreateBeforeCreateDependencies = ["Actor"]
)]
class BP_MyClass_C : Actor {
    // ...
}
```

#### 1.4 添加 FieldPath 属性

```kms
// 函数局部变量
sealed void ExecuteUbergraph(int EntryPoint) {
    [FieldPath(ResolvedOwner = "BP_MyClass_C")]
    bool CallFunc_IsValid_ReturnValue;

    [FieldPath(ResolvedOwner = null, EmptyPath = true)]
    Object K2Node_DynamicCast_AsBP_MyClass;
}
```

#### 1.5 完整的 CDO/Sub-object 声明

```kms
[CDO(OuterIndex = "BP_MyClass_C")]
object Default__BP_MyClass_C : BP_MyClass_C {
    ObjectProperty SimpleConstructionScript = @SimpleConstructionScript_0;
    ObjectProperty UberGraphFunction = @ExecuteUbergraph_BP_MyClass;

    [SubObject(ClassName = "SceneComponent", OuterIndex = self)]
    object TransformComponent0 : SceneComponent {
        // 属性值
    }
}
```

### Phase 2: 修改反编译器

修改 `KismetScript.Decompiler` 输出上述扩展语法：

| 任务 | 文件 | 说明 |
|-----|------|------|
| 输出 pragma 指令 | `KismetDecompiler.cs` | 在文件头部写入版本信息 |
| 扩展 Import 输出 | `WriteImports()` | 输出完整的 Import 结构 |
| 添加 Export 属性 | `WriteClass()`, `WriteFunction()` | 输出依赖关系 |
| 输出 FieldPath | `WriteFunction()` | 在变量声明中添加 ResolvedOwner |
| 完整 CDO 输出 | `WriteDataExport()` | 输出 Sub-object 结构 |

### Phase 3: 扩展编译器/解析器

修改 `KismetScript.Compiler` 和 `KismetScript.Parser`：

1. 解析新增的 pragma 指令
2. 解析扩展的 `[Import]` 属性
3. 解析 `[Export]` 和 `[ExportDependencies]` 属性
4. 解析 `[FieldPath]` 属性
5. 解析 `[CDO]` 和 `[SubObject]` 属性

### Phase 4: 重构 Linker

修改 `KismetScript.Linker` 支持独立构建：

```csharp
public class UAssetLinker : PackageLinker<UAsset>
{
    // 新增：从编译上下文构建完整资产
    public UAssetLinker(CompiledScriptContext context)
    {
        Package = CreateAssetFromContext(context);
    }

    private UAsset CreateAssetFromContext(CompiledScriptContext context)
    {
        var asset = CreateDefaultAsset();

        // 1. 应用 pragma 指令中的版本信息
        ApplyPragmaDirectives(asset, context.Pragmas);

        // 2. 构建 Import 列表
        BuildImportList(asset, context.Imports);

        // 3. 构建 Export 列表（按依赖顺序）
        BuildExportList(asset, context.Classes, context.Functions);

        // 4. 构建 CDO 和 Sub-objects
        BuildCDOExports(asset, context.CDOs);

        return asset;
    }
}
```

## 实施优先级

### 高优先级（必须实现）

1. **Pragma 指令** - 版本信息是决定序列化格式的关键
2. **完整 Import 结构** - 符号解析的基础
3. **Export 依赖关系** - 确保正确的序列化顺序
4. **FieldPath ResolvedOwner** - 字节码正确性的关键

### 中优先级

5. **CDO 完整结构** - 支持类属性初始化
6. **Sub-object 声明** - 支持组件结构

### 低优先级（可后续实现）

7. **DependsMap 重建** - 可从 Import/Export 推断
8. **AssetRegistryData** - 可设置为空
9. **PackageGuid** - 可生成新的

## 兼容性考虑

### 向后兼容

- 新语法使用 `#pragma` 和扩展属性，不破坏现有 KMS 解析
- 不含新语法的 KMS 仍需配合原始资产使用
- 编译器检测到缺少必要信息时，提示需要原始资产

### 验证策略

```bash
# 完整往返测试
UAssetStudio.Cli verify <asset> --mappings <usmap> --ue-version VER_UE5_6

# 独立编译测试（新功能）
UAssetStudio.Cli compile <script.kms> --standalone --ue-version VER_UE5_6
```

## 工作量估计

| 阶段 | 涉及模块 | 复杂度 |
|-----|---------|-------|
| Phase 1: KMS 语法扩展 | Syntax, Parser | 中 |
| Phase 2: 反编译器修改 | Decompiler | 中 |
| Phase 3: 编译器扩展 | Compiler, Parser | 高 |
| Phase 4: Linker 重构 | Linker | 高 |

## 替代方案

如果完整实现成本过高，可考虑：

1. **最小元数据文件** - 生成 `.kms.meta` JSON 文件存储元数据
2. **模板资产** - 使用空白模板资产 + KMS 脚本合并
3. **二进制差异** - 存储与标准模板的差异

## 参考文件

- `KismetScript.Linker/PackageLinkerBase.cs` - Linker 核心逻辑
- `KismetScript.Linker/UAssetLinker.cs` - UAsset 特定实现
- `KismetScript.Decompiler/KismetDecompiler.cs` - 反编译器主逻辑
- `KismetScript.Syntax/` - AST 定义
