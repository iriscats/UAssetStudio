# 目标
我希望扩充 uasset 反编译器的功能，增加对数据资产的解析

# 测试用例 
/Users/bytedance/Project/UAssetStudio/script/test_case_4_27/STE_FastZiplineSpeed.uasset

# 现状
```
[Parsed, ReplicationDataIsSetUp, EditInlineNew, CompiledFromBlueprint, HasInstancedReference]
class STE_FastZiplineSpeed_C : StatusEffect {
}
```

# 期望

参考反序列化后的 json：/Users/bytedance/Project/UAssetStudio/script/test_case_4_27/STE_FastZiplineSpeed.json
能够在 kms，显式存在 StatChange =4.0f 这些值。

```
      "$type": "UAssetAPI.ExportTypes.NormalExport, UAssetAPI",
      "Data": [
        {
          "$type": "UAssetAPI.PropertyTypes.Objects.ObjectPropertyData, UAssetAPI",
          "Name": "Stat",
          "ArrayIndex": 0,
          "IsZero": false,
          "PropertyTagFlags": "None",
          "PropertyTagExtensions": "NoExtension",
          "Value": -12
        },
        {
          "$type": "UAssetAPI.PropertyTypes.Objects.FloatPropertyData, UAssetAPI",
          "Value": 4.0,
          "Name": "StatChange",
          "ArrayIndex": 0,
          "IsZero": false,
          "PropertyTagFlags": "None",
          "PropertyTagExtensions": "NoExtension"
        }
      ],
```

# TODO
1. 帮重新设计 KMS 脚本。
2. 修改反编译器生成新的 kms 文件，包含 StatChange =4.0f 这些值。
3. 修改编译器支持，能够将 kms 中的 StatChange =4.0f 这些值，编译到 uasset 中。
4. 测试用例，验证运行， /Users/bytedance/Project/UAssetStudio/script/test_verify_4.27.sh 能够成功运行并且通过测试。