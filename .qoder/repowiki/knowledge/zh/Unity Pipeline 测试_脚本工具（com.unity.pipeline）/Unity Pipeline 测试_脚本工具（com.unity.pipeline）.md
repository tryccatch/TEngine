---
kind: external_dependency
name: Unity Pipeline 测试/脚本工具（com.unity.pipeline）
slug: unity-pipeline-测试-脚本工具-com-unity-pipeline
category: external_dependency
scope:
    - '**'
---

### 身份与角色
Unity 官方测试与脚本执行工具包，提供 `test run`、`script eval`、`script eval_file`、`capture screenshot` 等命令，由 `.codex` 工作流驱动自动化验证。

### 本地内嵌包现状
- 与官方 registry 的差异：本地 CHANGELOG 多出一节 `### Compatibility`，且移除了 `.signature`、`.attestation.p7m`、`CLAUDE.md` 等签名/文档文件。

### 升级契约影响
- `script eval` 直接返回求值结果（不再包裹 `{output, diagnostics, success, result}`），失败以 exit code 6 判断。
- `test run` / `test status` 字段改 camelCase（`summary.total`、`results[].fullName`），默认 editor 模式，`mode=all` 需传 `--async-tests true`。
- batch 超时 exit 14，所有命令改用 exit code 报错。

### 方向

verify exact API/params against official docs