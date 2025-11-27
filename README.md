# UAssetStudio

**重点：UAssetStudio.Cli 命令行用法**

UAssetStudio.Cli 提供生成单个 UE 资产的控制流图（CFG）与摘要的命令行工具。

**环境要求**
- `dotnet SDK 9.0+`

**基础用法**
- 命令格式：`UAssetStudio.Cli.exe <command> [options]`
- 全局选项：
  - `--ue-version <ver>`：UE 版本（默认 `VER_UE4_27`，可设置为 `VER_UE5_6` 等）
  - `--mappings <path>`：`.usmap` 文件路径（解析 unversioned properties 时需要）

**子命令**
- `cfg <asset> [--outdir <dir>]`
  - 生成控制流图 `.dot` 与摘要 `.txt`
  - 示例：
    - `UAssetStudio.Cli.exe -- cfg /Users/iris/Project/RogueCore/Content/WeaponsNTools/ZipLineGun/WPN_ZipLineGun.uasset --mappings /Users/iris/Project/RogueCore/DRG_RC_Mappings.usmap --ue-version VER_UE5_6 --outdir /Users/iris/Project/UAssetStudio/script/output`
    - 生成文件：
      - `/Users/iris/Project/UAssetStudio/script/output/WPN_ZipLineGun.dot`
      - `/Users/iris/Project/UAssetStudio/script/output/WPN_ZipLineGun.txt`

- `decompile <asset> [--outdir <dir>]`
  - 反编译 `.uasset/.umap` 为 `.kms`
  - 示例：
    - `UAssetStudio.Cli.exe -- decompile /Users/iris/Project/RogueCore/Content/WeaponsNTools/ZipLineGun/WPN_ZipLineGun.uasset --mappings /Users/iris/Project/RogueCore/DRG_RC_Mappings.usmap --ue-version VER_UE5_6 --outdir /Users/iris/Project/UAssetStudio/script/output`
    - 生成文件：
      - `/Users/iris/Project/UAssetStudio/script/output/WPN_ZipLineGun.kms`

- `compile <script.kms> [--asset <original.uasset>] [--outdir <dir>]`
  - 将 `.kms` 编译并链接到资产，生成 `.compiled.uasset/.compiled.uexp`
  - 若未指定 `--asset`，默认使用 `.kms` 同目录同名 `.uasset`
  - 示例：
    - `UAssetStudio.Cli.exe -- compile /Users/iris/Project/UAssetStudio/script/output/WPN_ZipLineGun.kms --asset /Users/iris/Project/RogueCore/Content/WeaponsNTools/ZipLineGun/WPN_ZipLineGun.uasset --mappings /Users/iris/Project/RogueCore/DRG_RC_Mappings.usmap --ue-version VER_UE5_6 --outdir /Users/iris/Project/UAssetStudio/script/output`
    - 生成文件：
      - `/Users/iris/Project/UAssetStudio/script/output/WPN_ZipLineGun.compiled.uasset`
      - `/Users/iris/Project/UAssetStudio/script/output/WPN_ZipLineGun.compiled.uexp`

- `verify <asset> [--outdir <dir>]`
  - 验证编译流程：反编译为 `.kms` → 重新编译 → 链接并输出 `.new.uasset/.new.uexp`
  - 示例：
    - `UAssetStudio.Cli.exe -- verify /Users/iris/Project/RogueCore/Content/WeaponsNTools/ZipLineGun/WPN_ZipLineGun.uasset --mappings /Users/iris/Project/RogueCore/DRG_RC_Mappings.usmap --ue-version VER_UE5_6 --outdir /Users/iris/Project/UAssetStudio/script/output`
    - 生成文件：
      - `/Users/iris/Project/UAssetStudio/script/output/WPN_ZipLineGun.new.uasset`
      - `/Users/iris/Project/UAssetStudio/script/output/WPN_ZipLineGun.new.uexp`


