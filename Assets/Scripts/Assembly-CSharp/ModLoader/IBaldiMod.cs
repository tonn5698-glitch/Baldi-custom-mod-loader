public interface IBaldiMod
{
    void Initialize(ModAPI api);
}

public class ModAPI
{
    private readonly LoadedMod mod;
    public string ModId { get { return mod.Manifest.id; } }
    public string ModName { get { return mod.Manifest.name; } }

    public ModAPI(LoadedMod loadedMod) { mod = loadedMod; }

    public void Log(string message)
    {
        UnityEngine.Debug.Log("[Mod:" + ModId + "] " + message);
    }

    public string ReadText(string relativePath)
    {
        return ModLoader.Instance == null ? null : ModLoader.Instance.ReadText(ModId, relativePath);
    }

    public UnityEngine.Texture2D LoadTexture(string relativePath)
    {
        return ModLoader.Instance == null ? null : ModLoader.Instance.LoadTexture(ModId, relativePath);
    }

    public UnityEngine.AssetBundle GetAssetBundle(string relativePath)
    {
        return ModLoader.Instance == null ? null : ModLoader.Instance.GetBundle(ModId, relativePath);
    }

    public System.Collections.Generic.List<ModLevelInfo> GetLevels()
    {
        return ModLoader.Instance == null
            ? new System.Collections.Generic.List<ModLevelInfo>()
            : ModLoader.Instance.GetLevels(ModId);
    }

    public bool LoadLevel(ModLevelInfo level)
    {
        return ModLoader.Instance != null && ModLoader.Instance.LoadLevelScene(ModId, level);
    }
}
