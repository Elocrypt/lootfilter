using Vintagestory.API.Client;
using ImGuiNET;

namespace LootFilter
{
    public class ImGuiRenderer : IRenderer
    {
        private ICoreClientAPI capi;
        private LootFilterSystem lootFilterSystem;

        public double RenderOrder => 0.0; // Render before other elements
        public int RenderRange => int.MaxValue;

        public ImGuiRenderer(ICoreClientAPI capi, LootFilterSystem lootFilterSystem)
        {
            this.capi = capi;
            this.lootFilterSystem = lootFilterSystem;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (!lootFilterSystem.ShowFilterUI) return;

            ImGui.Begin("Loot Filter Configuration");

            if (ImGui.Button("Add Held Item"))
            {
                var heldItem = capi.World.Player.InventoryManager.ActiveHotbarSlot.Itemstack;
                if (heldItem != null)
                {
                    string itemCode = heldItem.Collectible.Code.ToString();
                    lootFilterSystem.Config.FilteredItems.Add(itemCode);
                    lootFilterSystem.SaveFilterConfig();
                    capi.ShowChatMessage($"Added {itemCode} to filter.");
                }
            }

            if (ImGui.CollapsingHeader("Filtered Items"))
            {
                foreach (var itemCode in new List<string>(lootFilterSystem.Config.FilteredItems))
                {
                    ImGui.Text(itemCode);
                    ImGui.SameLine();
                    if (ImGui.Button($"Remove##{itemCode}"))
                    {
                        lootFilterSystem.Config.FilteredItems.Remove(itemCode);
                        lootFilterSystem.SaveFilterConfig();
                        capi.ShowChatMessage($"Removed {itemCode} from filter.");
                    }
                }
            }

            ImGui.End();
        }

        public void Dispose()
        {
            // No cleanup necessary, but this satisfies the IRenderer interface
        }
    }
}