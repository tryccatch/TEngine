---
kind: dependency_management
name: Unity Package Manager + npm 双轨依赖管理
slug: dependency_management
category: dependency_management
scope:
    - '**'
---

## 1. 使用的系统/方法

仓库采用 **Unity Package Manager (UPM)** 作为 Unity 工程的主要依赖管理工具，辅以 **npm** 管理 `Tools/FileServer` 中的 Node.js 静态资源服务器。没有发现 Go (`go.mod`)、Python (`requirements.txt`/`pyproject.toml`)、C# NuGet 包清单或私有 registry 配置。

- Unity 侧：通过 `UnityProject/Packages/manifest.json` 声明依赖，由 `UnityProject/Packages/packages-lock.json` 锁定解析后的完整依赖树与精确版本。
- Node 侧：`Tools/FileServer/package.json` 声明依赖，未检出 `package-lock.json`（可能由 CI 生成）。无 `.npmrc`、`GOBIN`、`GOPRIVATE` 等私有源配置。

## 2. 关键文件

- `UnityProject/Packages/manifest.json` — UPM 顶层依赖声明入口
- `UnityProject/Packages/packages-lock.json` — UPM 生成的锁文件，记录每个包的 `source`（`git` / `embedded` / `registry` / `builtin`）、`version`、`hash`、`url` 和依赖图
- `UnityProject/Packages/UniTask/package.json` — 内嵌的 UniTask 包元数据（`com.cysharp.unitask`）
- `UnityProject/Packages/YooAsset/package.json` — 内嵌的 YooAsset 包元数据（`com.tuyoogame.yooasset`），声明对 `com.unity.scriptablebuildpipeline` 及若干 `com.unity.modules.*` 的依赖
- `Tools/FileServer/package.json` — Node 静态服务器的 npm 依赖声明

## 3. 架构与约定

### 3.1 UPM 依赖来源分四类

| source | 含义 | 示例 |
|---|---|---|
| `git` | 直接指向 Git 仓库 URL，带 commit hash | `com.code-philosophy.hybridclr` → `https://github.com/focus-creative-games/hybridclr_unity.git`；`com.zzz.kit` → `https://github.com/tryccatch/com.zzz.kit.git` |
| `embedded` | 本地目录下的 UPM 包（`Packages/<name>/`） | `com.cysharp.unitask` → `file:UniTask`；`com.tuyoogame.yooasset` → `file:YooAsset` |
| `registry` | Unity 官方包注册中心 `https://packages.unity.com` | `com.unity.feature.2d`、`com.unity.nuget.newtonsoft-json`、`com.unity.pipeline` 等 |
| `builtin` | Unity 内置模块，无需下载 | `com.unity.modules.*`、`com.unity.ugui`、`com.unity.test-framework` 等 |

### 3.2 核心第三方库组织方式

- **HybridCLR**（热更新运行时）：以 git 源引入，commit hash 锁定在 `7e74e596...`。
- **ZZZ Kit**：以 git 源引入，commit hash 锁定在 `6640e92c...`。
- **UniTask**：以 `file:UniTask` 形式内联到工程，版本号 `2.5.11`，要求 Unity ≥ 2018.4。
- **YooAsset**：以 `file:YooAsset` 形式内联，版本号 `2.3.17`，要求 Unity ≥ 2019.4，并显式声明依赖 `com.unity.scriptablebuildpipeline` 与 `assetbundle`/`unitywebrequest` 模块。
- **Unity 官方包**：`com.unity.feature.2d@2.0.2`、`com.unity.nuget.newtonsoft-json@3.2.2`、`com.unity.pipeline@1.0.0`、`com.unity.ugui@2.0.0`、IDE 插件 Rider/VisualStudio。
- **Unity 内置模块**：大量 `com.unity.modules.*` 被显式声明为依赖（如 `animation`、`audio`、`physics`、`ui`、`unitywebrequest*`、`video`、`wind` 等），而非仅隐式使用。

### 3.3 Node 工具链

`Tools/FileServer` 是一个独立的 npm 项目，提供 `server` CLI（`bin.server = index.js`），运行依赖 `chalk`、`handlebars`、`mime`、`open`、`pem`、`yargs`，开发期依赖 `supervisor`。该工具用于启动本地静态资源服务器，供 TEngine 热更流程拉取资源。

## 4. 约定与约束

- **Git 源依赖必须带 hash**：`packages-lock.json` 中所有 `source: "git"` 的条目都附带 `hash` 字段，确保可复现构建。
- **内嵌包（embedded）不经过网络**：`UniTask` 与 `YooAsset` 以 `file:` 路径引用，源码随仓库一起提交，避免外部源不可用导致构建失败。
- **Unity 版本门槛**：内嵌包的 `package.json` 通过 `unity` 字段声明最低支持的 Unity 版本（UniTask 2018.4，YooAsset 2019.4）。
- **无自定义 registry/GOPRIVATE**：未发现 `.npmrc`、`PackageSources`、`nuget.config`、`go.mod` 等文件，所有非内嵌依赖均从 Unity 官方 registry 或公开 GitHub 仓库获取。
- **Node 依赖未加锁**：`Tools/FileServer/` 下未见 `package-lock.json`，npm 安装时可能产生非确定性版本解析（取决于 npm 缓存与语义版本范围 `^x.y.z`）。
- **无 vendored C#/NuGet 包**：除 `UnityProject/Assets/TEngine/Libraries/System.Buffers.dll` 与 `System.Runtime.CompilerServices.Unsafe.dll` 外，未见其他 vendored DLL；其余 C# 依赖通过 UPM 或 Unity 内置模块解决。