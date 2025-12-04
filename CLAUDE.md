# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

UAssetStudio is a comprehensive .NET toolchain for analyzing, decompiling, compiling, and verifying Unreal Engine game assets (.uasset files). The project specializes in Blueprint (Kismet) visual scripting analysis and supports UE versions 4.13-5.3+.

## Core Capabilities

1. **Control Flow Graph (CFG) Generation** - Creates `.dot` files and summaries from UE assets
2. **Decompilation** - Converts `.uasset/.umap` files to human-readable `.kms` (Kismet Script) format
3. **Compilation** - Converts `.kms` scripts back to compiled `.uasset/.uexp` files
4. **Verification** - Validates complete round-trip: decompile → compile → link → write

## Solution Architecture

```
UAssetStudio.sln
├── UAssetStudio.Cli/                    # Main CLI entry point (.NET 9.0)
├── UAssetAPI/                           # Core asset reading/writing library
│   ├── UAssetAPI/                       # Main library
│   ├── UAssetAPI.Tests/                 # Unit tests
│   └── UAssetAPI.Benchmark/             # Performance tests
├── KismetScript.Compiler/               # ANTLR-based Kismet script compiler
├── KismetScript.Decompiler/             # Asset decompilation logic
├── KismetScript.Linker/                 # Asset linking and compilation
├── KismetScript.Parser/                 # Kismet script parser
├── KismetScript.Syntax/                 # AST and syntax tree definitions
├── KismetScript.Utilities/              # Shared utility classes
├── KismetAnalyzer.CFG/                  # Control flow graph generation
└── UAsset.Localization/                 # Localization support
```

## Essential Commands

### Build Commands
```bash
# Build entire solution
dotnet build

# Build specific configuration
dotnet build --configuration Release
dotnet build --configuration DebugTracing  # With PostSharp tracing

# Build and run tests
dotnet test
dotnet test --verbosity normal
```

### CLI Usage
```bash
# Generate control flow graph
UAssetStudio.Cli.exe cfg <asset> --mappings <usmap> --ue-version VER_UE5_6 --outdir <dir>

# Decompile asset to .kms script
UAssetStudio.Cli.exe decompile <asset> --mappings <usmap> --ue-version VER_UE5_6 --outdir <dir>

# Compile .kms script back to asset
UAssetStudio.Cli.exe compile <script.kms> --asset <original.uasset> --mappings <usmap> --ue-version VER_UE5_6 --outdir <dir>

# Verify complete round-trip
UAssetStudio.Cli.exe verify <asset> --mappings <usmap> --ue-version VER_UE5_6 --outdir <dir>
```

### Test Scripts
```bash
# Run verification tests
./script/test_verify_5.6.sh   # UE5.6 test
./script/test_verify_4.27.sh  # UE4.27 test

# Other test scripts
./script/test_compile.sh
./script/test_decompile.sh
./script/test_gen_dot.sh
```

## Key Development Patterns

### Binary Equality Requirement
This project maintains **binary equality** with original assets - critical for game modding. Always verify with `VerifyBinaryEquality()` method when modifying serialization.

### Property Type System
- Uses reflection-based automatic discovery (`MainSerializer.cs:78-134`)
- Property types extend `PropertyData` and are auto-registered
- Located in `UAssetAPI/PropertyTypes/` with Objects/ and Structs/ subdirectories

### Engine Version Support
- Primary: UE4.27, UE5.6
- Version specified via `--ue-version` flag (e.g., `VER_UE4_27`, `VER_UE5_6`)
- Test assets organized by version in `script/test_case_*/`

### Asset Processing Flow
1. **Reading**: `UAsset.cs` → `MainSerializer.cs` → Property type handlers
2. **Decompilation**: Asset → Kismet bytecode → `.kms` script
3. **Compilation**: `.kms` script → Kismet bytecode → Linked asset
4. **Verification**: Original → Decompile → Compile → Compare

## Critical Files and Entry Points

### Core Library (UAssetAPI)
- `UAsset.cs:1` - Main asset loading/saving API
- `MainSerializer.cs:1` - Serialization engine and property registry
- `AssetBinaryReader.cs` / `AssetBinaryWriter.cs` - Binary I/O
- `ExportTypes/Export.cs:1` - Base export class, 12 export types

### CLI Tool
- `UAssetStudio.Cli/Program.cs` - Command parsing and orchestration
- Uses System.CommandLine for argument handling

### Compiler/Decompiler
- `KismetScript.Compiler/` - ANTLR-based parser for `.kms` files
- `KismetScript.Decompiler/` - Blueprint bytecode decompilation
- `KismetScript.Linker/` - Asset linking and final compilation
- `KismetScript.Parser/` - Kismet script parsing infrastructure
- `KismetScript.Syntax/` - Abstract syntax tree and syntax definitions
- `KismetScript.Utilities/` - Shared utility classes for Kismet processing

## Testing Infrastructure

### Test Asset Organization
- `script/test_case_4_27/` - UE4.27 specific tests
- `script/test_case_5_6/` - UE5.6 specific tests with real game assets
- `UAssetAPI/UAssetAPI.Tests/` - Comprehensive unit test suite

### Real Game Assets Include
- `BP_PlayerControllerBase.uasset` - Blueprint controller
- `WPN_ZipLineGun.uasset` - Weapon system asset
- Assets from F1Manager2023, Palworld, Tekken, etc.

### Test Categories
- JSON serialization tests
- Multi-engine version compatibility
- Game-specific asset handling
- Custom property type testing

## Important Considerations

### Mappings Files
- `.usmap` files required for unversioned property parsing
- Critical for modern UE versions with unversioned properties
- Specified via `--mappings` parameter

### Custom Serialization Flags
Available in `UAsset.cs:36-62`:
- `NoDummies` - Skip dummy property generation
- `SkipParsingBytecode` - Skip Kismet bytecode parsing
- `SkipPreloadDependencyLoading` - Skip preload dependencies
- `SkipParsingExports` - Skip export parsing

### Error Handling
- Use `Assert` class for validation with detailed diff output
- Recent commits show focus on improved error messages and verification
- Always maintain binary compatibility for game modding use cases
