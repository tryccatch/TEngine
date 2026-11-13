---
name: hybridclr
description: 解释 HybridCLR 包自身机制与排障：安装升级与 Installer、HybridCLRSettings 字段、AOT 泛型补充元数据、代码裁剪/LinkXml、热更程序集拆分/编译/加载、AB 资产脚本还原、MonoPInvokeCallback wrapper 预留、脚本语言配合。TEngine 业务热更开发使用 tengine-dev；仅在涉及 HybridCLR 配置、生成物或上述运行错误时加载。
---

# HybridCLR 机制与排障

内容来自 HybridCLR 包 v9.1.0（focus-creative-games/hybridclr_unity）自带的官方 `Data~/skills`，
已转为 references 并保留其交叉链接。
已按安装包 9.1.0 源码核实的路径差异：`SettingsUtil.HybridCLRDataDir = {project}/Library/HybridCLR`，
上游原文中的 `HybridCLRData/` 已全部改为该路径（configure/setup/aot-generic/hotupdate-assembly）。
示例与结论仍须以当前包源码和包内 `Data~/hybridclr_version.json` 复核。
`Library/PackageCache` 路径会随 UPM 解析重建，不是稳定引用目标；升级包时对照新版本 `Data~/skills` 同步本技能并更新版本标注。

## 路由

按任务或错误症状只读相关主题，其余由主题内部链接按需展开：

| 主题 | 触发情形 |
|---|---|
| [setup](references/setup.md) | 安装/升级、Installer 失败、找不到 git、玩家仍用原生 IL2CPP |
| [configure](references/configure.md) | HybridCLR/Settings、`HybridCLRSettings.asset` 字段、构建检查影响 |
| [aot-generic](references/aot-generic.md) | `AOT generic method not instantiated in aot module`、补充元数据、SuperSet/Consistent |
| [avoid-code-strip](references/avoid-code-strip.md) | `TypeLoadException`、非泛型 `MissingMethodException`、Generate/LinkXml |
| [hotupdate-assembly](references/hotupdate-assembly.md) | 程序集拆分、CompileDll、Assembly.Load 顺序、重复加载 |
| [monobehaviour](references/monobehaviour.md) | Script Missing、AB/预制体挂热更脚本、GetComponent(string) |
| [monopinvokecallback](references/monopinvokecallback.md) | ReversePInvokeWrapper 预留、原生回调崩溃、参数 marshal |
| [script-language](references/script-language.md) | 与 xlua/tolua/puerts/ZLua/ZenTS 配合 |

## 与本仓库的边界

- 热更业务代码（`Assets/GameScripts/HotFix`、界面、事件、资源）按 [tengine-dev](../tengine-dev/SKILL.md) 执行；
  实际 Editor、生成命令与构建验证通过 [unity-cli](../unity-cli/SKILL.md) 操作。
- `Library/HybridCLR/`（上游文档所称 `HybridCLRData/`）、Installer 合并出的 libil2cpp 与生成代码属于产物；修改前追溯设置与模板源，
  不把包旧文档结论当作当前 API 保证。
