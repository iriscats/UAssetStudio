#!/usr/bin/env bash

set -euo pipefail

# Paths (adjust if your environment differs)
REPO_ROOT="/Users/bytedance/Project/UAssetStudio"
CLI_PROJ="$REPO_ROOT/UAssetStudio.Cli/UAssetStudio.Cli.csproj"
OUTDIR="$REPO_ROOT/script/output"

# Explicit KMS/script and original asset
KMS_FILE="/Users/bytedance/Project/UAssetStudio/script/output/BP_PlayerControllerBase.kms"
ASSET_PATH="/Users/bytedance/Project/UAssetStudio/script/output/BP_PlayerControllerBase.uasset"

# Optional: set to a valid UE5.6 .usmap if needed
USMAP_PATH="/Users/bytedance/Project/RogueCore/DRG_RC_Mappings.usmap"

echo "[Info] Output dir: $OUTDIR"
mkdir -p "$OUTDIR"

# Basic checks
if [[ ! -f "$ASSET_PATH" ]]; then
  echo "[Error] Asset not found: $ASSET_PATH" >&2
  exit 1
fi
if [[ ! -f "$KMS_FILE" ]]; then
  echo "[Error] KMS script not found: $KMS_FILE" >&2
  exit 1
fi

echo "[Info] Building CLI..."
dotnet build "$CLI_PROJ" -v minimal

COMPILED_FILE="$OUTDIR/$(basename -s .kms "$KMS_FILE").compiled.uasset"


echo "[Info] Running compile (UE5.6)..."

# Run compile with optional mappings
if [[ -n "$USMAP_PATH" ]] && [[ -f "$USMAP_PATH" ]]; then
  echo "[Info] Using mappings: $USMAP_PATH"
  dotnet run --project "$CLI_PROJ" -- compile "$KMS_FILE" --asset "$ASSET_PATH" --ue-version VER_UE5_6 --mappings "$USMAP_PATH" --outdir "$OUTDIR"
else
  [[ -n "$USMAP_PATH" ]] && echo "[Warn] USMAP not found: $USMAP_PATH (continue without mappings)"
  dotnet run --project "$CLI_PROJ" -- compile "$KMS_FILE" --asset "$ASSET_PATH" --ue-version VER_UE5_6 --outdir "$OUTDIR"
fi

if [[ -f "$COMPILED_FILE" ]]; then
  echo "[Success] Compiled asset generated: $COMPILED_FILE"
else
  echo "[Warn] Compiled asset not found: $COMPILED_FILE" >&2
  exit 3
fi

echo "[Done] Compile test complete."
