#!/usr/bin/env bash

set -euo pipefail

# Paths (adjust if your environment differs)
ASSET_PATH="/Users/bytedance/Project/RogueCore/Content/WeaponsNTools/ZipLineGun/WPN_ZipLineGun.uasset"
USMAP_PATH="/Users/bytedance/Project/RogueCore/DRG_RC_Mappings.usmap"
REPO_ROOT="/Users/bytedance/Project/UAssetStudio"
CLI_PROJ="$REPO_ROOT/UAssetStudio.Cli/UAssetStudio.Cli.csproj"
OUTDIR="$REPO_ROOT/script/output"

echo "[Info] Output dir: $OUTDIR"
mkdir -p "$OUTDIR"

# Basic checks
if [[ ! -f "$ASSET_PATH" ]]; then
  echo "[Error] Asset not found: $ASSET_PATH" >&2
  exit 1
fi
if [[ ! -f "$USMAP_PATH" ]]; then
  echo "[Error] USMAP not found: $USMAP_PATH" >&2
  exit 1
fi

echo "[Info] Building CLI..."
dotnet build "$CLI_PROJ" -v minimal

FILENAME=$(basename "$ASSET_PATH")
KMS_FILE="$OUTDIR/${FILENAME%.*}.kms"
COMPILED_FILE="$OUTDIR/${FILENAME%.*}.compiled.uasset"

# Prepare .kms by decompiling if missing
if [[ ! -f "$KMS_FILE" ]]; then
  echo "[Info] KMS not found. Running decompile to prepare..."
  dotnet run --project "$CLI_PROJ" -- decompile "$ASSET_PATH" --mappings "$USMAP_PATH" --ue-version VER_UE5_6 --outdir "$OUTDIR"
fi

if [[ ! -f "$KMS_FILE" ]]; then
  echo "[Error] KMS still missing after decompile: $KMS_FILE" >&2
  exit 2
fi

echo "[Info] Running compile (UE5.6)..."
dotnet run --project "$CLI_PROJ" -- compile "$KMS_FILE" --asset "$ASSET_PATH" --mappings "$USMAP_PATH" --ue-version VER_UE5_6 --outdir "$OUTDIR"

if [[ -f "$COMPILED_FILE" ]]; then
  echo "[Success] Compiled asset generated: $COMPILED_FILE"
else
  echo "[Warn] Compiled asset not found: $COMPILED_FILE" >&2
  exit 3
fi

echo "[Done] Compile test complete."

