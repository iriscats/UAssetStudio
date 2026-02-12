#!/usr/bin/env bash

set -euo pipefail

# Paths
ASSET_PATH="/Users/bytedance/Project/RogueCore/Content/WeaponsNTools/ZipLineGun/WPN_ZipLineGun.uasset"
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

echo "[Info] Running CLI to generate CFG/DOT (UE5.6)..."
dotnet run --project "$CLI_PROJ" -- "$ASSET_PATH" --mappings "$USMAP_PATH" --ue-version "$UE_VERSION" --outdir "$OUTDIR"

FILENAME=$(basename "$ASSET_PATH")
DOT_FILE="$OUTDIR/${FILENAME%.*}.dot"
TXT_FILE="$OUTDIR/${FILENAME%.*}.txt"

if [[ -f "$DOT_FILE" ]]; then
  echo "[Success] DOT generated: $DOT_FILE"
else
  echo "[Warn] DOT not found: $DOT_FILE" >&2
  exit 2
fi

if [[ -f "$TXT_FILE" ]]; then
  echo "[Info] Summary generated: $TXT_FILE"
fi

echo "[Done] Generation complete."

