---
kind: logging_system
name: TEngine 日志系统：基于 Conditional 宏与 ILogHelper 可插拔的分级日志框架
slug: logging_system
category: logging_system
scope:
    - '**'
---

## 1. 使用的系统与架构

TEngine 在 Unity 工程中内置了一套自研的轻量级日志子系统，位于 `UnityProject/Assets/TEngine/Runtime/Core/Log/` 目录。它不是第三方日志库（如 NLog、Serilog），而是对 Unity 自身 `UnityEngine.Debug` 的封装，并通过 C# 预编译条件宏实现零开销的日志级别裁剪。

核心组件分为三层：
- **API 层**：`Log` 静态类（`TEngine.Log`），对外暴露 `Debug / Info / Warning / Error / Fatal` 五个方法族，每个方法提供从 0 到 16 个参数的泛型重载，调用格式为 `Log.Info("msg {0}", arg)`。
- **路由层**：`GameFrameworkLog` 静态类，内部持有 `ILogHelper s_LogHelper = new DefaultLogHelper()`，所有 `Log.*` 调用最终转发到该 helper。
- **实现层**：`DefaultLogHelper : ILogHelper`，默认将日志输出到 Unity Console；通过 `GameFrameworkLog.SetLogHelper(...)` 可替换为自定义 sink（例如文件、网络、远程收集）。

日志等级由 `GameFrameworkLogLevel` 枚举定义：`Debug(0) → Info → Warning → Error → Fatal`，以 `byte` 存储。

## 2. 关键文件

| 文件 | 职责 |
|---|---|
| `Runtime/Core/Log/Log.cs` | 对外 API，所有方法均使用 `[Conditional]` 装饰 |
| `Runtime/Core/Log/GameFrameworkLog.cs` | 内部路由，持有 `ILogHelper` 单例 |
| `Runtime/Core/Log/GameFrameworkLogLevel.cs` | 日志等级枚举 |
| `Runtime/Core/Log/GameFrameworkLog.ILogHelper.cs` | `ILogHelper` 接口定义（仅含 `void Log(GameFrameworkLogLevel, object)`） |
| `Runtime/Core/Utility/DefaultHelper/DefaultLogHelper.cs` | 默认实现，写 Unity Console |
| `Runtime/Module/RootModule.cs` | 启动时通过反射加载并设置 `logHelperTypeName`（默认 `"TEngine.DefaultLogHelper"`） |
| `Editor/DefineSymbols/LogScriptingDefineSymbols.cs` | Editor 菜单工具，管理 ENABLE_* 宏 |

## 3. 架构与设计决策

### 3.1 预编译条件裁剪（零运行时开销）

`Log.Debug` 等方法被同时标记了多个 `[Conditional]`：

```csharp
[Conditional("ENABLE_LOG")]
[Conditional("ENABLE_DEBUG_LOG")]
[Conditional("ENABLE_DEBUG_AND_ABOVE_LOG")]
public static void Debug(object message)
```

这意味着只要定义了 `ENABLE_LOG`、`ENABLE_DEBUG_LOG` 或 `ENABLE_DEBUG_AND_ABOVE_LOG` 中任意一个符号，编译器才会生成 `Debug` 调用；否则整条语句被完全剔除。Info 及以上级别的方法则对应 `ENABLE_INFO_LOG`、`ENABLE_INFO_AND_ABOVE_LOG` 等组合。

编辑器侧提供了完整的菜单入口 `TEngine/Log Scripting Define Symbols/...`，支持：
- `Disable All Logs` — 清除所有 ENABLE_* 符号
- `Enable All Logs` — 仅启用 `ENABLE_LOG`
- `Enable Debug/Info/Warning/Error/Fatal And Above Logs` — 启用对应的 `_AND_ABOVE_` 符号
- `SetSpecifyLogScriptingDefineSymbols` — 精确指定某几个级别

这构成了仓库内控制日志输出的唯一官方入口。

### 3.2 可插拔 sink 机制

`GameFrameworkLog` 通过 `ILogHelper` 抽象出日志落盘逻辑：

```csharp
private static ILogHelper s_LogHelper = new DefaultLogHelper();
public static void SetLogHelper(ILogHelper logHelper) { s_LogHelper = logHelper; }
```

默认实现 `DefaultLogHelper` 将消息写入 Unity Console，并在堆栈过滤中排除自身及 TEngine 内部路径（见其包含 `assetPath.Contains("DefaultLogHelper.cs")` 等判断）。`RootModule` 在初始化阶段通过字符串类型名 `"TEngine.DefaultLogHelper"` 反射创建并注入 helper，允许业务工程覆盖该类型名以实现自定义 sink。

### 3.3 参数格式化

带 `{}` 占位符的重载在 `GameFrameworkLog` 中统一调用 `Utility.Text.Format(format, args...)`，而非直接 `string.Format`，这是 TEngine 内部的文本格式化实现。

## 4. 观察到的约定与约束

- **所有业务日志必须走 `TEngine.Log`**：在已检查的 GameLogic、Procedure、UIBindComponent 等模块中，日志调用统一为 `Log.Debug / Log.Info / Log.Warning / Log.Error / Log.Fatal`，未见直接使用 `UnityEngine.Debug.Log` 作为业务日志的场景。
- **日志级别语义**：`Debug` 用于开发期调试信息（下载进度、资源模式检测等）；`Info` 用于正常流程节点（模块 shutdown、下载完成等）；`Warning` 用于异常但非致命情况（热更新入口提示）；`Error` 用于错误分支（未知资源模式、缺少组件、索引越界）；`Fatal` 用于不可恢复状态（找不到 UIRoot、Singleton 创建失败）。
- **宏开关是强制性的构建期裁剪**：`Log.cs` 中每个方法的文档注释明确写明“仅在带有 ENABLE_LOG / ENABLE_DEBUG_LOG / ... 预编译选项时生效”，并由 `[Conditional]` 属性在编译器层面保证——未定义对应符号的方法体不会出现在 IL 中。
- **日志 sink 可通过 `GameFrameworkLog.SetLogHelper` 替换**：这是框架提供的扩展点，`RootModule` 默认通过反射加载 `TEngine.DefaultLogHelper`。
- **没有结构化字段**：当前日志输出是纯字符串拼接（`$"..."` 或 `Format`），未发现键值对、JSON 或结构化字段的设计。
- **没有运行时动态开关**：日志级别的控制完全依赖编译期宏，不存在运行期切换 level 的 API（`GameFrameworkLogLevel` 枚举存在但未在 `GameFrameworkLog` 中暴露阈值配置）。
- **日志文件输出位置**：UnityProject 根下存在空目录 `Logs/`，但当前代码中未见向该目录写文件的 sink 实现；默认 sink 只输出到 Unity Console。

## 5. 总结

TEngine 的日志系统是一个极简但设计清晰的方案：对外统一的 `Log` API + 编译期宏裁剪 + 可插拔 `ILogHelper` sink。它不引入外部依赖，通过 Editor 菜单集中管理宏定义，适合 Unity 游戏工程在开发期保留丰富日志、发布期零开销的目标。