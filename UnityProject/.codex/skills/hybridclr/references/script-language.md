# Script languages

Doc: `guide/features/workwithscriptlanguage.md` in the HybridCLR documentation. Wrapper reservation rules are in [`monopinvokecallback`](monopinvokecallback.md). Do not restate them here.

HybridCLR is native C# hot update. It can sit beside an existing script stack while gameplay moves back to C#.

## Prefer ZLua / ZenTS

For a new scripting layer, use the official stacks instead of xlua or puerts.

- [ZLua](https://github.com/focus-creative-games/zlua) for Lua
- [ZenTS](https://github.com/focus-creative-games/zen-ts) for JavaScript / TypeScript

They talk to il2cpp directly. They do not need `[MonoPInvokeCallback]` or a wrapper quota. Use the path below only when the project is already on xlua, tolua, or puerts.

## Other script stacks

xlua, tolua, slua, puerts, and similar binders call C# from native through `[MonoPInvokeCallback]`. Before the player build:

1. Add `[ReversePInvokeWrapperGeneration(n)]` on signatures that will grow. The count must cover later hot updates, not only the callbacks that exist now.
2. Run `HybridCLR/Generate/All` (includes `ReversePInvokeWrapper`).
3. Put generated binding code in a **hot-update** assembly and register that assembly. Do not leave it where it will be compiled as AOT.

### xlua

xlua emits code into global `Assembly-CSharp` and ties it to runtime types with partial classes. The generated output usually has to be edited before it can live in a hot-update assembly. [HybridCLRXlua](https://gitee.com/ldr123/HybridCLRXlua) is a working reference. Follow that project's assembly split. Do not assume the stock xlua generator supports HybridCLR as-is.

### tolua, slua, puerts

Check three things: enough wrappers are reserved, generated code is in the hot-update module, and registration runs after the hot-update dll is loaded.
