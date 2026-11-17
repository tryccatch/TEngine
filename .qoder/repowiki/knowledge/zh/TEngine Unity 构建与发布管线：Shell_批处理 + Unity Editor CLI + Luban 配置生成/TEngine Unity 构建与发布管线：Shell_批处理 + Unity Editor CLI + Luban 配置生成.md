---
kind: build_system
name: TEngine Unity 构建与发布管线：Shell/批处理 + Unity Editor CLI + Luban 配置生成
slug: build_system
category: build_system
scope:
    - '**'
---

## 1. 总体方案

仓库采用「外部 Shell/批处理脚本 → Unity 命令行 → Unity Editor C# 构建工具」的三层流水线，辅以 Luban 数据配置代码生成。没有 Makefile、Dockerfile 或 CI 配置文件；所有自动化入口均为 `.sh` / `.bat`。

- **Android 打包**：`BuildCLI/build_android.sh`（macOS/Linux）和 `BuildCLI/build_android.bat`（Windows）通过 Unity 命令行 `-executeMethod TEngine.ReleaseTools.AutomationBuildAndroid` 启动 Unity 编辑器并调用内部静态方法完成打包。
- **Luban 配置生成**：`Configs/GameConfig/gen_code_bin_to_project.{sh,bat}` 调用 `dotnet Tools/Luban/Luban.dll`，将 Excel 表编译为 C# 客户端代码与二进制资源。
- **Luban 自身构建**：`Tools/build-luban.{sh,bat}` 从 GitHub 拉取 luban 源码并 `dotnet build -c Release` 输出到 `Tools/Luban/`。
- **TEngine 框架更新**：`update-tengine.bat` 使用 git sparse-checkout 仅克隆 `UnityProject/Packages/YooAsset` 与 `UnityProject/Assets/TEngine` 两个子目录，再用 robocopy `/MIR` 同步到工程。

## 2. 关键文件

| 路径 | 作用 |
|---|---|
| `BuildCLI/build_android.sh` / `build_android.bat` | Android 平台一键打包入口 |
| `BuildCLI/path_define.sh` / `path_define.bat` | 定义 `WORKSPACE`、`UNITYEDITOR_PATH`、日志路径等环境变量 |
| `UnityProject/Assets/TEngine/Editor/ReleaseTools/ReleaseTools.cs` | 核心构建逻辑：AB 打包、Player 构建、最小包后处理、平台映射 |
| `UnityProject/Assets/TEngine/Editor/ReleaseTools/BuildPipelineWindow.cs` | Unity 菜单入口（`TEngine/Build/*`），调用 `ReleaseTools.BuildWithConfig` |
| `UnityProject/Assets/TEngine/Editor/ReleaseTools/BuildConfig.cs` | 构建参数对象（BuildTarget、OutputRoot、PackageVersion 等） |
| `UnityProject/Assets/TEngine/Editor/Utility/CommandLineReader.cs` | 解析 `-CustomArgs:Key=Value` 参数，供非交互模式使用 |
| `Configs/GameConfig/luban.conf` | Luban 目标定义（client/server/all，topModule=`GameConfig`） |
| `Configs/GameConfig/gen_code_bin_to_project.{sh,bat}` | 执行 Luban 生成 C# 代码与二进制表 |
| `Tools/build-luban.{sh,bat}` | 拉取并编译 Luban 源码 |
| `update-tengine.bat` | 增量更新 TEngine 框架与 YooAsset |

## 3. 架构与约定

### 3.1 构建入口统一为 Unity Editor 静态方法
所有可被 Unity 命令行调用的入口均位于 `TEngine.ReleaseTools` 命名空间下，以 `public static void` 暴露：
- `AutomationBuildAndroid()` — Android 一键打包
- `AutomationBuildIOS()` — iOS 一键打包
- `AutomationBuild()` — Windows 一键打包
- `BuildDll()` / `BuildAssetBundle()` — 旧版 CLI 兼容入口

这些方法最终都走 `BuildWithConfig(BuildConfig, bool buildPlayer)`，该方法是唯一编排点：
1. 可选编译热更 DLL（`BuildDLLCommand.BuildAndCopyDlls()`）
2. `AssetDatabase.Refresh()`
3. 调用 `BuildInternalWithConfig(config)` 通过 YooAsset 构建 AssetBundle
4. 可选执行 `ProcessMinimalPackage()` 清理 StreamingAssets 中未保留 tag 的 .bundle
5. 可选调用 `BuildImp()` 构建 Player 应用

### 3.2 平台选择策略
`GetBuildTarget(string platform)` 将字符串平台名映射到 Unity `BuildTarget`：Android、iOS、Windows、MacOS、Linux、WebGL、Switch、PS4、PS5。`BuildCLI` 中的 shell/bat 脚本固定传入 `Android`。

### 3.3 AssetBundle 构建管线
`BuildInternalWithConfig` 根据 `config.BuildPipeline` 在 `BuiltinBuildPipeline` 与 `ScriptableBuildPipeline` 之间切换，默认使用 ScriptableBuildPipeline。包名固定为 `DefaultPackage`，文件名风格由 `BuildConfig.FileNameStyle` 控制（默认 `BundleName_HashName`）。压缩选项、加密服务、依赖 DB、共享打包规则等全部通过 `BuildParameters` 注入。

### 3.4 最小包模式
`ProcessMinimalPackage` 读取 YooAsset 生成的 BuildReport JSON，按用户配置的 retainTags（支持中英文逗号分隔）筛选需要保留的 bundle，删除 StreamingAssets 下其余 .bundle 并递归清理空目录。

### 3.5 Luban 配置生成流程
`gen_code_bin_to_project.sh` 设置环境变量：
- `WORKSPACE` → 项目根
- `LUBAN_DLL` → `Tools/Luban/Luban.dll`
- `CONF_ROOT` → `Configs/GameConfig`
- `DATA_OUTPATH` → `UnityProject/Assets/AssetRaw/Configs/bytes/`
- `CODE_OUTPATH` → `UnityProject/Assets/GameScripts/HotFix/GameProto/GameConfig/`

先复制 `CustomTemplate/ConfigSystem.cs` 与 `ExternalTypeUtil.cs` 到生成目录，再执行：
```
dotnet Luban.dll -t client -c cs-bin -d bin --conf luban.conf \
  -x code.lineEnding=crlf \
  -x outputCodeDir=... \
  -x outputDataDir=...
```
`luban.conf` 定义三个 group（c/s/e）、schemaFiles（Defines/Datas/__tables__.xlsx/__beans__.xlsx/__enums__.xlsx）以及 targets（server/client/all，topModule=`GameConfig`）。

### 3.6 环境隔离与跨平台
- `path_define.sh` 与 `path_define.bat` 分别维护 macOS 与 Windows 的 Unity 安装路径与工作区路径，脚本开头 `cd "$(dirname "$0")"` 确保相对路径正确。
- `build-luban.sh` 使用 `set -e` 严格模式，失败即退出；`build-luban.bat` 显式检查 `errorlevel`。
- Luban 构建脚本会检测 `../../luban/.git`，不存在则 clone，存在则 `git fetch` + 比较 HEAD 决定是否 pull，避免重复下载。

### 3.7 TEngine 框架更新机制
`update-tengine.bat` 强制要求工程布局存在 `UnityProject/Packages/YooAsset` 与 `UnityProject/Assets/TEngine`，否则直接 abort。使用 `git clone --no-checkout --depth=1 --filter=blob:none` 配合 `sparse-checkout set` 只拉取所需目录，再用 `robocopy /MIR` 镜像同步，最后清理临时目录。

## 4. 约定与约束

- **Android 打包必须通过 `AutomationBuildAndroid` 入口**：`BuildCLI/build_android.{sh,bat}` 硬编码调用该方法，不直接调用 `BuildWithConfig`。
- **Unity 版本锁定**：`path_define.sh` 与 `path_define.bat` 中 Unity 路径指向 `2021.3.20f1c1`，这是当前工程绑定的编辑器版本。
- **包名固定**：AssetBundle 构建始终使用 `PackageName = "DefaultPackage"`（见 `BuildInternalWithConfig` 第 201 行）。
- **版本号生成**：默认 package version 由 `DateTime.Now.ToString("yyyy-MM-dd") + "-" + (Hour*60+Minute)` 生成（见 `GetBuildPackageVersion`）。
- **CRLF 换行**：Luban 生成 C# 代码时强制 `-x code.lineEnding=crlf`，适配 Windows 环境。
- **Luban 源码位置**：`Tools/build-luban.{sh,bat}` 期望 luban 源码位于仓库根的上两级目录（`../../luban`），不在仓库内跟踪。
- **TEngine 更新范围**：`update-tengine.bat` 仅同步 `YooAsset` 与 `TEngine` 两个目录，其他内容不受影响。
- **无 CI/容器化**：仓库未发现任何 `.github/workflows`、`.gitlab-ci.yml`、`Jenkinsfile`、`Dockerfile` 或 `Makefile`；所有自动化均由本地 shell/bat 触发。

## 5. 缺失项说明

- 无 Docker 镜像或多平台交叉编译脚本（仅 Android 打包，且依赖本机 Unity 安装）。
- 无持续集成配置（CI pipeline）。
- 无统一的版本发布标签或 release 流程脚本（版本号由运行时时间戳生成）。
- 无 Makefile 或 npm/yarn/pip 等语言级包管理构建入口（Luban 通过 dotnet 直接构建）。