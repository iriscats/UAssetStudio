@echo off
setlocal enabledelayedexpansion

:: =============================================================================
:: UAssetStudio Compile Script (Production)
:: Compile .kms script to .uasset
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
set "KMS_FILE="
set "ASSET_FILE="
set "OUTPUT_FILE="
set "USMAP_FILE="

:: Check if first argument is a file path (drag-and-drop)
set "FIRST_ARG=%~1"
if not "!FIRST_ARG!"=="" (
    if not exist "!FIRST_ARG!" goto :parse_args
    :: Check if it's not a flag (doesn't start with --)
    set "FIRST_TWO=!FIRST_ARG:~0,2!"
    if not "!FIRST_TWO!"=="--" (
        :: It's a file path - check extension
        set "EXT=!FIRST_ARG:~-4!"
        if /i "!EXT!"==".kms" (
            set "KMS_FILE=!FIRST_ARG!"
            echo [Info] Drag-and-drop detected: KMS file
            goto :run_compile
        ) else (
            echo [Error] Invalid file type for compile: !FIRST_ARG!
            echo Please drag a .kms file or use command-line arguments.
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
if /i "%~1"=="--kms" (
    set "KMS_FILE=%~2"
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="--asset" (
    set "ASSET_FILE=%~2"
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="--out" (
    set "OUTPUT_FILE=%~2"
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
echo   compile.bat --kms ^<file.kms^> [options]
echo   compile.bat ^<file.kms^>        (drag-and-drop)
echo.
echo Required:
echo   --kms ^<path^>          Path to input .kms script file
echo.
echo Optional:
echo   --asset ^<path^>        Path to original .uasset file (for template)
echo   --out ^<path^>          Output file path (e.g., C:\Mods\MyMod.uasset)
echo   --usmap ^<path^>        Path to .usmap mappings file
echo   --ue-version ^<ver^>    UE version (default: VER_UE4_27)
echo.
echo Examples:
echo   compile.bat --kms "C:\input.kms" --asset "C:\original.uasset"
echo   compile.bat "C:\input.kms"        (drag-and-drop with default settings)
echo   compile.bat --kms "C:\input.kms" --asset "C:\orig.uasset" --usmap "C:\mappings.usmap"
pause
exit /b 1

:check_args
if "!KMS_FILE!"=="" (
    echo [Error] Missing required argument: --kms
echo.
    echo You can also drag-and-drop a .kms file onto this script.
    echo.
    goto :show_help
)

:run_compile
:: Check if KMS file exists
if not exist "!KMS_FILE!" (
    echo [Error] KMS file not found: !KMS_FILE!
    pause
    exit /b 1
)

:: Build command
echo [Info] ========================================
echo [Info] UAssetStudio Compile
echo [Info] ========================================
echo [Info] KMS File: !KMS_FILE!
echo [Info] UE Version: !UE_VERSION!

set "CMD="!EXE_PATH!" compile "!KMS_FILE!" --ue-version !UE_VERSION!"

if not "!ASSET_FILE!"=="" (
    if exist "!ASSET_FILE!" (
        echo [Info] Asset Template: !ASSET_FILE!
        set "CMD=!CMD! --asset "!ASSET_FILE!""
    ) else (
        echo [Warn] Asset file not found: !ASSET_FILE!
    )
)

if not "!USMAP_FILE!"=="" (
    if exist "!USMAP_FILE!" (
        echo [Info] Mappings: !USMAP_FILE!
        set "CMD=!CMD! --mappings "!USMAP_FILE!""
    ) else (
        echo [Warn] Mappings file not found: !USMAP_FILE!
    )
)

if not "!OUTPUT_FILE!"=="" (
    echo [Info] Output: !OUTPUT_FILE!
    set "CMD=!CMD! --out "!OUTPUT_FILE!""
) else (
    echo [Info] Output: (default path)
)

echo.
echo [Info] Executing...
echo.
!CMD!

if %ERRORLEVEL% neq 0 (
    echo.
    echo [Error] Compilation failed with code %ERRORLEVEL%
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [Success] Compilation complete!
pause
endlocal
exit /b 0
