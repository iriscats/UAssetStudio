#!/usr/bin/env bash

set -euo pipefail

# Paths (adjust if your environment differs)
REPO_ROOT="/Users/bytedance/Project/UAssetStudio"
CLI_PROJ="$REPO_ROOT/UAssetStudio.Cli/UAssetStudio.Cli.csproj"
OUTDIR="$REPO_ROOT/script/output"

# Source asset (UE4.27)
ASSET_PATH="$REPO_ROOT/script/test_case_4_27/OC_PGL_Ammo_C.uasset"

# Optional: provide a UE4.27 mappings file if required
USMAP_PATH=""  # Optional: set to a valid UE4.27 .usmap if needed

echo "[Info] Output dir: $OUTDIR"
mkdir -p "$OUTDIR"

# Basic checks
if [[ ! -f "$ASSET_PATH" ]]; then
  echo "[Error] Asset not found: $ASSET_PATH" >&2
  exit 1
fi

echo "[Info] Building CLI..."
dotnet build "$CLI_PROJ" -v minimal

JSON_FILE="$OUTDIR/$(basename "$ASSET_PATH").json"

USE_MAPPINGS=0
if [[ -n "${USMAP_PATH:-}" ]]; then
  if [[ -f "$USMAP_PATH" ]]; then
    echo "[Info] Using mappings: $USMAP_PATH"
    USE_MAPPINGS=1
  else
    echo "[Warn] USMAP not found: $USMAP_PATH (continue without mappings)"
  fi
fi

run_cli() {
  local input_path="$1"
  local output_path="$2"
  local -a args=(dotnet run --project "$CLI_PROJ" -- json "$input_path" --ue-version VER_UE4_27)
  if [[ "$USE_MAPPINGS" -eq 1 ]]; then
    args+=(--mappings "$USMAP_PATH")
  fi
  args+=(--out "$output_path")
  "${args[@]}"
}

echo "[Info] Converting .uasset -> .json..."
run_cli "$ASSET_PATH" "$JSON_FILE"

if [[ ! -f "$JSON_FILE" ]]; then
  echo "[Error] JSON export failed: $JSON_FILE" >&2
  exit 2
fi

echo "[Done] JSON conversion test complete."
