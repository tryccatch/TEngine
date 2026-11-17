---
kind: configuration_system
name: TEngine 配置系统：ScriptableObject + Luban 双轨配置体系
slug: configuration_system
category: configuration_system
scope:
    - '**'
---

## 概述

本仓库存在两套并行的配置系统，分别服务于「运行时引擎参数」和「游戏数据表」两类场景：

1. **运行时配置**：基于 Unity `ScriptableObject` + `Settings` 单例聚合，存放于 `UnityProject/Assets/TEngine/Settings/`。
2. **游戏数据配置**：基于 [Luban](https://github.com/focus-creative-games/luban) 将 Excel 定义编译为 C# 代码与二进制资源，由自定义 `ConfigSystem` 通过 `IResourceModule` 加载。

没有发现 `.env`、`.yaml`、`.toml`、`application.properties` 等通用配置文件；也没有统一的 Feature Flag / 开关中心。配置来源以 Unity 工程资产和 Luban 生成物为主。

---

## 一、运行时配置（ScriptableObject + Settings）

### 核心文件

| 路径 | 作用 |
|---|---|
| `UnityProject/Assets/TEngine/Runtime/Module/Settings/Settings.cs` | 全局 `Settings` 单例，持有 `AudioSetting`、`ProcedureSetting`、`UpdateSetting` 三个 ScriptableObject 引用，并提供静态访问器 |
| `UnityProject/Assets/TEngine/Runtime/Module/AudioModule/AudioSetting.cs` | 音频分组配置，标记 `[CreateAssetMenu("TEngine/AudioSetting")]` |
| `UnityProject/Assets/TEngine/Runtime/Module/AudioModule/AudioGroupConfig.cs` | 单个音轨组的数据结构（name/mute/volume/agentHelperCount/AudioType/audioRolloffMode/minDistance/maxDistance） |
| `UnityProject/Assets/TEngine/Runtime/Module/ProcedureModule/ProcedureSetting.cs` | 流程配置，声明 `availableProcedureTypeNames` 与 `entranceProcedureTypeName`，并在 `StartProcedure()` 中通过反射 `Utility.Assembly.GetType(...)` 动态注册流程 |
| `UnityProject/Assets/TEngine/Settings/AudioSetting.asset` | 已创建的 AudioSetting 资产，内含 Music/Sound/UISound/Voice 四组配置 |
| `UnityProject/Assets/TEngine/Settings/ProcedureSetting.asset` | 已创建的 ProcedureSetting 资产，入口流程为 `Procedure.ProcedureLaunch` |
| `UnityProject/Assets/TEngine/Settings/UpdateSetting.asset` | 热更新相关设置（DLL 列表、下载地址、WebGL 加载方式等） |
| `UnityProject/Assets/Editor/TEngineSettingsProvider/TEngineSettingsProvider.cs` | 在 Unity 菜单 `TEngine/Settings/TEngine UpdateSettings` 下提供可视化编辑界面，直接序列化 `Settings.UpdateSetting` 字段 |

### 架构约定

- `Settings` 是 `MonoBehaviour` 单例，通过 `Utility.Unity.FindObjectOfType<Settings>()` 查找；若编辑器模式下找不到实例，则通过 `AssetDatabase.FindAssets("t:UpdateSetting")` 反查资产路径加载。
- 每个模块的运行时配置都继承 `ScriptableObject`，并通过 `[CreateAssetMenu]` 暴露到 Unity 菜单，便于策划/美术在 Inspector 中编辑。
- `Settings.AudioSetting`、`Settings.ProcedureSetting`、`Settings.UpdateSetting` 三个静态属性是唯一的运行时访问入口，模块侧统一通过 `Settings.Xxx` 读取，而非直接引用 Asset。
- `ProcedureSetting.StartProcedure()` 在启动时根据字符串类型名反射创建流程实例，因此新增流程必须同时出现在 `availableProcedureTypeNames` 列表中，否则会在初始化时报错（见第 71–83 行）。这是被代码强校验的规则。
- `UpdateSetting` 在编辑器模式下的特殊处理：如果 `Settings.Instance` 为空，会直接通过 AssetDatabase 加载磁盘上的 `UpdateSetting.asset`，绕过 Scene 中的 MonoBehaviour 依赖，保证编辑器工具链可用。

### 使用方示例

- `GameEntry.cs` 调用 `Settings.ProcedureSetting.StartProcedure()` 作为应用启动入口。
- `AudioModule.cs` 在初始化时调用 `Initialize(Settings.AudioSetting.audioGroupConfigs)`。
- `ResourceModuleDriver.cs` 从 `Settings.UpdateSetting` 读取 HostServerURL、FallbackHostServerURL、LoadResWayWebGL 等。
- `ProcedureInitResources.cs`、`ProcedureLoadAssembly.cs` 同样通过 `Settings.UpdateSetting` 控制热更新行为。

---

## 二、游戏数据配置（Luban 管线）

### 核心文件

| 路径 | 作用 |
|---|---|
| `Configs/GameConfig/luban.conf` | Luban 主配置：定义 groups (c/s/e)、schemaFiles、targets (server/client/all)，topModule 为 `GameConfig` |
| `Configs/GameConfig/Datas/__tables__.xlsx` | 数据表清单（Excel） |
| `Configs/GameConfig/Datas/__beans__.xlsx` | Bean 定义清单 |
| `Configs/GameConfig/Datas/__enums__.xlsx` | 枚举定义清单 |
| `Configs/GameConfig/Datas/item.xlsx` | 示例数据表 |
| `Configs/GameConfig/Defines/builtin.xml` | 自定义 Bean 映射：将 `vector2/vector3/vector4/vector2int/vector3int` 映射到 `UnityEngine.Vector2/Vector3/Vector4/Vector2Int` 等类型，并通过 `ExternalTypeUtil.NewXxx` 构造 |
| `Configs/GameConfig/CustomTemplate/ConfigSystem.cs` | 运行时加载器：单例 `ConfigSystem.Instance.Tables`，通过 `IResourceModule.LoadAsset<TextAsset>` 读取 Luban 生成的二进制表 |
| `Configs/GameConfig/CustomTemplate/ExternalTypeUtil.cs` | 外部类型构造辅助（供 builtin.xml 中 Vector* 类型构造） |
| `UnityProject/Assets/TEngine/Editor/LubanTools/LubanTools.cs` | 编辑器菜单 `TEngine/Luban/转表 &X`，跨平台调用 `gen_code_bin_to_project_lazyload.(sh|bat)` |
| `Configs/GameConfig/gen_code_bin_to_project.bat/sh` | Luban 代码生成脚本（客户端+服务端） |
| `Configs/GameConfig/gen_code_bin_to_project_lazyload.bat/sh` | 懒加载模式的代码生成脚本 |
| `Configs/GameConfig/gen_code_bin_to_server.bat/sh` | 仅服务端代码生成脚本 |

### 架构约定

- 所有游戏数据源集中在 `Configs/GameConfig/Datas/*.xlsx`，由 Luban 按 `luban.conf` 中定义的 targets 生成多份 C# 代码（client/server/all）及二进制资源。
- `builtin.xml` 通过 `<mapper target="client" codeTarget="cs-bin,cs-simple-json,cs-newtonsoft-json">` 将 Unity 原生类型注入到 Luban 代码生成阶段，使生成的 C# 可以直接使用 `UnityEngine.Vector2` 等类型。
- 运行时通过 `ConfigSystem.Instance.Tables` 访问，内部委托给 `Tables` 构造函数传入的 `LoadByteBuf` 回调；该回调通过 `ModuleSystem.GetModule<IResourceModule>().LoadAsset<TextAsset>(file)` 从 YooAsset 资源系统中读取二进制表。
- 表格加载采用懒加载：首次访问 `Tables` 属性时才触发 `Load()`，避免启动时一次性加载全部数据。
- 编辑器侧通过 `LubanTools.ZhuanXiaoYi()` 一键执行生成脚本，自动根据平台选择 `.sh` 或 `.bat` 入口。

---

## 三、构建期配置（非运行时）

`UnityProject/Assets/TEngine/Editor/ReleaseTools/BuildConfig.cs` 提供纯 C# 类的构建期配置（BuildTarget、CompressOption、EncryptionType、PackageVersion、OutputRoot、MinimalPackage、EnableSharePackRule 等），用于 ReleaseTools 自动化打包，不参与运行时配置加载。

---

## 四、观察到的约束与规则

1. **新增流程类必须登记**：`ProcedureSetting.availableProcedureTypeNames` 数组包含的类型名会被反射实例化，遗漏会导致初始化失败（`ProcedureSetting.cs` 第 68–83 行显式报错）。
2. **入口流程必须匹配**：`entranceProcedureTypeName` 必须在 `availableProcedureTypeNames` 中出现，否则 `ProcedureSetting` 初始化后无法找到入口流程（第 91–95 行）。
3. **Luban 自定义类型需同步维护**：`builtin.xml` 中新增的 bean 需要同时在 `__beans__.xlsx` 中声明，且 `CustomTemplate/ExternalTypeUtil.cs` 需提供对应构造方法，否则代码生成阶段会报类型解析错误。
4. **Settings 资产命名规范**：各 ScriptableObject 通过 `[CreateAssetMenu(menuName = "TEngine/...")]` 统一挂载到 TEngine 菜单下，资产文件名与类名一致（如 `AudioSetting.asset`、`ProcedureSetting.asset`）。
5. **UpdateSetting 在编辑器可独立加载**：`Settings.UpdateSetting` 在编辑器模式下允许无 Scene 依赖地通过 AssetDatabase 加载，这是编辑器工具链的硬性要求。
6. **配置加载不持久化**：当前 `AudioSetting`、`ProcedureSetting`、`UpdateSetting` 均为只读 ScriptableObject，没有运行时写回磁盘的逻辑；运行时修改不会持久化。
7. **不存在环境变量/JSON/TOML 配置层**：未发现任何 `Environment.GetEnvironmentVariable`、`JsonUtility` 反序列化外部 JSON、`ConfigurationBuilder` 等通用配置框架的使用。

---

## 五、关键文件清单

- `UnityProject/Assets/TEngine/Runtime/Module/Settings/Settings.cs`
- `UnityProject/Assets/TEngine/Runtime/Module/AudioModule/AudioSetting.cs`
- `UnityProject/Assets/TEngine/Runtime/Module/AudioModule/AudioGroupConfig.cs`
- `UnityProject/Assets/TEngine/Runtime/Module/ProcedureModule/ProcedureSetting.cs`
- `UnityProject/Assets/TEngine/Settings/AudioSetting.asset`
- `UnityProject/Assets/TEngine/Settings/ProcedureSetting.asset`
- `UnityProject/Assets/TEngine/Settings/UpdateSetting.asset`
- `UnityProject/Assets/Editor/TEngineSettingsProvider/TEngineSettingsProvider.cs`
- `Configs/GameConfig/luban.conf`
- `Configs/GameConfig/Defines/builtin.xml`
- `Configs/GameConfig/CustomTemplate/ConfigSystem.cs`
- `Configs/GameConfig/CustomTemplate/ExternalTypeUtil.cs`
- `UnityProject/Assets/TEngine/Editor/LubanTools/LubanTools.cs`
- `Configs/GameConfig/gen_code_bin_to_project_lazyload.sh`
- `Configs/GameConfig/gen_code_bin_to_project_lazyload.bat`
- `UnityProject/Assets/TEngine/Editor/ReleaseTools/BuildConfig.cs`
