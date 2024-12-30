using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace lootfilter;

[HarmonyPatch(typeof(EntityBehaviorCollectEntities))]
public static class LootFilterPatch
{
    [HarmonyPatch("OnFoundCollectible")]
    [HarmonyPrefix]
    public static bool OnFoundCollectiblePrefix(Entity foundEntity, Entity __instance, ref bool __result)
    {
        var config = LootFilterMod.ApiInstance?.ModLoader.GetModSystem<LootFilterMod>()?.Config;
        if (config == null) return true;
        if (foundEntity is EntityItem entityItem)
        {
            string itemCode = entityItem.Itemstack.Collectible.Code.ToString();
            string itemName = entityItem.Itemstack.GetName();
            if (config.FilteredItemCodes.Contains(itemCode) ||
                /*config.FilteredCategories.Exists(cat => entityItem.Itemstack.Collectible.Attributes?[cat]?.AsBool() == true) ||*/
                config.FilteredKeywords.Exists(keyword => itemName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                __result = false;
                return false;
            }
            return true;
        }
        return true;
    }
}