using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace LootFilter
{
    /// <summary>
    /// Mod entry point.  Wires up networking, Harmony patches, the GUI (client),
    /// and the per-player config store + auto-drop tick (server).
    /// </summary>
    public class LootFilterMod : ModSystem
    {
        private const string ChannelName = "lootfilter";
        private const string HarmonyId   = "lootfilter.mod";
        private const int    AutoDropIntervalMs = 500;

        // ── Shared state ─────────────────────────────────────────────────
        // In singleplayer, Start() is called twice — once for the server,
        // once for the client — each with a different ICoreAPI.  A single
        // static field would be overwritten by the second call, breaking
        // whichever side ran first.  We store each side separately.

        /// <summary>Server API reference.  Null on dedicated clients.</summary>
        internal static ICoreServerAPI? ServerApi { get; private set; }

        /// <summary>Client API reference.  Null on dedicated servers.</summary>
        internal static ICoreClientAPI? ClientApi { get; private set; }

        /// <summary>
        /// Convenience accessor used only for the Harmony patch's catch-all
        /// logger.  Prefer <see cref="ServerApi"/> or <see cref="ClientApi"/>
        /// everywhere else.
        /// </summary>
        public static ICoreAPI? ApiInstance => (ICoreAPI?)ServerApi ?? ClientApi;

        // ── Server-only state ────────────────────────────────────────────
        internal static PerPlayerConfigStore? ServerStore { get; private set; }
        private IServerNetworkChannel serverChannel = null!;

        // ── Client-only state ────────────────────────────────────────────

        /// <summary>
        /// Mirror of the server-authoritative config, populated exclusively
        /// by incoming <see cref="FilterSyncPacket"/> messages.  The GUI
        /// reads from this; it never writes to disk.
        /// </summary>
        internal LootFilterConfig ClientMirror { get; private set; } = new LootFilterConfig();
        private IClientNetworkChannel clientChannel = null!;
        private FilterGuiDialog guiDialog = null!;

        // ─────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            if (api.Side == EnumAppSide.Server)
                StartServer(api as ICoreServerAPI ?? throw new InvalidOperationException());

            if (api.Side == EnumAppSide.Client)
                StartClient(api as ICoreClientAPI ?? throw new InvalidOperationException());

            // Harmony patches apply on both sides; the patch itself gates
            // on ServerApi != null before doing real work.
            new Harmony(HarmonyId).PatchAll();
        }

        public override void Dispose()
        {
            new Harmony(HarmonyId).UnpatchAll(HarmonyId);

            // Clear static state so a mod reload in the same process
            // doesn't see stale references.
            ServerApi   = null;
            ClientApi   = null;
            ServerStore = null;

            base.Dispose();
        }

        // ─────────────────────────────────────────────────────────────────
        //  Server
        // ─────────────────────────────────────────────────────────────────

        private void StartServer(ICoreServerAPI sapi)
        {
            ServerApi = sapi;

            // Per-player config store — pure file-backed, no API dependency.
            string storeRoot = Path.Combine(GamePaths.ModConfig, "LootFilter", "players");
            ServerStore = new PerPlayerConfigStore(storeRoot);

            // Network channel: receive FilterUpdatePacket, send FilterSyncPacket.
            serverChannel = sapi.Network
                .RegisterChannel(ChannelName)
                .RegisterMessageType<FilterUpdatePacket>()
                .RegisterMessageType<FilterSyncPacket>()
                .SetMessageHandler<FilterUpdatePacket>(OnFilterUpdateReceived);

            // Send the player their saved config on join.
            sapi.Event.PlayerJoin += OnPlayerJoin;

            // Trash-on-Sight server tick.
            sapi.Event.RegisterGameTickListener(ServerAutoDropTick, AutoDropIntervalMs);

            // Chat commands — instantiated here so they actually exist.
            _ = new LootFilterCommands(sapi, serverChannel);

            sapi.Logger.Notification("[LootFilter] Server-side initialisation complete.");
        }

        /// <summary>
        /// Called when a client sends an updated filter config.
        /// Validates, persists, and echoes the authoritative state back.
        /// </summary>
        private void OnFilterUpdateReceived(IServerPlayer fromPlayer, FilterUpdatePacket packet)
        {
            if (fromPlayer == null || packet == null) return;

            string uid = fromPlayer.PlayerUID;
            LootFilterConfig cfg = packet.ToConfig();

            ServerStore!.Put(uid, cfg);
            LootFilterMatchLogic.InvalidateCache();

            // Confirm back to the sender so their client mirror is authoritative.
            serverChannel.SendPacket(FilterSyncPacket.FromConfig(cfg), fromPlayer);

            ServerApi?.Logger.Debug(
                "[LootFilter] Saved config for {0} ({1} codes, {2} keywords, {3} attr rules).",
                fromPlayer.PlayerName,
                cfg.FilteredItemCodes.Count,
                cfg.FilteredKeywords.Count,
                cfg.FilteredAttributes.Count);
        }

        private void OnPlayerJoin(IServerPlayer player)
        {
            if (player == null) return;

            LootFilterConfig cfg = ServerStore!.Get(player.PlayerUID);
            serverChannel.SendPacket(FilterSyncPacket.FromConfig(cfg), player);
        }

        // ── Trash-on-Sight auto-drop tick ────────────────────────────────

        /// <summary>Scratch list reused across ticks to avoid allocations.</summary>
        private readonly List<int> dropSlotIndices = new List<int>();

        private void ServerAutoDropTick(float dt)
        {
            if (ServerApi == null) return;
            IPlayer[]? players = ServerApi.World?.AllOnlinePlayers;
            if (players == null) return;

            for (int p = 0; p < players.Length; p++)
            {
                IServerPlayer? sp = players[p] as IServerPlayer;
                if (sp?.InventoryManager == null) continue;

                // Skip creative-mode players.
                if (sp.WorldData?.CurrentGameMode == EnumGameMode.Creative) continue;

                LootFilterConfig cfg = ServerStore!.Get(sp.PlayerUID);
                if (cfg == null || !cfg.AutoDropFiltered) continue;

                // Crouch bypass: suppress auto-drop while sneaking.
                EntityControls? controls = sp.Entity?.Controls;
                if (cfg.CrouchBypassEnabled && controls != null && controls.Sneak)
                    continue;

                AutoDropForPlayer(sp, cfg);
            }
        }

        private static readonly string[] InventoryIds =
            { "hotbar", "backpack", "character", "crafting" };

        private void AutoDropForPlayer(IServerPlayer sp, LootFilterConfig cfg)
        {
            for (int invIdx = 0; invIdx < InventoryIds.Length; invIdx++)
            {
                IInventory? inv = sp.InventoryManager.GetOwnInventory(InventoryIds[invIdx]);
                if (inv == null) continue;

                dropSlotIndices.Clear();

                for (int i = 0; i < inv.Count; i++)
                {
                    ItemSlot slot = inv[i];
                    if (slot?.Empty != false) continue;

                    ItemStack stack = slot.Itemstack;
                    string code = stack?.Collectible?.Code?.ToString() ?? "";
                    string name = stack?.GetName() ?? "";

                    // Pass the full stack so attribute rules (durability%, freshness, …) work.
                    if (LootFilterMatchLogic.MatchesFilter(cfg, code, name, stack))
                        dropSlotIndices.Add(i);
                }

                if (dropSlotIndices.Count > 0 && inv is InventoryBase invBase)
                {
                    EntityPos? pos = sp.Entity?.Pos;
                    if (pos == null) continue;

                    // Drop 0.5 blocks in front of the player at foot level.
                    double yaw = pos.Yaw;
                    double dropX = pos.X - Math.Sin(yaw) * 0.5;
                    double dropY = pos.Y + 0.1;
                    double dropZ = pos.Z - Math.Cos(yaw) * 0.5;
                    var dropPos = new Vintagestory.API.MathTools.Vec3d(dropX, dropY, dropZ);

                    invBase.DropSlots(dropPos, dropSlotIndices.ToArray());
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Client
        // ─────────────────────────────────────────────────────────────────

        private void StartClient(ICoreClientAPI capi)
        {
            ClientApi = capi;

            // Network channel: receive FilterSyncPacket, send FilterUpdatePacket.
            clientChannel = capi.Network
                .RegisterChannel(ChannelName)
                .RegisterMessageType<FilterUpdatePacket>()
                .RegisterMessageType<FilterSyncPacket>()
                .SetMessageHandler<FilterSyncPacket>(OnFilterSyncReceived);

            // GUI dialog — reads from ClientMirror, sends packets via SendConfigToServer.
            guiDialog = new FilterGuiDialog(capi, this);

            // Hotkey: toggle the filter GUI.
            capi.Input.RegisterHotKey("lootfilter.toggle", "Toggle Loot Filter GUI", GlKeys.Tilde);
            capi.Input.SetHotKeyHandler("lootfilter.toggle", OnToggleGui);

            capi.Logger.Notification("[LootFilter] Client-side initialisation complete.");
        }

        private void OnFilterSyncReceived(FilterSyncPacket packet)
        {
            if (packet == null) return;

            ClientMirror = packet.ToConfig();
            guiDialog?.OnMirrorUpdated();

            ClientApi?.Logger.Debug(
                "[LootFilter] Received sync: {0} codes, {1} keywords, {2} attr rules.",
                ClientMirror.FilteredItemCodes.Count,
                ClientMirror.FilteredKeywords.Count,
                ClientMirror.FilteredAttributes.Count);
        }

        /// <summary>
        /// Called by the GUI (or anything client-side) to push a config
        /// update to the server.
        /// </summary>
        internal void SendConfigToServer(LootFilterConfig cfg)
        {
            clientChannel?.SendPacket(FilterUpdatePacket.FromConfig(cfg));
        }

        private bool toggleKeyHeld;

        private bool OnToggleGui(KeyCombination comb)
        {
            // Prevent repeated toggles while the key is held down.
            if (toggleKeyHeld) return false;
            toggleKeyHeld = true;

            if (ClientApi != null)
            {
                ClientApi.Event.RegisterCallback(_ => { toggleKeyHeld = false; }, 200);
            }

            guiDialog?.Toggle();
            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Public helpers for the Harmony patch
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Retrieves the server-side config for a given player UID.
        /// Returns null when called before the store exists or the server
        /// hasn't initialised.
        /// </summary>
        internal static LootFilterConfig? GetServerConfig(string uid)
        {
            return ServerStore?.Get(uid);
        }
    }
}
