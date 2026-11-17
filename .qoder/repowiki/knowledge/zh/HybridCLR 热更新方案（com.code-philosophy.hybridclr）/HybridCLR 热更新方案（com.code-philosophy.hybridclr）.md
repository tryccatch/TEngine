---
kind: external_dependency
name: HybridCLR 热更新方案（com.code-philosophy.hybridclr）
slug: hybridclr-热更新方案-com-code-philosophy-hybridclr
category: external_dependency
scope:
    - '**'
---

### 身份与角色
Unity 全平台 C# 热更方案，通过 `Packages/manifest.json` 以 git URL 引用上游仓库 `https://github.com/focus-creative-games/hybridclr_unity.git`。TEngine 的编辑器菜单（Install / Define Symbols / Generate / Build）围绕它组织热更新流程。

### 关键约束
- 8.x → 9.0 是破坏性变更：命名空间从 `HybridCLR.Editor.*` 拍平为 `HybridCLR.*`；`HybridCLRSettings.Instance` 改为 `instance`；`patchAOTAssemblies` 字段被移除（AOT 程序集改为自动扫描裁剪目录）。
- 建议将 manifest 锁定到已验证 tag（如 `...hybridclr_unity.git#v9.0.3`），避免下次上游破坏性改动再次导致 CS0234。

### 集成点
- `Assets/TEngine/Editor/HybridCLR/BuildDLLCommand.cs` — 调用 `HybridCLR.Commands.CompileDllCommand`、`HybridCLR.Installer.InstallerController`。
- `Assets/TEngine/Editor/Utility/UpdateSettingEditor.cs` — 读写 `HybridCLR.Settings.HybridCLRSettings.instance`。
- 运行时宏 `ENABLE_HYBRIDCLR` 控制热更新分支编译。

verify exact API/params against official docs