# UAssetStudio

UAssetStudio 是一个全面的 .NET 9.0 工具链，用于分析、反编译、编译和验证 Unreal Engine 游戏资产（.uasset 文件）。该项目专门用于蓝图（Kismet）可视化脚本分析，支持 UE 4.13-5.6+ 版本。

## 核心功能

1. **控制流图（CFG）生成** - 从 UE 资产创建 `.dot` 文件和摘要
2. **反编译** - 将 `.uasset/.umap` 文件转换为可读的 `.kms`（Kismet Script）格式
3. **编译** - 将 `.kms` 脚本重新编译为 `.uasset/.uexp` 文件
4. **验证** - 验证完整的往返流程：反编译 → 编译 → 链接 → 写入
5. **独立编译** - 使用元数据编译 `.kms`（无需原始资产）

## 解决方案架构

```
UAssetStudio.sln
├── UAssetStudio.Cli/                    # 主 CLI 入口点 (.NET 9.0)
├── UAssetAPI/                           # 核心资产读写库 (git submodule)
│   ├── UAssetAPI/                       # 主库
│   ├── UAssetAPI.Tests/                 # 单元测试
│   └── UAssetAPI.Benchmark/             # 性能测试
├── KismetScript.Compiler/               # 基于 ANTLR 的 Kismet 脚本编译器
├── KismetScript.Decompiler/             # 资产反编译逻辑
├── KismetScript.Linker/                 # 资产链接和编译
├── KismetScript.Parser/                 # Kismet 脚本解析器
├── KismetScript.Syntax/                 # AST 和语法树定义
├── KismetScript.Utilities/              # 共享工具类
├── KismetAnalyzer.CFG/                  # 控制流图生成
├── AssetRegistry.Serializer/            # 资产注册表序列化
└── UAsset.Localization/                 # 本地化支持
```

## 环境要求

- **.NET SDK 9.0+**
- `.usmap` 映射文件（UE5+ 需要，用于解析 unversioned properties）

## 构建项目

```bash
# 构建整个解决方案
dotnet build

# 构建特定配置
dotnet build --configuration Release
dotnet build --configuration DebugTracing  # 启用 PostSharp 跟踪

# 运行所有测试
dotnet test
dotnet test --verbosity normal

# 运行特定测试项目
dotnet test UAssetAPI/UAssetAPI.Tests/UAssetAPI.Tests.csproj
```

## CLI 用法

### 全局选项

```
--ue-version <ver>    # UE 版本（默认 VER_UE4_27，支持 VER_UE5_6 等）
--mappings <path>     # .usmap 文件路径（UE5+ 需要，UE4 可选）
```

### 子命令

#### 1. cfg - 生成控制流图

生成 `.dot` 控制流图和摘要 `.txt` 文件。

```bash
dotnet run --project UAssetStudio.Cli -- cfg <asset> \
  --mappings <usmap> \
  --ue-version VER_UE5_6 \
  --outdir <dir>
```

**示例：**
```bash
dotnet run --project UAssetStudio.Cli -- cfg \
  /path/to/WPN_ZipLineGun.uasset \
  --mappings /path/to/DRG_RC_Mappings.usmap \
  --ue-version VER_UE5_6 \
  --outdir ./output
```

**输出文件：**
- `WPN_ZipLineGun.dot` - Graphviz 格式控制流图
- `WPN_ZipLineGun.txt` - 文本摘要

#### 2. decompile - 反编译资产

将 `.uasset/.umap` 反编译为 `.kms` 脚本。

```bash
dotnet run --project UAssetStudio.Cli -- decompile <asset> \
  --mappings <usmap> \
  --ue-version VER_UE5_6 \
  --outdir <dir> \
  [--meta]  # 生成元数据文件用于独立编译
```

**示例：**
```bash
dotnet run --project UAssetStudio.Cli -- decompile \
  /path/to/WPN_ZipLineGun.uasset \
  --mappings /path/to/DRG_RC_Mappings.usmap \
  --ue-version VER_UE5_6 \
  --outdir ./output \
  --meta
```

**输出文件：**
- `WPN_ZipLineGun.kms` - Kismet 脚本
- `WPN_ZipLineGun.kms.meta` - 元数据（使用 `--meta` 时）

#### 3. compile - 编译 KMS 脚本

将 `.kms` 脚本编译回 `.uasset/.uexp`。

```bash
dotnet run --project UAssetStudio.Cli -- compile <script.kms> \
  --asset <original.uasset> \
  --mappings <usmap> \
  --ue-version VER_UE5_6 \
  --outdir <dir>
```

**示例：**
```bash
dotnet run --project UAssetStudio.Cli -- compile \
  ./output/WPN_ZipLineGun.kms \
  --asset /path/to/WPN_ZipLineGun.uasset \
  --mappings /path/to/DRG_RC_Mappings.usmap \
  --ue-version VER_UE5_6 \
  --outdir ./output
```

**输出文件：**
- `WPN_ZipLineGun.compiled.uasset`
- `WPN_ZipLineGun.compiled.uexp`

#### 4. verify - 验证完整流程

验证往返流程：反编译 → 编译 → 链接 → 输出。

```bash
dotnet run --project UAssetStudio.Cli -- verify <asset> \
  --mappings <usmap> \
  --ue-version VER_UE5_6 \
  --outdir <dir> \
  [--meta]  # 测试独立编译路径
```

**示例：**
```bash
dotnet run --project UAssetStudio.Cli -- verify \
  /path/to/WPN_ZipLineGun.uasset \
  --mappings /path/to/DRG_RC_Mappings.usmap \
  --ue-version VER_UE5_6 \
  --outdir ./output
```

**输出文件：**
- `WPN_ZipLineGun.new.uasset`
- `WPN_ZipLineGun.new.uexp`

## 测试脚本

```bash
# 运行验证测试
./script/test_verify_5.6.sh   # UE5.6 测试（需要 .usmap）
./script/test_verify_4.27.sh  # UE4.27 测试（不需要 .usmap）

# 其他测试脚本
./script/test_compile.sh
./script/test_decompile.sh
./script/test_gen_dot.sh
```

## 重要说明

### 二进制等价性

本项目要求与原始资产保持**二进制等价**——这对游戏模组制作至关重要。修改序列化时请务必使用 `VerifyBinaryEquality()` 方法进行验证。

### 映射文件 (.usmap)

- UE5+ 版本解析 unversioned properties 时需要 `.usmap` 文件
- UE4 版本（如 VER_UE4_27）通常可以无需映射文件工作
- 通过 `--mappings` 参数指定映射文件路径

### 独立编译

- 反编译时添加 `--meta` 标志可生成 `.kms.meta` JSON 元数据文件
- 元数据文件捕获编译所需的资产上下文信息
- 无需原始资产即可编译，便于模组分发（只需 `.kms` + `.kms.meta`）

### 支持的 UE 版本

- **主要支持**: UE 4.27, UE 5.6
- **版本范围**: UE 4.13 - 5.6+
- 通过 `--ue-version` 标志指定版本（如 `VER_UE4_27`, `VER_UE5_6`）

## 关键入口点

- **UAssetAPI/UAssetAPI/UAsset.cs** - 主资产加载/保存 API
- **UAssetAPI/UAssetAPI/MainSerializer.cs** - 序列化引擎和属性注册表
- **UAssetStudio.Cli/Program.cs** - CLI 命令解析和编排
- **KismetScript.Decompiler/KismetDecompiler.cs** - 反编译器入口
- **KismetScript.Linker/UAssetLinker.cs** - 资产链接和最终编译
