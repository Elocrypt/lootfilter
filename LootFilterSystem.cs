using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using System.Collections.Generic;

namespace LootFilter
{
    public class LootFilterSystem : ModSystem
    {
        private ICoreClientAPI? capi; // Nullable to satisfy initialization
        public LootFilterConfig Config { get; private set; } = new LootFilterConfig();
        public bool ShowFilterUI { get; private set; } = false; // Control visibility of ImGui UI

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            // Load filter configuration or create default
            Config = api.LoadModConfig<LootFilterConfig>("lootfilter.json") ?? new LootFilterConfig();
            SaveFilterConfig(); // Ensure the config file is created if it doesn't exist

            // Register hotkey for UI
            api.Input.RegisterHotKey("lootfilterui", "Open Loot Filter UI", GlKeys.F10);
            api.Input.SetHotKeyHandler("lootfilterui", ToggleFilterUI);

            // Hook into the tick event to intercept item pickups
            api.Event.RegisterGameTickListener(OnClientTick, 50); // Check every 50ms

            // Register custom renderer for ImGui
            api.Event.RegisterRenderer(new ImGuiRenderer(api, this), EnumRenderStage.Before);
        }

        // Toggle the visibility of the filter UI
        private bool ToggleFilterUI(KeyCombination hotkey)
        {
            ShowFilterUI = !ShowFilterUI;
            return true;
        }

        // Client tick handler to monitor nearby items
        private void OnClientTick(float deltaTime)
        {
            if (capi == null) return;

            var playerEntity = capi.World.Player.Entity;

            // Get all nearby entities in pickup range
            foreach (var entity in capi.World.GetEntitiesAround(
                new Vec3d(playerEntity.Pos.X, playerEntity.Pos.Y, playerEntity.Pos.Z),
                2f, 2f, e => e is EntityItem))
            {
                EntityItem entityItem = entity as EntityItem;

                if (entityItem == null || entityItem.Itemstack == null) continue;

                string itemCode = entityItem.Itemstack.Collectible.Code.ToString();

                if (Config.FilteredItems.Contains(itemCode))
                {
                    // Skip pickup for filtered items
                    entityItem.WatchedAttributes.SetBool("preventPickup", true); // Mark item to be skipped
                }
                else
                {
                    // Allow pickup for non-filtered items
                    entityItem.WatchedAttributes.RemoveAttribute("preventPickup");
                }
            }
        }

        // Save the filter configuration
        public void SaveFilterConfig()
        {
            capi?.StoreModConfig(Config, "lootfilter.json");
        }
    }
}
