---
kind: external_dependency
name: ZZZ Kit 安装器（com.zzz.kit）
slug: zzz-kit-安装器-com-zzz-kit
category: external_dependency
scope:
    - '**'
---

### 身份与角色
第三方 Unity Package Manager 安装器，通过 git URL `https://github.com/tryccatch/com.zzz.kit.git` 引入，负责自动安装项目依赖包（包括 `com.unity.pipeline`）。

### 已知行为
- 该行为与嵌入式 pipeline 包共存时不可绕过，属于安装器自身逻辑限制。

### 集成点
仅声明于 `Packages/manifest.json`，具体安装逻辑位于远程包内。