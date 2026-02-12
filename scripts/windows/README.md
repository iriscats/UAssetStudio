# UAssetStudio Windows 生产环境脚本

这些脚本用于直接调用已编译好的 `UAssetStudio.Cli.exe`，无需 .NET SDK。

**支持拖入文件**：直接将文件拖到 `.bat` 文件上即可运行

无需安装 .NET Runtime，直接运行即可。

## 脚本说明

### compile.bat - 编译 KMS 到 uasset

**拖入文件**: 将 `.kms` 文件拖到 `compile.bat` 上

```bat
compile.bat --kms "C:\input.kms" --asset "C:\original.uasset"
compile.bat --kms "C:\input.kms" --out "C:\output.uasset"
compile.bat --kms "C:\input.kms" --asset "C:\orig.uasset" --usmap "C:\mappings.usmap"
```

参数：
- `--kms <path>` - **必需**，输入 .kms 脚本路径
- `--asset <path>` - 原始 .uasset 文件路径（作为模板）
- `--out <path>` - 输出文件路径（覆盖默认输出名）
- `--usmap <path>` - .usmap 映射文件路径
- `--ue-version <ver>` - UE 版本（默认：VER_UE4_27）

### decompile.bat - 反编译 uasset 到 KMS

**拖入文件**: 将 `.uasset` 或 `.umap` 文件拖到 `decompile.bat` 上

```bat
decompile.bat --asset "C:\Game\Weapon.uasset"
decompile.bat --asset "C:\Game\Weapon.uasset" --usmap "C:\mappings.usmap"
decompile.bat --asset "C:\Game\Weapon.uasset" --outdir "C:\Output" --meta
```

参数：
- `--asset <path>` - **必需**，输入 .uasset/.umap 文件路径
- `--outdir <path>` - 输出目录（默认：资源所在目录）
- `--usmap <path>` - .usmap 映射文件路径
- `--ue-version <ver>` - UE 版本（默认：VER_UE4_27）
- `--meta` - 生成 .kms.meta 元数据文件（用于独立编译）

### verify.bat - 验证完整流程

**拖入文件**: 将 `.uasset` 或 `.umap` 文件拖到 `verify.bat` 上

```bat
verify.bat --asset "C:\Game\Weapon.uasset"
verify.bat --asset "C:\Game\Weapon.uasset" --usmap "C:\mappings.usmap"
verify.bat --asset "C:\Game\Weapon.uasset" --outdir "C:\Output" --meta
```

参数：
- `--asset <path>` - **必需**，输入 .uasset/.umap 文件路径
- `--outdir <path>` - 输出目录
- `--usmap <path>` - .usmap 映射文件路径
- `--ue-version <ver>` - UE 版本（默认：VER_UE4_27）
- `--meta` - 测试独立编译模式

流程：反编译 → 编译 → 链接 → 写入 → 对比验证

### cfg.bat - 生成控制流图

**拖入文件**: 将 `.uasset` 或 `.umap` 文件拖到 `cfg.bat` 上

```bat
cfg.bat --asset "C:\Game\Weapon.uasset"
cfg.bat --asset "C:\Game\Weapon.uasset" --usmap "C:\mappings.usmap"
cfg.bat --asset "C:\Game\Weapon.uasset" --outdir "C:\Output"
```

参数：
- `--asset <path>` - **必需**，输入 .uasset/.umap 文件路径
- `--outdir <path>` - 输出目录
- `--usmap <path>` - .usmap 映射文件路径
- `--ue-version <ver>` - UE 版本（默认：VER_UE4_27）

输出文件：
- `<asset_name>.dot` - Graphviz DOT 文件
- `<asset_name>.txt` - 文本摘要

转换为 PNG（需要安装 Graphviz）：
```bat
dot -Tpng "output.dot" -o "output.png"
```

## 支持的 UE 版本

- `VER_UE4_27` - Unreal Engine 4.27（默认）
- `VER_UE5_6` - Unreal Engine 5.6

## 使用方式

### 方式 1: 拖入文件（推荐）

直接将文件拖到对应的 `.bat` 脚本上：

| 操作 | 拖入文件类型 | 目标脚本 |
|------|-------------|----------|
| 反编译 | `.uasset` 或 `.umap` | `decompile.bat` |
| 编译 | `.kms` | `compile.bat` |
| 验证 | `.uasset` 或 `.umap` | `verify.bat` |
| 生成 CFG | `.uasset` 或 `.umap` | `cfg.bat` |

### 方式 2: 命令行参数

```bat
:: 反编译资源
decompile.bat --asset "C:\Game\Weapon.uasset" --outdir "C:\Mods" --meta

:: 编辑生成的 C:\Mods\Weapon.kms 文件

:: 编译回 uasset（使用 metadata 独立编译）
compile.bat --kms "C:\Mods\Weapon.kms" --out "C:\Mods\Weapon_Modded.uasset"

:: 或者使用原始资源作为模板编译
compile.bat --kms "C:\Mods\Weapon.kms" --asset "C:\Game\Weapon.uasset" --out "C:\Mods\Weapon_Modded.uasset"

:: 验证资源完整性
verify.bat --asset "C:\Game\Weapon.uasset" --meta
```
