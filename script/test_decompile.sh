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

echo "[Info] Running decompile (UE5.6)..."
dotnet run --project "$CLI_PROJ" -- decompile "$ASSET_PATH" --mappings "$USMAP_PATH" --ue-version VER_UE5_6 --outdir "$OUTDIR"

FILENAME=$(basename "$ASSET_PATH")
KMS_FILE="$OUTDIR/${FILENAME%.*}.kms"

if [[ -f "$KMS_FILE" ]]; then
  echo "[Success] KMS generated: $KMS_FILE"
else
  echo "[Warn] KMS not found: $KMS_FILE" >&2
  exit 2
fi

echo "[Done] Decompile test complete."

