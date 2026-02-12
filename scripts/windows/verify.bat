@echo off
setlocal enabledelayedexpansion

:: =============================================================================
:: UAssetStudio Verify Script (Production)
:: Decompile -> Compile -> Link -> Write, then verify output matches original
:: Supports: Drag-and-drop or command-line arguments
:: =============================================================================

:: Configuration - UAssetStudio.Cli.exe location
:: Default: Same directory as this script
set "EXE_PATH=%~dp0UAssetStudio.Cli.exe"

:: Default UE Version
set "UE_VERSION=VER_UE4_27"

:: Check if EXE exists
if not exist "!EXE_PATH!" (
    echo [Error] UAssetStudio.Cli.exe not found at: !EXE_PATH!
    echo Please modify EXE_PATH in this script or build the project first.
    pause
    exit /b 1
)

:: Parse arguments
set "ASSET_FILE="
set "OUTPUT_DIR="
set "USMAP_FILE="

:: Check if first argument is a file path (drag-and-drop)
set "FIRST_ARG=%~1"
if not "!FIRST_ARG!"=="" (
    if not exist "!FIRST_ARG!" goto :parse_args
    :: Check if it's not a flag (doesn't start with --)
    set "FIRST_TWO=!FIRST_ARG:~0,2!"
    if not "!FIRST_TWO!"=="--" (
        :: It's a file path - check extension
        set "EXT=!FIRST_ARG:~-7!"
        set "EXT_SHORT=!FIRST_ARG:~-4!"
        if /i "!EXT_SHORT!"==".uasset" (
            set "ASSET_FILE=!FIRST_ARG!"
            echo [Info] Drag-and-drop detected: UAsset file
            goto :run_verify
        ) else if /i "!EXT_SHORT!"==".umap" (
            set "ASSET_FILE=!FIRST_ARG!"
            echo [Info] Drag-and-drop detected: UMap file
            goto :run_verify
        ) else if /i "!EXT!"==".uasset" (
            set "ASSET_FILE=!FIRST_ARG!"
            echo [Info] Drag-and-drop detected: UAsset file
            goto :run_verify
        ) else if /i "!EXT!"==".umap" (
            set "ASSET_FILE=!FIRST_ARG!"
            echo [Info] Drag-and-drop detected: UMap file
            goto :run_verify
        ) else (
            echo [Error] Invalid file type: !FIRST_ARG!
            echo Please drag a .uasset or .umap file.
            pause
            exit /b 1
        )
    ) else (
        :: Starts with --, treat as command line argument
        goto :parse_args
    )
)

:parse_args
if "%~1"=="" goto :check_args
if /i "%~1"=="--asset" (
    set "ASSET_FILE=%~2"
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="--outdir" (
    set "OUTPUT_DIR=%~2"
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="--usmap" (
    set "USMAP_FILE=%~2"
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="--ue-version" (
    set "UE_VERSION=%~2"
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="--help" goto :show_help
shift
goto :parse_args

:show_help
echo Usage:
echo   verify.bat --asset ^<file.uasset^> [options]
echo   verify.bat ^<file.uasset^>          (drag-and-drop)
echo.
echo Required:
echo   --asset ^<path^>        Path to input .uasset or .umap file
echo.
echo Optional:
echo   --outdir ^<path^>       Output directory (default: same as asset)
echo   --usmap ^<path^>        Path to .usmap mappings file
echo   --ue-version ^<ver^>    UE version (default: VER_UE4_27)
echo.
echo Examples:
echo   verify.bat --asset "C:\Game\Weapon.uasset"
echo   verify.bat "C:\Game\Weapon.uasset"         (drag-and-drop)
echo   verify.bat --asset "C:\Game\Weapon.uasset" --usmap "C:\mappings.usmap"
pause
exit /b 1

:check_args
if "!ASSET_FILE!"=="" (
    echo [Error] Missing required argument: --asset
    echo.
    echo You can also drag-and-drop a .uasset or .umap file onto this script.
    echo.
    goto :show_help
)

:run_verify
:: Check if asset file exists
if not exist "!ASSET_FILE!" (
    echo [Error] Asset file not found: !ASSET_FILE!
    pause
    exit /b 1
)

:: Build command
echo [Info] ========================================
echo [Info] UAssetStudio Verify
echo [Info] (Decompile -^> Compile -^> Link -^> Write)
echo [Info] ========================================
echo [Info] Asset: !ASSET_FILE!
echo [Info] UE Version: !UE_VERSION!

set "CMD="!EXE_PATH!" verify "!ASSET_FILE!" --ue-version !UE_VERSION!"

if not "!USMAP_FILE!"=="" (
    if exist "!USMAP_FILE!" (
        echo [Info] Mappings: !USMAP_FILE!
        set "CMD=!CMD! --mappings "!USMAP_FILE!""
    ) else (
        echo [Warn] Mappings file not found: !USMAP_FILE!
    )
)

if not "!OUTPUT_DIR!"=="" (
    echo [Info] Output Dir: !OUTPUT_DIR!
    set "CMD=!CMD! --outdir "!OUTPUT_DIR!""
) else (
    for %%F in ("!ASSET_FILE!") do set "OUTPUT_DIR=%%~dpF"
    echo [Info] Output Dir: !OUTPUT_DIR! (default)
)

echo.
echo [Info] Executing...
echo.
!CMD!

if %ERRORLEVEL% neq 0 (
    echo.
    echo [Error] Verification failed with code %ERRORLEVEL%
    pause
    exit /b %ERRORLEVEL%
)

:: Display output files info
for %%F in ("!ASSET_FILE!") do set "FILENAME=%%~nF"
set "KMS_FILE=!OUTPUT_DIR!!FILENAME!.kms"
set "NEW_FILE=!OUTPUT_DIR!!FILENAME!.new.uasset"

echo.
echo [Success] Verification complete! Binary equality verified.
echo.
if exist "!KMS_FILE!" echo   - KMS: !KMS_FILE!
if exist "!NEW_FILE!" echo   - New Asset: !NEW_FILE!
pause
endlocal
exit /b 0
