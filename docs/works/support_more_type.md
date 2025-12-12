# 目标
我希望扩充 uasset 反编译器/编译器的功能，增加对更多类型的解析。

# 测试用例 
/Users/bytedance/Project/UAssetStudio/script/test_case_4_27/BP_PlayerControllerBase.uasset

# 测试脚本
/Users/bytedance/Project/UAssetStudio/script/test_verify_4.27.sh

# 现状
BP_PlayerControllerBase.uasset 反编译后生成：/Users/bytedance/Project/UAssetStudio/script/output/BP_PlayerControllerBase.kms

在重新编译会 uasset 资产过程中，会遇到如下错误：

```
[Error] No viable alternative at Line 453, Column 54, Text: 'uL'
Syntax error at line 453:    Struct<TimerHandle> AnselTimerHandle = { Handle: 0uL };
                             Struct<TimerHandle> AnselTimerHandle = { Handle: 0^^ };
```
 
在 /Users/bytedance/Project/UAssetStudio/KismetScript.Decompiler/KismetDecompiler.cs, 发现 Int64PropertyData， UInt32PropertyData、UInt64PropertyData
```
    private string GetDecompiledPropertyValue(PropertyData propData)
    {
        return propData switch
        {
            FloatPropertyData floatProp => FormatFloat(floatProp.Value),
            DoublePropertyData doubleProp => $"{doubleProp.Value}d",
            IntPropertyData intProp => intProp.Value.ToString(),
            Int64PropertyData int64Prop => $"{int64Prop.Value}L",
            Int16PropertyData int16Prop => int16Prop.Value.ToString(),
            Int8PropertyData int8Prop => int8Prop.Value.ToString(),
            UInt16PropertyData uint16Prop => uint16Prop.Value.ToString(),
            UInt32PropertyData uint32Prop => $"{uint32Prop.Value}u",
            UInt64PropertyData uint64Prop => $"{uint64Prop.Value}uL",
```
这 3 个，未被编译器支持。

# TODO
1. 重新设计 KMS 脚本。
2. 修改 Antlr4 和编译器，支持更多的类型。
4. 测试用例，验证运行， /Users/bytedance/Project/UAssetStudio/script/test_verify_4.27.sh 能够成功运行并且通过测试。
5. 使用 diff 工具，对比生成的 /Users/bytedance/Project/UAssetStudio/new.json 和 /Users/bytedance/Project/UAssetStudio/old.json 差异，
6. 持续修复最终消除差异

