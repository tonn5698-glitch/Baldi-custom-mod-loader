BALDI MOD LOADER - INITIAL FOUNDATION
======================================

Runtime mods folder:
Application.persistentDataPath/Mods

The exact Android path is printed in the Unity log:
[BaldiModLoader] Mods folder: ...

Each mod goes in its own folder:
Mods/
  example-mod/
    mod.json
    Assets/
      icon.png
    Levels/
      example.json
    Bundles/
      example-assets
    Scripts/
      ExampleMod.dll

Example mod.json:
{
  "id": "example-mod",
  "name": "Example Mod",
  "version": "1.0.0",
  "description": "Example mod",
  "enabled": true,
  "scriptAssemblies": ["Scripts/ExampleMod.dll"],
  "assetBundles": ["Bundles/example-assets"],
  "levels": ["Levels/example.json"]
}

A managed script DLL must implement IBaldiMod:
public class ExampleMod : IBaldiMod {
  public void Initialize(ModAPI api) {
    api.Log("Hello from my mod!");
  }
}

Level metadata example:
{
  "id": "my-level",
  "name": "My Level",
  "description": "Example level",
  "sceneBundle": "Bundles/my-levels",
  "sceneName": "MyCustomScene"
}

Current foundation:
- scans enabled mod folders and reads mod.json
- loads listed AssetBundles
- loads managed script DLL entry points implementing IBaldiMod
- exposes API for reading text, loading PNG/JPG textures, bundles and level metadata

Important limits:
- This is not yet Psych Engine-style Lua support.
- DLL scripts need a compatible Mono scripting backend; dynamic assembly loading does not work on every IL2CPP build.
- A custom Unity scene must be built into an AssetBundle for the same target platform (Android bundles for Android).
- There is not yet a level-select screen or automatic replacement of all original game assets.
- DLL mods execute code inside the game. Only use scripts from sources you trust.

CUSTOM NOTEBOOK QUESTIONS
=========================

A mod can include a questions.json file beside mod.json. Example:
{
  "questions": [
    { "text": "CUSTOM QUESTION 1:\\nWhat is 2 + 2?", "answer": "4" },
    { "text": "CUSTOM QUESTION 2:\\nWhat is 5 + 3?", "answer": "8" },
    { "text": "CUSTOM QUESTION 3:\\nWhat is 10 - 4?", "answer": "6" }
  ]
}

The first three valid entries replace Notebook questions 1, 2 and 3 in order.
The answer must match the entered text exactly (for example, answer "04" is not the same as "4").
If a mod does not provide enough entries, the remaining questions use the game's original logic.
Put the mod folder under Application.persistentDataPath/Mods and keep its mod.json enabled.
