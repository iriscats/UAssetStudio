@echo off
REM UAsset.Localization - RogueCore资源本地化脚本
REM Created by Iris

REM 设置变量
set DLL_PATH=UAsset.Localization\bin\Debug\net9.0\UAsset.Localization.dll
set MAPPINGS_PATH=C:\Users\bytedance\Desktop\RogueCore_Mappings_by_Iris.usmap
set ASSETS_PATH=C:\Users\bytedance\Project\RogueCore\Content\WeaponsNTools
set UE_VERSION=VER_UE5_6
set OUTPUT_PATH=RogueCore_WeaponsNTools_Localization.json

echo 开始处理RogueCore资源本地化...

REM 检查dll文件是否存在
if not exist "%DLL_PATH%" (
    echo 错误：找不到 %DLL_PATH%
    echo 请确保项目已编译：dotnet build
    pause
    exit /b 1
)

REM 检查映射文件是否存在
if exist "%MAPPINGS_PATH%" (
    set MAPPINGS_ARG=--mappings "%MAPPINGS_PATH%"
) else (
    echo 警告：找不到映射文件 %MAPPINGS_PATH%
    echo 命令将不带映射文件运行
    set MAPPINGS_ARG=
)

REM 检查资源目录是否存在
if not exist "%ASSETS_PATH%" (
    echo 错误：找不到资源目录 %ASSETS_PATH%
    pause
    exit /b 1
)

REM 构建并执行命令
echo 目标目录: %ASSETS_PATH%
echo UE版本: %UE_VERSION%
echo 输出文件: %OUTPUT_PATH%

dotnet "%DLL_PATH%" "%ASSETS_PATH%" --ue-version "%UE_VERSION%" %MAPPINGS_ARG% --out "%OUTPUT_PATH%"

if %ERRORLEVEL% equ 0 (
    echo 本地化完成！结果保存在: %OUTPUT_PATH%
    
    REM 显示统计信息
    if exist "%OUTPUT_PATH%" (
        for /f %%i in ('find /c /v "" ^< "%OUTPUT_PATH%"') do set LINE_COUNT=%%i
        echo 共提取 %LINE_COUNT% 条本地化条目
    )
) else (
    echo 本地化处理失败
    pause
    exit /b 1
)

pause