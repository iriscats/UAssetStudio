#!/usr/bin/env bash

set -euo pipefail

# Paths (adjust if your environment differs)
ASSET_PATH="/Users/bytedance/Project/UAssetStudio/script/test_case_4_27/BP_PlayerControllerBase.uasset"
USMAP_PATH=""  # Optional: set to a valid UE4.27 .usmap if needed
REPO_ROOT="/Users/bytedance/Project/UAssetStudio"
CLI_PROJ="$REPO_ROOT/UAssetStudio.Cli/UAssetStudio.Cli.csproj"
OUTDIR="$REPO_ROOT/script/output"

# UE Version
UE_VERSION="VER_UE4_27"

echo "[Info] Output dir: $OUTDIR"
mkdir -p "$OUTDIR"

# Basic checks
if [[ ! -f "$ASSET_PATH" ]]; then
  echo "[Error] Asset not found: $ASSET_PATH" >&2
  exit 1
fi

echo "[Info] Building CLI..."
dotnet build "$CLI_PROJ" -v minimal

echo "[Info] Running decompile (UE4.27)..."

# Run decompile with optional mappings
if [[ -n "$USMAP_PATH" ]] && [[ -f "$USMAP_PATH" ]]; then
  echo "[Info] Using mappings: $USMAP_PATH"
  dotnet run --project "$CLI_PROJ" -- decompile "$ASSET_PATH" --ue-version "$UE_VERSION" --mappings "$USMAP_PATH" --outdir "$OUTDIR"
else
  [[ -n "$USMAP_PATH" ]] && echo "[Warn] USMAP not found: $USMAP_PATH (continue without mappings)"
  dotnet run --project "$CLI_PROJ" -- decompile "$ASSET_PATH" --ue-version "$UE_VERSION" --outdir "$OUTDIR"
fi

FILENAME=$(basename "$ASSET_PATH")
KMS_FILE="$OUTDIR/${FILENAME%.*}.kms"

if [[ -f "$KMS_FILE" ]]; then
  echo "[Success] KMS generated: $KMS_FILE"
else
  echo "[Warn] KMS not found: $KMS_FILE" >&2
  exit 2
fi

echo "[Done] Decompile test complete."
