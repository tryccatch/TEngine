# Hot-update MonoBehaviour

Doc: `guide/features/monobehaviour.md` in the HybridCLR documentation. Registering the assembly is [`hotupdate-assembly`](hotupdate-assembly.md).

`AddComponent<T>()` and `AddComponent(Type)` work after that dll has been `Assembly.Load`ed.

## Scripts and ScriptableObjects on assets

Deserialization needs all of the following:

1. The dll that contains the script is already loaded.
2. The asset is loaded from an AssetBundle (Addressables and other AB-based systems count). A script on a Resources asset or a scene that ships in the player becomes **Script Missing**. Building that asset into an AssetBundle and then placing the bundle in Resources is fine.
3. The dll name is in the generated `ScriptingAssemblies.json`. Hot-update dlls are removed by `IFilterBuildAssemblies`, so they are not listed on their own. `PatchScriptingAssemblyList` writes names from `hotUpdateAssemblyDefinitions`, `hotUpdateAssemblies`, and `preserveHotUpdateAssemblies`. Unlisted assemblies are not restored.

The hot-update dll itself may live in an AssetBundle, as a raw file, or in an encrypted package. `Assembly.Load` it **before** loading assets that contain the scripts.

Do not rename these assemblies after release. The assembly list is baked into the player and cannot be hot-updated.

## Known limits

- `GetComponent(string)` cannot find a hot-update script that was restored from a hot-update asset. Use `GetComponent<T>()` or `GetComponent(typeof(T))`. Components added with `AddComponent` in code are not affected.
- Do not disable TypeTree when building AssetBundles. Script signature checks then fail and a normal load fails. If TypeTree must be off, call `AssetBundleCreateRequest.SetEnableCompatibilityChecks(false)` through reflection and keep code and asset versions matched. A mismatch can crash.
