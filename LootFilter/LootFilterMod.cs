using HarmonyLib;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace lootfilter;
public class LootFilterMod : ModSystem
{
    private LootFilterConfig config = new LootFilterConfig();
    private FilterGuiDialog? guiDialog;
    private bool lootfilterToggleKeyHeld = false;
    private bool lootfilterReloadConfigKeyHeld = false;
    public static ICoreAPI? ApiInstance {  get; private set; }
    public LootFilterConfig Config => config;
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        Harmony.DEBUG = false;
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
            guiDialog = new FilterGuiDialog(capi, config, ReloadConfigWrapper);
            capi.Input.RegisterHotKey("lootfilter.toggle", "Toggle Loot Filter GUI", GlKeys.Tilde);
            capi.Input.SetHotKeyHandler("lootfilter.toggle", ToggleGui);
            capi.Input.RegisterHotKey("lootfilter.reloadconfig", "Reload Loot Filter Config", GlKeys.End);
            capi.Input.SetHotKeyHandler("lootfilter.reloadconfig", ReloadConfig);
            capi.Event.KeyUp += (KeyEvent ke) =>
            {
                if (ke.KeyCode == capi.Input.HotKeys["lootfilter.toggle"].CurrentMapping.KeyCode)
                {
                    lootfilterToggleKeyHeld = false;
                }
                if (ke.KeyCode == capi.Input.HotKeys["lootfilter.reloadconfig"].CurrentMapping.KeyCode)
                {
                    lootfilterReloadConfigKeyHeld = false;
                }
            };
        }
        if (api.Side == EnumAppSide.Server)
        {
            var serverApi = api as ICoreServerAPI;
            var commands = new LootFilterCommands(
                serverApi,
                config,
                SaveConfig
                //ReloadConfigWrapper
            );
        }
        var harmony = new Harmony("lootfilter.mod");
        harmony.PatchAll();
    }
    private bool ReloadConfig(KeyCombination? comb)
    {
        if (lootfilterReloadConfigKeyHeld) return false;
        lootfilterReloadConfigKeyHeld = true;

        string configPath = Path.Combine(GamePaths.ModConfig, "lootfilterconfig.json");
        if (File.Exists(configPath))
        {
            try
            {
                config = JsonConvert.DeserializeObject<LootFilterConfig>(File.ReadAllText(configPath)) ?? new LootFilterConfig();
                guiDialog?.UpdateConfig(config);

                (ApiInstance as ICoreClientAPI)?.ShowChatMessage("[Loot Filter] Configuration reloaded.");
                ApiInstance?.Logger.Notification("[Loot Filter] Configuration reloaded successfully.");
            }
            catch (Exception ex)
            {
                (ApiInstance as ICoreClientAPI)?.ShowChatMessage($"[Loot Filter] Error reloading configuration: {ex.Message}");
                ApiInstance?.Logger.Error($"[Loot Filter] Error reloading configuration: {ex}");
            }
        }
        else
        {
            (ApiInstance as ICoreClientAPI)?.ShowChatMessage("[Loot Filter] Configuration file not found!");
            ApiInstance?.Logger.Warning("[Loot Filter] Configuration file not found!");
        }
        return true;
    }
    private void ReloadConfigWrapper()
    {
        string configPath = Path.Combine(GamePaths.ModConfig, "lootfilterconfig.json");
        if (File.Exists(configPath))
        {
            try
            {
                config = JsonConvert.DeserializeObject<LootFilterConfig>(File.ReadAllText(configPath)) ?? new LootFilterConfig();
                guiDialog?.UpdateConfig(config);

                //(ApiInstance as ICoreClientAPI)?.ShowChatMessage("[Loot Filter] Configuration reloaded.");
                //ApiInstance?.Logger.Notification("[Loot Filter] Configuration reloaded successfully.");
            }
            catch (Exception ex)
            {
                (ApiInstance as ICoreClientAPI)?.ShowChatMessage($"[Loot Filter] Error reloading configuration: {ex.Message}");
                ApiInstance?.Logger.Error($"[Loot Filter] Error reloading configuration: {ex}");
            }
        }
        else
        {
            (ApiInstance as ICoreClientAPI)?.ShowChatMessage("[Loot Filter] Configuration file not found!");
            ApiInstance?.Logger.Warning("[Loot Filter] Configuration file not found!");
        }
    }

    public void SaveConfig()
    {
        string configPath = System.IO.Path.Combine(GamePaths.ModConfig, "lootfilterconfig.json");
        System.IO.File.WriteAllText(configPath, Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));
    }
    private bool ToggleGui(KeyCombination comb)
    {
        if (lootfilterToggleKeyHeld) return false;
        lootfilterToggleKeyHeld = true;
        guiDialog?.Toggle();
        return true;
    }
}