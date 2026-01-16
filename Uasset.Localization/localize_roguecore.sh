#!/bin/bash

# UAsset.Localization - RogueCore资源本地化脚本
# Created by Iris

# 设置变量
DLL_PATH="/Users/bytedance/Project/UAssetStudio/UAsset.Localization/bin/Debug/net9.0/UAsset.Localization.dll"
MAPPINGS_PATH="/Users/bytedance/Desktop/RogueCore_Mappings_by_Iris.usmap"
ASSETS_PATH="/Users/bytedance/Project/RogueCore/Content/"
UE_VERSION="VER_UE5_6"
OUTPUT_PATH="RogueCore_WeaponsNTools_Localization.json"

# 检查dll文件是否存在
if [ ! -f "$DLL_PATH" ]; then
    echo "错误：找不到 $DLL_PATH"
    echo "请确保项目已编译：dotnet build"
    exit 1
fi

# 检查映射文件是否存在
if [ ! -f "$MAPPINGS_PATH" ]; then
    echo "警告：找不到映射文件 $MAPPINGS_PATH"
    echo "命令将不带映射文件运行"
    MAPPINGS_ARG=""
else
    MAPPINGS_ARG="--mappings $MAPPINGS_PATH"
fi

# 检查资源目录是否存在
if [ ! -d "$ASSETS_PATH" ]; then
    echo "错误：找不到资源目录 $ASSETS_PATH"
    exit 1
fi

# 构建并执行命令
echo "开始处理RogueCore资源本地化..."
echo "目标目录: $ASSETS_PATH"
echo "UE版本: $UE_VERSION"
echo "输出文件: $OUTPUT_PATH"

dotnet "$DLL_PATH" "$ASSETS_PATH" \
    --ue-version "$UE_VERSION" \
    $MAPPINGS_ARG \
    --out "$OUTPUT_PATH"

if [ $? -eq 0 ]; then
    echo "本地化完成！结果保存在: $OUTPUT_PATH"
    
    # 显示统计信息
    if [ -f "$OUTPUT_PATH" ]; then
        LINE_COUNT=$(wc -l < "$OUTPUT_PATH")
        echo "共提取 $LINE_COUNT 条本地化条目"
    fi
else
    echo "本地化处理失败"
    exit 1
fi