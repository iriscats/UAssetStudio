#!/usr/bin/env bash

set -euo pipefail

# Paths (adjust if your environment differs)
ASSET_PATH="/Users/bytedance/Project/UAssetStudio/script/test_case_5_6/BP_PlayerControllerBase.uasset"
USMAP_PATH="/Users/bytedance/Project/RogueCore/DRG_RC_Mappings.usmap"
REPO_ROOT="/Users/bytedance/Project/UAssetStudio"
CLI_PROJ="$REPO_ROOT/UAssetStudio.Cli/UAssetStudio.Cli.csproj"
OUTDIR="$REPO_ROOT/script/output"

# UE Version
UE_VERSION="VER_UE5_6"

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
NEW_FILE="$OUTDIR/${FILENAME%.*}.new.uasset"

echo "[Info] Running verify (UE5.6): decompile -> compile -> link -> write"
dotnet run --project "$CLI_PROJ" -- verify "$ASSET_PATH" --mappings "$USMAP_PATH" --ue-version "$UE_VERSION" --outdir "$OUTDIR"

if [[ -f "$KMS_FILE" ]]; then
  echo "[Success] KMS generated: $KMS_FILE"
else
  echo "[Warn] KMS not found: $KMS_FILE" >&2
  exit 2
fi

if [[ -f "$NEW_FILE" ]]; then
  echo "[Success] New asset generated: $NEW_FILE"
else
  echo "[Warn] New asset not found: $NEW_FILE" >&2
  exit 3
fi

echo "[Done] Verify test complete."

