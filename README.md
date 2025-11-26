# UAssetStudio

**重点：UAssetStudio.Cli 命令行用法**

UAssetStudio.Cli 提供生成单个 UE 资产的控制流图（CFG）与摘要的命令行工具。

**环境要求**
- `dotnet SDK 9.0+`

**基础用法**
- 命令格式：`UAssetStudio.Cli.exe <input> [options]`
- 参数与选项：
  - `<input>`：资产路径（`.uasset` 或 `.umap`）
  - `--outdir <dir>`：输出目录（默认使用资产所在目录）
  - `--ue-version <ver>`：UE 版本（默认 `VER_UE4_27`，可设置为 `VER_UE5_6` 等）
  - `--mappings <path>`：`.usmap` 文件路径（解析 unversioned properties 时需要）

**示例：生成 CFG `.dot` 与摘要 `.txt`（UE5.6 + .usmap）**
- `UAssetStudio.Cli.exe -- /Users/iris/Project/RogueCore/Content/WeaponsNTools/ZipLineGun/WPN_ZipLineGun.uasset --mappings /Users/iris/Project/RogueCore/DRG_RC_Mappings.usmap --ue-version VER_UE5_6 --outdir /Users/iris/Project/UAssetStudio/script/output`
- 生成文件：
  - `/Users/iris/Project/UAssetStudio/script/output/WPN_ZipLineGun.dot`
  - `/Users/iris/Project/UAssetStudio/script/output/WPN_ZipLineGun.txt`



