// Compile this into Scripts/ExampleMod.dll with references to the game's
// Assembly-CSharp.dll and Unity assemblies before adding it to mod.json.
public class ExampleMod : IBaldiMod
{
    public void Initialize(ModAPI api)
    {
        api.Log("Example mod started.");
        var levels = api.GetLevels();
        api.Log("Levels found: " + levels.Count);
    }
}
