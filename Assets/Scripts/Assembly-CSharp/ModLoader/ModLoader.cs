using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModLoader : MonoBehaviour
{
    public static ModLoader Instance { get; private set; }
    public static string ModsPath { get; private set; }
    public readonly List<LoadedMod> LoadedMods = new List<LoadedMod>();
    private readonly Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        GameObject host = new GameObject("BaldiModLoader");
        DontDestroyOnLoad(host);
        host.AddComponent<ModLoader>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ModsPath = Path.Combine(Application.persistentDataPath, "Mods");
        Directory.CreateDirectory(ModsPath);
        ScanMods();
        Debug.Log("[BaldiModLoader] Mods folder: " + ModsPath);
    }

    public void ScanMods()
    {
        foreach (string folder in Directory.GetDirectories(ModsPath))
        {
            string manifestPath = Path.Combine(folder, "mod.json");
            if (!File.Exists(manifestPath)) continue;
            try
            {
                ModManifest manifest = JsonUtility.FromJson<ModManifest>(File.ReadAllText(manifestPath));
                if (manifest == null || string.IsNullOrEmpty(manifest.id)) continue;
                if (!manifest.enabled) { Debug.Log("[BaldiModLoader] Disabled: " + manifest.id); continue; }
                if (LoadedMods.Exists(m => m.Manifest.id == manifest.id))
                {
                    Debug.LogWarning("[BaldiModLoader] Duplicate mod id: " + manifest.id);
                    continue;
                }
                LoadedMod mod = new LoadedMod(folder, manifest);
                LoadedMods.Add(mod);
                LoadBundles(mod);
                LoadScripts(mod);
                Debug.Log("[BaldiModLoader] Loaded mod: " + manifest.id + " (" + manifest.version + ")");
            }
            catch (Exception ex) { Debug.LogError("[BaldiModLoader] Failed: " + folder + "\n" + ex); }
        }
    }

    private void LoadBundles(LoadedMod mod)
    {
        if (mod.Manifest.assetBundles == null) return;
        foreach (string relative in mod.Manifest.assetBundles)
        {
            string path = SafePath(mod.Folder, relative);
            if (path == null || !File.Exists(path)) { Debug.LogWarning("[BaldiModLoader] Missing bundle: " + relative); continue; }
            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null) { Debug.LogWarning("[BaldiModLoader] Invalid bundle: " + path); continue; }
            bundles[mod.Manifest.id + "/" + relative] = bundle;
        }
    }

    private void LoadScripts(LoadedMod mod)
    {
        if (mod.Manifest.scriptAssemblies == null) return;
        foreach (string relative in mod.Manifest.scriptAssemblies)
        {
            string path = SafePath(mod.Folder, relative);
            if (path == null || !File.Exists(path)) { Debug.LogWarning("[BaldiModLoader] Missing script DLL: " + relative); continue; }
            try
            {
                Assembly assembly = Assembly.Load(File.ReadAllBytes(path));
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsAbstract || type.IsInterface || !typeof(IBaldiMod).IsAssignableFrom(type)) continue;
                    IBaldiMod plugin = (IBaldiMod)Activator.CreateInstance(type);
                    plugin.Initialize(new ModAPI(mod));
                    Debug.Log("[BaldiModLoader] Started script: " + type.FullName);
                }
            }
            catch (Exception ex) { Debug.LogError("[BaldiModLoader] Script failed: " + path + "\n" + ex); }
        }
    }

    public string ReadText(string modId, string relativePath)
    {
        LoadedMod mod = FindMod(modId);
        if (mod == null) return null;
        string path = SafePath(mod.Folder, relativePath);
        return path != null && File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public Texture2D LoadTexture(string modId, string relativePath)
    {
        LoadedMod mod = FindMod(modId);
        if (mod == null) return null;
        string path = SafePath(mod.Folder, relativePath);
        if (path == null || !File.Exists(path)) return null;
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(File.ReadAllBytes(path))) { Destroy(texture); return null; }
        texture.name = Path.GetFileNameWithoutExtension(path);
        return texture;
    }

    public AssetBundle GetBundle(string modId, string relativePath)
    {
        AssetBundle bundle;
        bundles.TryGetValue(modId + "/" + relativePath, out bundle);
        return bundle;
    }

    public List<ModLevelInfo> GetLevels(string modId)
    {
        List<ModLevelInfo> result = new List<ModLevelInfo>();
        LoadedMod mod = FindMod(modId);
        if (mod == null || mod.Manifest.levels == null) return result;
        foreach (string relative in mod.Manifest.levels)
        {
            string json = ReadText(modId, relative);
            if (string.IsNullOrEmpty(json)) continue;
            try
            {
                ModLevelInfo level = JsonUtility.FromJson<ModLevelInfo>(json);
                if (level != null && !string.IsNullOrEmpty(level.id)) result.Add(level);
            }
            catch (Exception ex) { Debug.LogWarning("[BaldiModLoader] Bad level " + relative + ": " + ex.Message); }
        }
        return result;
    }

    public bool LoadLevelScene(string modId, ModLevelInfo level)
    {
        if (level == null || string.IsNullOrEmpty(level.sceneBundle) || string.IsNullOrEmpty(level.sceneName)) return false;
        AssetBundle bundle = GetBundle(modId, level.sceneBundle);
        if (bundle == null) { Debug.LogWarning("[BaldiModLoader] Level bundle not loaded: " + level.sceneBundle); return false; }
        try
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(level.sceneName);
            return operation != null;
        }
        catch (Exception ex)
        {
            Debug.LogError("[BaldiModLoader] Could not load level " + level.id + ": " + ex.Message);
            return false;
        }
    }

    private LoadedMod FindMod(string id)
    {
        return LoadedMods.Find(m => m.Manifest.id == id);
    }

    private static string SafePath(string root, string relative)
    {
        if (string.IsNullOrEmpty(relative) || Path.IsPathRooted(relative)) return null;
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string full = Path.GetFullPath(Path.Combine(root, relative));
        return full.StartsWith(fullRoot, StringComparison.Ordinal) ? full : null;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        foreach (AssetBundle bundle in bundles.Values) if (bundle != null) bundle.Unload(false);
        bundles.Clear();
        Instance = null;
    }
}

[Serializable]
public class ModManifest
{
    public string id;
    public string name;
    public string version = "1.0.0";
    public string description;
    public bool enabled = true;
    public string[] scriptAssemblies;
    public string[] assetBundles;
    public string[] levels;
}

public class LoadedMod
{
    public readonly string Folder;
    public readonly ModManifest Manifest;
    public LoadedMod(string folder, ModManifest manifest) { Folder = folder; Manifest = manifest; }
}

[Serializable]
public class ModLevelInfo
{
    public string id;
    public string name;
    public string description;
    public string sceneBundle;
    public string sceneName;
}
