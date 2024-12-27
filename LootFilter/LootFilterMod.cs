using HarmonyLib;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Client;

namespace lootfilter;
public class LootFilterMod : ModSystem
{
    private LootFilterConfig config = new LootFilterConfig();
    private FilterGuiDialog? guiDialog;
    public static ICoreAPI? ApiInstance {  get; private set; }
    public LootFilterConfig Config => config;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        ApiInstance = api;

        string configPath = Path.Combine(GamePaths.ModConfig, "lootfilterconfig.json");
        if (!File.Exists(configPath))
        {
            File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        else
        {
            config = JsonConvert.DeserializeObject<LootFilterConfig>(File.ReadAllText(configPath)) ?? new LootFilterConfig();
        }
        if (api.Side == EnumAppSide.Client)
        {
            var capi = api as ICoreClientAPI;
            guiDialog = new FilterGuiDialog(capi, config);
            capi.Input.RegisterHotKey("lootfilter.toggle", "Toggle Loot Filter GUI", GlKeys.Tilde);
            capi.Input.SetHotKeyHandler("lootfilter.toggle", ToggleGui);
        }
        var harmony = new Harmony("lootfilter.mod");
        harmony.PatchAll();
    }
    private bool ToggleGui(KeyCombination comb)
    {
        guiDialog?.Toggle();
        return true;
    }
}
