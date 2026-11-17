---
kind: error_handling
name: TEngine 错误处理体系：GameFrameworkException 与 GameFrameworkLog 日志系统
slug: error_handling
category: error_handling
scope:
    - '**'
---

## 1. 总体方案

TEngine Unity 框架在错误处理上采用两层机制：
- **异常类型层**：定义统一的 `GameFrameworkException` 作为框架级异常基类，用于表达“参数非法、模块未找到、资源无效”等可恢复的运行时错误。
- **日志输出层**：通过 `GameFrameworkLog` + `ILogHelper` + `DefaultLogHelper` 提供带级别（Debug/Info/Warning/Error/Fatal）的结构化日志，并桥接到 Unity 的 `Debug.Log*` API；Fatal 级别最终抛出一个 `System.Exception` 以中断执行。

该仓库没有自定义的错误码枚举、也没有全局 try/catch 中间件或 panic/recover 机制；错误主要通过抛出异常和记录日志两种手段传播。

## 2. 关键文件与包

| 路径 | 作用 |
|---|---|
| `UnityProject/Assets/TEngine/Runtime/Core/Utility/GameFrameworkException.cs` | 框架统一异常类型，继承 `System.Exception`，支持序列化 |
| `UnityProject/Assets/TEngine/Runtime/Core/Log/GameFrameworkLogLevel.cs` | 日志等级枚举 `Debug / Info / Warning / Error / Fatal` |
| `UnityProject/Assets/TEngine/Runtime/Core/Log/GameFrameworkLog.ILogHelper.cs` | 日志抽象接口 `ILogHelper.Log(level, message)` |
| `UnityProject/Assets/TEngine/Runtime/Core/Log/GameFrameworkLog.cs` | 静态门面，持有 `s_LogHelper` 单例并提供 Debug/Info/Warning/Error/Fatal 重载 |
| `UnityProject/Assets/TEngine/Runtime/Core/Utility/DefaultHelper/DefaultLogHelper.cs` | 默认实现，格式化彩色日志、收集堆栈、转发到 Unity Debug API，并在 Fatal 时抛异常 |
| `UnityProject/Assets/TEngine/Runtime/Core/ModuleSystem.cs` | 模块系统大量使用 `GameFrameworkException` 表达“找不到模块/类型非法”等错误 |
| `UnityProject/Assets/TEngine/Runtime/Core/DataStruct/*` | 数据结构（LinkedList、MultiDictionary、TypeNamePair 等）对非法状态抛 `GameFrameworkException` |
| `UnityProject/Assets/GameScripts/HotFix/GameProto/LubanLib/ByteBuf.cs` | 二进制序列化层自定义 `SerializationException : Exception` |
| `UnityProject/Assets/GameScripts/HotFix/GameLogic/.../UIModule/UIModule.cs` | 业务层使用 `GameFrameworkException` 报告窗口创建失败、重复注册等 UI 错误 |

## 3. 架构与约定

### 3.1 日志子系统（可插拔）

- `GameFrameworkLog` 是 `static partial class`，内部维护一个静态字段 `private static ILogHelper s_LogHelper = new DefaultLogHelper();`。
- 所有公共方法（`Debug`/`Info`/`Warning`/`Error`/`Fatal`）都先检查 `s_LogHelper == null` 后直接返回，因此可以在初始化前安全调用。
- 通过 `SetLogHelper(ILogHelper logHelper)` 可以替换默认实现，实现日志后端的可插拔。
- `DefaultLogHelper` 内部用 `ELogLevel`（Info/Debug/Assert/Warning/Error/Exception）+ `FILTER_LEVEL = ELogLevel.Info` 做过滤；Warning 及以上才采集 `StackTrace`。
- 日志文本经 `GetFormatString` 加上 `[INFO] ►` / `[ERROR] ►` / `[EXCEPTION] ►` 等彩色标签，再交给 `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` / `Debug.LogAssertion`。
- Fatal 级别在 `LogImp` 中走 `throw new Exception(logStr)` 分支，即把致命错误转为异常终止。
- Editor 侧还包含 `LogRedirection`（`#if UNITY_EDITOR`），通过 `OnOpenAsset` 反射 `ConsoleWindow`，点击 Console 中的日志行自动跳转到实际源码位置。

### 3.2 异常类型体系

- 唯一由框架定义的异常类型是 `TEngine.GameFrameworkException`，标记 `[Serializable]`，提供无参、带消息、带 innerException、以及序列化构造器，遵循 .NET 异常规范。
- 数据结构和核心模块（`ModuleSystem`、`GameFrameworkLinkedList`、`GameFrameworkMultiDictionary`、`TypeNamePair`、`UIModule`）在检测到非法参数或非法状态时统一 `throw new GameFrameworkException(...)`。
- 二进制协议层 `ByteBuf.cs` 单独定义了 `SerializationException : Exception`，用于表示超出最大分段大小等序列化错误，说明协议层不混入框架异常。
- 内存池相关代码（`MemoryPool.cs`、`MemoryPool.MemoryCollection.cs`、`MemoryPoolExtension.cs`）仍直接使用裸 `throw new Exception(...)`，尚未迁移到 `GameFrameworkException`，这是当前代码库中观察到的不一致点。

### 3.3 错误传播模式

- **可恢复错误**：通过 `GameFrameworkLog.Warning` / `GameFrameworkLog.Error` 记录，调用方继续执行。
- **不可恢复错误**：通过 `throw new GameFrameworkException(...)` 向上传播，由上层（通常是 Procedure 流程节点）捕获并切换流程。
- **致命错误**：通过 `GameFrameworkLog.Fatal` 触发 `DefaultLogHelper` 内部的 `throw new Exception(...)`，或直接抛 `GameFrameworkException`。
- 业务流程位于 `UnityProject/Assets/GameScripts/Procedure/`，例如 `ProcedureLaunch.cs`、`ProcedureLoadAssembly.cs`、`ProcedureInitPackage.cs` 中使用 `try { ... } catch (Exception e) { ... }` 包裹异步流程，将异常转换为流程错误状态。

## 4. 约定与约束

- **日志入口统一**：框架内日志应通过 `GameFrameworkLog.Debug/Info/Warning/Error/Fatal` 调用，而非直接 `Debug.Log`；`GameFrameworkLog` 会委托给 `ILogHelper`，便于替换后端。
- **日志级别语义**：`GameFrameworkLogLevel` 定义为 `byte` 枚举，顺序为 Debug=0 → Info=1 → Warning=2 → Error=3 → Fatal=4；`DefaultLogHelper.FILTER_LEVEL = ELogLevel.Info` 意味着 Debug 日志默认被丢弃。
- **异常类型**：框架内部错误应使用 `TEngine.GameFrameworkException`；协议层错误使用 `SerializationException`。裸 `System.Exception` 仅在内存池等遗留代码中出现，属于未被统一化的例外。
- **Fatal 行为**：`DefaultLogHelper` 对 Fatal 级别直接 `throw new Exception(...)`，因此调用方不应依赖 Fatal 仅记录日志——它一定会中断当前线程。
- **编辑器调试体验**：`LogRedirection.OnOpenAsset` 通过反射解析 Console 中的堆栈，自动打开对应 `.cs` 文件的精确行号，前提是日志格式保持 `[DEBUG/INFO/ASSERT/WARNING/ERROR/EXCEPTION]` 前缀。
- **无全局错误中间件**：仓库中没有类似 Web 框架的全局异常处理器或中间件；错误处理分散在各模块自身及 Procedure 流程节点中，通过 try/catch 局部捕获。
- **无错误码枚举**：未发现统一的错误码（error code）枚举或 Result<T, E> 风格返回值；错误主要靠异常类型区分。
- **可序列化异常**：`GameFrameworkException` 实现了完整的序列化构造器，表明其设计考虑了跨进程/远程场景下的异常传递。

## 5. 观察到的不一致

- 部分核心代码（`MemoryPool.*`）仍使用裸 `throw new Exception(...)`，未迁移到 `GameFrameworkException`。
- 业务层 `UIModule.cs` 中既有 `throw new GameFrameworkException(...)`，也有 `UIWindow.cs` 中 `throw new Exception(...)`，说明异常类型的使用尚未完全收敛。
- 编辑器工具（如 `AtlasRefWindow.cs`、`ReferenceFinderData.cs`、`UIScriptGenerator/ScriptAutoGenerator.cs`）普遍使用 `catch (Exception e) { Debug.LogError(e); }` 吞掉异常并记录，这与运行时的异常传播策略不同，因为编辑器代码不需要崩溃整个游戏进程。
