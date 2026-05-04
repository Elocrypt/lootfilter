using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace LootFilter
{
    /// <summary>
    /// Harmony prefix on <see cref="EntityBehaviorCollectEntities.OnFoundCollectible"/>
    /// that blocks pickup of items matching the player's filter config.
    /// All real work is server-gated; the patch is a no-op on the client.
    /// </summary>
    [HarmonyPatch(typeof(EntityBehaviorCollectEntities))]
    internal static class LootFilterPatch
    {
        /// <summary>Cached reflection handle for the owning entity of any EntityBehavior.</summary>
        private static readonly FieldInfo EntityField =
            AccessTools.Field(typeof(EntityBehavior), "entity");

        [HarmonyPatch("OnFoundCollectible")]
        [HarmonyPrefix]
        public static bool Prefix(
            Entity foundEntity,
            EntityBehaviorCollectEntities __instance,
            ref bool __result)
        {
            try
            {
                return PrefixCore(foundEntity, __instance, ref __result);
            }
            catch (Exception ex)
            {
                // Never let a mod exception crash the pickup pipeline.
                LootFilterMod.ApiInstance?.Logger?.Error(
                    "[LootFilter] Patch error: {0}", ex);
                return true;
            }
        }

        private static bool PrefixCore(
            Entity foundEntity,
            EntityBehaviorCollectEntities __instance,
            ref bool __result)
        {
            // Server-authoritative: only run when the server API is available.
            // In singleplayer, both sides share the same process, but ServerApi
            // is only set once during server-side Start().  This avoids the old
            // bug where a single static ApiInstance was overwritten by the client.
            ICoreServerAPI sapi = LootFilterMod.ServerApi;
            if (sapi == null) return true;

            // Only care about item entities.
            if (foundEntity is not EntityItem ei) return true;

            // Resolve the collecting entity → player.
            var collector = EntityField?.GetValue(__instance) as Entity;
            if (collector is not EntityPlayer eplayer) return true;

            // Find the IPlayer for this entity among online players.
            // No LINQ in the hot path — use a manual loop.
            IPlayer player = null;
            IPlayer[] allPlayers = sapi.World?.AllOnlinePlayers;
            if (allPlayers == null) return true;

            long targetId = eplayer.EntityId;
            for (int i = 0; i < allPlayers.Length; i++)
            {
                if (allPlayers[i]?.Entity?.EntityId == targetId)
                {
                    player = allPlayers[i];
                    break;
                }
            }
            if (player == null) return true;

            // Retrieve the server-side per-player config.
            var cfg = LootFilterMod.GetServerConfig(player.PlayerUID);
            if (cfg == null) return true;

            // Crouch bypass: if enabled and the player is sneaking, allow all pickups.
            if (cfg.CrouchBypassEnabled)
            {
                EntityControls controls = player.Entity?.Controls;
                if (controls != null && controls.Sneak)
                    return true;
            }

            // Evaluate filter.
            var stack = ei.Itemstack;
            string code = stack?.Collectible?.Code?.ToString() ?? "";
            string name = stack?.GetName() ?? "";

            if (LootFilterMatchLogic.MatchesFilter(cfg, code, name))
            {
                __result = false;   // block pickup
                return false;       // skip original method
            }

            return true;
        }
    }
}
