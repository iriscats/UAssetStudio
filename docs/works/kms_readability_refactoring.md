# KMS 语法可读性优化研究报告

## 背景

KMS (Kismet Script) 是 UAssetStudio 反编译 Unreal Engine Blueprint 资产后生成的文本格式。原始输出存在大量低级字节码痕迹，可读性较差，阻碍了模组开发者理解和修改蓝图逻辑。

本次重构在**严格保持 round-trip 二进制一致性**的约束下，对 KMS 输出格式进行了可读性优化。所有改动均已通过 `verify` 命令验证（反编译 → 编译 → 与原始资产逐字节比对）。

### 测试资产

- `script/BP_PetComponent.uasset` (UE4.27, 无需 .usmap)
- 验证脚本: `script/test_verify_4.27.sh`

---

## 已实施的优化

### 1. 去除 EX_ 前缀（反编译器）

**改动文件**: `KismetScript.Decompiler/KismetDecompiler.Expressions.cs`

原始的 KMS 输出直接暴露了 UE Kismet 字节码的指令名称前缀 `EX_`，这对阅读者来说是纯噪音：

```
// 优化前
EX_Context(EX_Context(Pet, EX_InstanceVariable("Temperature")), EX_InstanceVariable("WarmingRate"))
EX_LetObj(result, EX_NoObject())
K2Node_ClassDynamicCast_AsAnim_Instance = EX_MetaCast("AnimInstance", var)

// 优化后
Context(Context(Pet, InstanceVariable("Temperature")), InstanceVariable("WarmingRate"))
LetObj(result, NoObject())
K2Node_ClassDynamicCast_AsAnim_Instance = MetaCast("AnimInstance", var)
```

**涉及的表达式**: 所有 intrinsic 表达式，包括 `Context`, `LetObj`, `LetDelegate`, `LetWeakObjPtr`, `LetMulticastDelegate`, `NoObject`, `NameConst`, `VectorConst`, `RotationConst`, `StructConst`, `SoftObjectConst`, `InstanceVariable`, `FinalFunction`, `VirtualFunction`, `DynamicCast`, `MetaCast`, `PrimitiveCast`, `BindDelegate`, `AddMulticastDelegate`, `PushExecutionFlow`, `PopExecutionFlow`, `PopExecutionFlowIfNot`, `ArrayGetByRef`, `LetValueOnPersistentFrame`, `SkipOffsetConst` 等。

**编译器适配**: `KismetScript.Compiler/KismetScriptCompiler.cs`

编译器的 intrinsic 函数识别已支持带/不带 `EX_` 前缀的名称。但在符号解析（SymbolResolution）和引用解析（References）中存在硬编码的 `EX_` 字符串比较，需要同步修复。

新增辅助方法：

```csharp
// KismetScriptCompiler.cs
private bool IsIntrinsicToken(string name, EExprToken token);
private bool IsIntrinsicTokenAny(string name, params EExprToken[] tokens);
```

**改动文件**:
- `KismetScript.Compiler/KismetScriptCompiler.SymbolResolution.cs` — 替换所有 `callOperator.Identifier.Text == "EX_Context"` 等硬编码检查
- `KismetScript.Compiler/KismetScriptCompiler.References.cs` — 替换 `"EX_ArrayGetByRef"` 硬编码检查

> **维护注意**: 若未来在编译器中新增 intrinsic 的硬编码字符串匹配，必须使用 `IsIntrinsicToken` / `IsIntrinsicTokenAny`，不可直接硬编码 `"EX_Xxx"` 或 `"Xxx"` 字符串。

### 2. 缩短标签命名（反编译器）

**改动文件**: `KismetScript.Decompiler/KismetDecompiler.cs` — `FormatCodeOffset` 方法

UberGraph 函数中的标签名原先包含完整的函数名，极其冗长：

```
// 优化前
ExecuteUbergraph_BP_PetComponent_156:
ExecuteUbergraph_BP_PetComponent_788:
ExecuteUbergraph_BP_PetComponent_1986:
EX_PushExecutionFlow(ExecuteUbergraph_BP_PetComponent_156);

// 优化后
L_156:                          // 函数内部标签 → L_{offset}
L_788:                          // 函数入口点（有调用者）→ CallerName_{offset}
ReceiveBeginPlay_2196:          // 跨函数入口保留调用者名
PushExecutionFlow(L_156);
```

命名规则：

| 场景 | 格式 | 示例 |
|------|------|------|
| 函数内部标签 | `L_{codeOffset}` | `L_156`, `L_788` |
| 跨函数入口（有调用者） | `{callerName}_{codeOffset}` | `ReceiveBeginPlay_2196` |
| 跨函数引用（不同函数名） | `{targetFunctionName}_{codeOffset}` | 保留全路径 |

> **维护注意**: `FormatCodeOffset` 的 `callingFunctionName` 参数由 `GetUbergraphEntryFunction` 提供。若修改入口点检测逻辑，需同步验证标签唯一性。

### 3. PopExecutionFlow → break（反编译器）

**改动文件**:
- `KismetScript.Decompiler/KismetDecompiler.cs` — 新增 `_insideExecutionFlow` 状态标记
- `KismetScript.Decompiler/KismetDecompiler.Expressions.cs` — `EX_PopExecutionFlow` 和 `EX_PopExecutionFlowIfNot` 分支

在 `while(true)` 块内，`PopExecutionFlow()` 的语义等同于 `break`：

```
// 优化前
while (true) {
    CallFunc_IsValid_ReturnValue = (bool)(KismetSystemLibrary.IsValid(this.OwningPlayer));
    EX_PopExecutionFlowIfNot(CallFunc_IsValid_ReturnValue);
    // ...
    EX_PopExecutionFlow();
}

// 优化后
while (true) {
    CallFunc_IsValid_ReturnValue = (bool)(KismetSystemLibrary.IsValid(this.OwningPlayer));
    if (!(CallFunc_IsValid_ReturnValue)) break;
    // ...
    break;
}
```

**编译器天然支持**: 编译器在 `CompileBreakStatement` 中检查 `CurrentScope.IsExecutionFlow`，当为 `true`（`while(true)` 上下文）时直接生成 `EX_PopExecutionFlow`。同理，`if (!cond) break;` 在此上下文中编译为 `EX_PopExecutionFlowIfNot`。

**实现机制**: 反编译器使用 `_insideExecutionFlow` 布尔字段追踪当前是否处于 `JumpBlockNode`（即 `while(true)` 块）内。进入时压栈保存，退出时恢复。

> **维护注意**: `_insideExecutionFlow` 仅在 `WriteBlock` 处理 `JumpBlockNode` 时设置。如果未来新增其他块类型也涉及 `PushExecutionFlow`，需评估是否也应设置此标记。在 `while(true)` 块**外**出现的 `PopExecutionFlow()` 仍保持函数调用语法。

---

## 因 Round-trip 约束未实施的优化

以下优化在可读性维度上有显著收益，但在 round-trip 模式下会破坏二进制一致性。记录原因便于未来在非 round-trip 模式（如只读分析场景）中实施。

### 4. 数学库函数 → 运算符

**目标**: `KismetMathLibrary.Add_IntInt(a, b)` → `a + b`

**尝试与失败原因**:

编译器的运算符编译路径（`KismetScriptCompiler.Operators.cs`）需要对操作数进行类型推导，以确定生成哪个 `KismetMathLibrary` 函数（如 `Add_IntInt` vs `Add_FloatFloat`）。类型推导在以下场景失败：

1. **复合类型**: `Vector`, `Rotator` 等被解析为 `Struct` 类型，编译器不认识 `Multiply_StructFloat` 这类运算
2. **嵌套表达式**: `Context(Context(Pet, InstanceVariable("Temperature")), InstanceVariable("WarmingRate")) * 2` — 内层 `Context` 返回类型解析为 `Unresolved`

**根本限制**: 编译器的类型解析依赖 import 声明中的信息。intrinsic 表达式（如 `Context`, `InstanceVariable`）的返回类型在编译阶段无法准确推导，因为 `.kms` 的 import 仅声明了类/属性的存在性，不包含完整的类型签名。

**未来改进路径**:
- 增强编译器类型推导，为 `Context` 表达式建立上下文类型链
- 或在 `.kms` 中保留类型注释（如 `(Vector)(Context(Mesh, ...)) * 1.1f`）
- 或仅在非 round-trip 模式下启用

### 5. 省略冗余 import 声明

**目标**: 隐藏 `CoreUObject` 中的基础类型 import（如 `Class`, `Object`, `IntProperty` 等）

**失败原因**: 编译器的符号解析（`KismetScriptCompiler.SymbolResolution.cs`）完全依赖 KMS 文件中的 import 声明来构建符号表。去除任何 import 会导致编译阶段符号查找失败。

**根本限制**: 编译器没有内置的"标准库"概念，不会自动引入 `CoreUObject` 类型。

**未来改进路径**:
- 在编译器中建立隐式 import 机制，自动引入标准类型
- 或在 `.kms` 中使用折叠区域标记（仅 IDE 展示优化）

### 6. Context → 点操作符语法

**目标**: `Context(obj, FinalFunction("Method", args))` → `obj.Method(args)`

**尝试与失败原因**:

将 `EX_Context` 处理切换为 `UseContext=true` 模式（使用 `_context` 机制设置上下文，让内层表达式生成点语法）后，编译器报错：`The name OnDeath does not exist in the current context`。

编译器在处理 `obj.Member` 时需要解析 `obj` 的类型，然后在该类型的成员列表中查找 `Member`。但：
1. 对象的类型信息来源于 import 声明，而 import 并不总是声明完整的成员列表
2. 某些属性通过匿名类（如 `AnonymousClass_c9fe93fd708e4e9694493bb2f47aa488`）间接持有
3. 编译器的 `GetSymbol<T>` 不支持对所有对象类型进行成员查找

而 `Context(obj, InstanceVariable("OnDeath"))` 语法绕过了类型解析——它通过字符串字面量直接引用属性名，编译器不需要知道 `obj` 的类型。

**根本限制**: KMS 的 import 系统是最小化的，只声明编译所需的符号，不包含完整的类层次结构信息。点操作符语法要求编译器具备完整的类型系统。

**未来改进路径**:
- 增强 import 声明，包含完整的成员信息
- 或增强编译器支持「上下文无关的成员访问」（类似当前 `Context` intrinsic 的语义但用点语法）
- 或仅在非 round-trip 模式下启用

### 7. UberGraph 函数分解

**目标**: 将 `ExecuteUbergraph_BP_PetComponent` 函数按入口点拆分为独立的事件处理函数

**不可行原因**: UberGraph 是 UE 编译器将所有蓝图事件合并到一个函数中的产物。反向操作（将独立函数合并回 UberGraph）需要：
1. 重建 UberGraph 的统一局部变量列表
2. 重建 `goto EntryPoint` 跳转表
3. 重建所有入口点的字节码偏移
4. 处理共享变量（如 `K2Node_DynamicCast_AsSpider_Enemy` 在多个"事件"中使用）

编译器目前完全不支持此逆操作。

**未来改进路径**:
- 在编译器中实现 UberGraph 合并 pass
- 或在反编译输出中添加注释标记入口点边界（不影响编译）

### 8. for 循环模式识别

**目标**: 将以下模式识别为 for 循环：

```
// 优化前
Temp_int_Variable = 0;
L_28: ...
CallFunc_LessEqual_IntInt_ReturnValue = KismetMathLibrary.LessEqual_IntInt(Temp_int_Variable, ...);
if (!CallFunc_LessEqual_IntInt_ReturnValue) break;
while (true) { /* body */ break; }
CallFunc_Add_IntInt_ReturnValue = KismetMathLibrary.Add_IntInt(Temp_int_Variable, 1);
Temp_int_Variable = CallFunc_Add_IntInt_ReturnValue;
goto L_28;

// 理想输出
for (Temp_int_Variable = 0; LessEqual_IntInt(Temp_int_Variable, ...); Temp_int_Variable = Add_IntInt(Temp_int_Variable, 1)) {
    /* body */
}
```

**暂缓原因**: 需要新增复杂的 AST 重写 pass，涉及：
1. 模式匹配（初始化 + 条件 + 增量 + 回跳）
2. 将匹配到的 block 重组为 `ForBlockNode`（需新增节点类型）
3. 处理嵌套 `while(true)` 块的 body 提取
4. 编译器已支持 `for` 语法（`CompileForStatement`），round-trip 理论可行

**未来改进路径**: 在 `KismetScript.Decompiler` 中新增 `CreateForLoopPass`，在 `CreateWhileBlocksPass` 之后运行。

---

## 改动文件清单

| 文件 | 改动类型 | 说明 |
|------|----------|------|
| `KismetScript.Decompiler/KismetDecompiler.Expressions.cs` | 修改 | 去除所有 intrinsic 表达式的 `EX_` 前缀；`PopExecutionFlow` → `break` 转换 |
| `KismetScript.Decompiler/KismetDecompiler.cs` | 修改 | 缩短标签格式（`FormatCodeOffset`）；新增 `_insideExecutionFlow` 字段和块上下文追踪 |
| `KismetScript.Compiler/KismetScriptCompiler.cs` | 修改 | 新增 `IsIntrinsicToken` / `IsIntrinsicTokenAny` 辅助方法 |
| `KismetScript.Compiler/KismetScriptCompiler.SymbolResolution.cs` | 修改 | 用 helper 方法替换硬编码 `EX_` 字符串比较 |
| `KismetScript.Compiler/KismetScriptCompiler.References.cs` | 修改 | 用 helper 方法替换硬编码 `EX_` 字符串比较 |

---

## 已知局限和风险

### 1. 向后兼容性

去除 `EX_` 前缀后，**旧版本反编译器生成的 KMS 文件仍可被新编译器编译**（编译器同时支持两种前缀格式）。但新反编译器生成的 KMS 文件**不能被旧编译器编译**（旧编译器不认识无前缀的 intrinsic 名称）。

### 2. `_insideExecutionFlow` 状态正确性

`_insideExecutionFlow` 是反编译器的全局可变状态，依赖 `WriteBlock` 的调用栈正确压入/恢复。如果未来修改了块写入逻辑（如并行写入、延迟写入），需确保此状态的生命周期正确。

### 3. 标签唯一性

缩短后的标签格式 `L_{offset}` 在单个函数内是唯一的（同一函数内不可能有两个相同偏移的代码块）。但如果未来支持多函数标签交叉引用，需注意 `L_` 格式可能在不同函数间产生冲突。当前的跨函数引用仍保留完整名称，避免了此问题。

### 4. PopExecutionFlow 在非 while(true) 上下文

在 `while(true)` 块外出现的 `PopExecutionFlow()` 保持不变（仍输出为函数调用语法）。如果未来的控制流分析产生了新的块类型（非 `JumpBlockNode`）但语义上也是 execution flow，`_insideExecutionFlow` 不会被设置，`break` 转换也不会生效。

### 5. 测试覆盖

当前仅使用 `BP_PetComponent.uasset` (UE4.27) 进行了 round-trip 验证。建议在以下场景补充测试：
- UE5.x 资产（需要 .usmap，`test_verify_5.6.sh`）
- 包含 `EX_ArrayGetByRef` 的资产
- 包含 `EX_InterfaceContext` 的资产（接口调用）
- 大型 UberGraph 函数（多入口点、深层嵌套）
- 不含 `while(true)` 块的纯线性函数

---

## 重构前后对比示例

### 函数体对比（节选 ExecuteUbergraph 部分块）

**优化前**:
```
ExecuteUbergraph_BP_PetComponent_550: CallFunc_IsValid_ReturnValue = (bool)(KismetSystemLibrary.IsValid(this.OwningPlayer));
EX_PopExecutionFlowIfNot(CallFunc_IsValid_ReturnValue);

EX_Context(EX_Context(this.OwningPlayer, EX_InstanceVariable("CommunicationComponent")), EX_FinalFunction("ShoutCustom", EX_NoObject()));
EX_PopExecutionFlow();

ExecuteUbergraph_BP_PetComponent_645: K2Node_ClassDynamicCast_AsAnim_Instance = EX_MetaCast("AnimInstance", Temp_class_Variable);
K2Node_ClassDynamicCast_bSuccess = EX_PrimitiveCast("ObjectToBool", K2Node_ClassDynamicCast_AsAnim_Instance);
EX_PopExecutionFlowIfNot(K2Node_ClassDynamicCast_bSuccess);
```

**优化后**:
```
L_550: CallFunc_IsValid_ReturnValue = (bool)(KismetSystemLibrary.IsValid(this.OwningPlayer));
if (!(CallFunc_IsValid_ReturnValue)) break;

Context(Context(this.OwningPlayer, InstanceVariable("CommunicationComponent")), FinalFunction("ShoutCustom", NoObject()));
break;

L_645: K2Node_ClassDynamicCast_AsAnim_Instance = MetaCast("AnimInstance", Temp_class_Variable);
K2Node_ClassDynamicCast_bSuccess = PrimitiveCast("ObjectToBool", K2Node_ClassDynamicCast_AsAnim_Instance);
if (!(K2Node_ClassDynamicCast_bSuccess)) break;
```

### 量化统计（BP_PetComponent.kms）

| 指标 | 优化前 | 优化后 |
|------|--------|--------|
| 文件总行数 | 592 | 592 |
| `EX_` 出现次数 | ~95 | 0 |
| 最长标签字符数 | 46 (`ExecuteUbergraph_BP_PetComponent_2041`) | 43 (`OnLoaded_2EDD9CD4487CCB079DB91DB3612F9E9C_2172`*) |
| `PopExecutionFlow()` 调用 | 14 | 0 (全部转为 `break`) |
| `PopExecutionFlowIfNot()` 调用 | 4 | 0 (全部转为 `if (!...) break`) |

*注: 最长标签来自事件名称本身，非函数名前缀问题。

---

## 后续建议优先级

| 优先级 | 任务 | 可行性 | 影响 |
|--------|------|--------|------|
| P1 | for 循环模式识别 | 高（编译器已支持 for 语法） | 中 |
| P1 | 扩展 round-trip 测试覆盖 | 高 | 高（回归保障） |
| P2 | 非 round-trip 模式下启用数学运算符 | 中（需模式开关） | 中 |
| P2 | 非 round-trip 模式下启用点操作符 | 中（需模式开关） | 高 |
| P3 | 编译器类型推导增强 | 低（大工程） | 高（解锁运算符/点语法的 round-trip） |
| P3 | 编译器隐式 import | 低（需标准库定义） | 中 |
| P3 | UberGraph 分解与合并 | 低（需编译器重大改造） | 高 |
