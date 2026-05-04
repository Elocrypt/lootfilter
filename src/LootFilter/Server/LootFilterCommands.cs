using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace LootFilter
{
    /// <summary>
    /// Registers all <c>/lootfilter</c> chat commands on the server.
    /// Every mutation saves to disk and sends a <see cref="FilterSyncPacket"/>
    /// back to the calling player so their client mirror stays current.
    /// </summary>
    internal class LootFilterCommands
    {
        private readonly ICoreServerAPI sapi;
        private readonly IServerNetworkChannel channel;

        public LootFilterCommands(ICoreServerAPI sapi, IServerNetworkChannel channel)
        {
            this.sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
            this.channel = channel ?? throw new ArgumentNullException(nameof(channel));
            Register();
        }

        private void Register()
        {
            sapi.ChatCommands.Create("lootfilter")
                .WithDescription("Manage your loot filter")
                .RequiresPrivilege("chat")

                // /lootfilter add
                .BeginSubCommand("add")
                    .WithDescription("Add the held item's code to your filter")
                    .HandleWith(CmdAdd)
                .EndSubCommand()

                // /lootfilter remove
                .BeginSubCommand("remove")
                    .WithDescription("Remove the held item's code from your filter")
                    .HandleWith(CmdRemove)
                .EndSubCommand()

                // /lootfilter keyword add|remove
                .BeginSubCommand("keyword")
                    .WithDescription("Manage display-name keywords")
                    .BeginSubCommand("add")
                        .WithArgs(sapi.ChatCommands.Parsers.Word("keyword"))
                        .HandleWith(CmdKeywordAdd)
                    .EndSubCommand()
                    .BeginSubCommand("remove")
                        .WithArgs(sapi.ChatCommands.Parsers.Word("keyword"))
                        .HandleWith(CmdKeywordRemove)
                    .EndSubCommand()
                .EndSubCommand()

                // /lootfilter list
                .BeginSubCommand("list")
                    .WithDescription("Print your current filter")
                    .HandleWith(CmdList)
                .EndSubCommand()

                // /lootfilter reset
                .BeginSubCommand("reset")
                    .WithDescription("Clear all codes and keywords")
                    .HandleWith(CmdReset)
                .EndSubCommand()

                // /lootfilter trash on|off
                .BeginSubCommand("trash")
                    .WithDescription("Toggle Trash-on-Sight (auto-drop filtered items)")
                    .BeginSubCommand("on").HandleWith(ctx => CmdSetBool(ctx, SetAutoDrop, true)).EndSubCommand()
                    .BeginSubCommand("off").HandleWith(ctx => CmdSetBool(ctx, SetAutoDrop, false)).EndSubCommand()
                .EndSubCommand()

                // /lootfilter allowlist on|off
                .BeginSubCommand("allowlist")
                    .WithDescription("Toggle Allowlist Mode (only pick up listed items)")
                    .BeginSubCommand("on").HandleWith(ctx => CmdSetBool(ctx, SetAllowlist, true)).EndSubCommand()
                    .BeginSubCommand("off").HandleWith(ctx => CmdSetBool(ctx, SetAllowlist, false)).EndSubCommand()
                .EndSubCommand()

                // /lootfilter crouch on|off
                .BeginSubCommand("crouch")
                    .WithDescription("Toggle Crouch Bypass (sneak to pick up anything)")
                    .BeginSubCommand("on").HandleWith(ctx => CmdSetBool(ctx, SetCrouch, true)).EndSubCommand()
                    .BeginSubCommand("off").HandleWith(ctx => CmdSetBool(ctx, SetCrouch, false)).EndSubCommand()
                .EndSubCommand();
        }

        // ── Helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Extracts the calling <see cref="IServerPlayer"/> and their config.
        /// Returns null (and sends an error) if the caller is not a player.
        /// </summary>
        private IServerPlayer Require(TextCommandCallingArgs args, out LootFilterConfig cfg)
        {
            cfg = null;
            if (args.Caller.Player is not IServerPlayer sp)
                return null;

            cfg = LootFilterMod.GetServerConfig(sp.PlayerUID);
            return sp;
        }

        /// <summary>Persist to disk, invalidate regex cache, and echo the
        /// authoritative config back to the client.</summary>
        private void SaveAndSync(IServerPlayer sp, LootFilterConfig cfg)
        {
            LootFilterMod.ServerStore.Save(sp.PlayerUID);
            LootFilterMatchLogic.InvalidateCache();
            channel.SendPacket(FilterSyncPacket.FromConfig(cfg), sp);
        }

        // ── Commands ─────────────────────────────────────────────────────

        private TextCommandResult CmdAdd(TextCommandCallingArgs args)
        {
            var sp = Require(args, out var cfg);
            if (sp == null) return TextCommandResult.Error("[LootFilter] Must be a player.");
            if (cfg == null) return TextCommandResult.Error("[LootFilter] Config unavailable.");

            var stack = sp.InventoryManager?.ActiveHotbarSlot?.Itemstack;
            if (stack == null)
                return TextCommandResult.Error("[LootFilter] No item in hand.");

            string code = stack.Collectible?.Code?.ToString();
            if (string.IsNullOrEmpty(code))
                return TextCommandResult.Error("[LootFilter] Item has no code.");

            if (cfg.FilteredItemCodes.Contains(code))
                return TextCommandResult.Success($"[LootFilter] '{code}' is already filtered.");

            cfg.FilteredItemCodes.Add(code);
            SaveAndSync(sp, cfg);
            return TextCommandResult.Success($"[LootFilter] Added '{code}'.");
        }

        private TextCommandResult CmdRemove(TextCommandCallingArgs args)
        {
            var sp = Require(args, out var cfg);
            if (sp == null) return TextCommandResult.Error("[LootFilter] Must be a player.");
            if (cfg == null) return TextCommandResult.Error("[LootFilter] Config unavailable.");

            var stack = sp.InventoryManager?.ActiveHotbarSlot?.Itemstack;
            if (stack == null)
                return TextCommandResult.Error("[LootFilter] No item in hand.");

            string code = stack.Collectible?.Code?.ToString();
            if (string.IsNullOrEmpty(code))
                return TextCommandResult.Error("[LootFilter] Item has no code.");

            if (!cfg.FilteredItemCodes.Remove(code))
                return TextCommandResult.Success($"[LootFilter] '{code}' was not filtered.");

            SaveAndSync(sp, cfg);
            return TextCommandResult.Success($"[LootFilter] Removed '{code}'.");
        }

        private TextCommandResult CmdKeywordAdd(TextCommandCallingArgs args)
        {
            var sp = Require(args, out var cfg);
            if (sp == null) return TextCommandResult.Error("[LootFilter] Must be a player.");
            if (cfg == null) return TextCommandResult.Error("[LootFilter] Config unavailable.");

            string kw = args[0] as string;
            if (string.IsNullOrWhiteSpace(kw))
                return TextCommandResult.Error("[LootFilter] Provide a keyword.");

            if (cfg.FilteredKeywords.Contains(kw))
                return TextCommandResult.Success($"[LootFilter] Keyword '{kw}' already exists.");

            cfg.FilteredKeywords.Add(kw);
            SaveAndSync(sp, cfg);
            return TextCommandResult.Success($"[LootFilter] Keyword '{kw}' added.");
        }

        private TextCommandResult CmdKeywordRemove(TextCommandCallingArgs args)
        {
            var sp = Require(args, out var cfg);
            if (sp == null) return TextCommandResult.Error("[LootFilter] Must be a player.");
            if (cfg == null) return TextCommandResult.Error("[LootFilter] Config unavailable.");

            string kw = args[0] as string;
            if (string.IsNullOrWhiteSpace(kw))
                return TextCommandResult.Error("[LootFilter] Provide a keyword.");

            if (!cfg.FilteredKeywords.Remove(kw))
                return TextCommandResult.Success($"[LootFilter] Keyword '{kw}' was not in the filter.");

            SaveAndSync(sp, cfg);
            return TextCommandResult.Success($"[LootFilter] Keyword '{kw}' removed.");
        }

        private TextCommandResult CmdList(TextCommandCallingArgs args)
        {
            var sp = Require(args, out var cfg);
            if (sp == null) return TextCommandResult.Error("[LootFilter] Must be a player.");
            if (cfg == null) return TextCommandResult.Error("[LootFilter] Config unavailable.");

            var sb = new StringBuilder();
            sb.AppendLine("[LootFilter] Current filter:");

            sb.Append("  Codes (").Append(cfg.FilteredItemCodes.Count).AppendLine("):");
            for (int i = 0; i < cfg.FilteredItemCodes.Count; i++)
                sb.Append("    ").AppendLine(cfg.FilteredItemCodes[i]);

            sb.Append("  Keywords (").Append(cfg.FilteredKeywords.Count).AppendLine("):");
            for (int i = 0; i < cfg.FilteredKeywords.Count; i++)
                sb.Append("    ").AppendLine(cfg.FilteredKeywords[i]);

            sb.Append("  Trash-on-Sight: ").AppendLine(cfg.AutoDropFiltered ? "ON" : "OFF");
            sb.Append("  Allowlist Mode: ").AppendLine(cfg.AllowlistMode ? "ON" : "OFF");
            sb.Append("  Crouch Bypass: ").AppendLine(cfg.CrouchBypassEnabled ? "ON" : "OFF");

            return TextCommandResult.Success(sb.ToString());
        }

        private TextCommandResult CmdReset(TextCommandCallingArgs args)
        {
            var sp = Require(args, out var cfg);
            if (sp == null) return TextCommandResult.Error("[LootFilter] Must be a player.");
            if (cfg == null) return TextCommandResult.Error("[LootFilter] Config unavailable.");

            cfg.FilteredItemCodes.Clear();
            cfg.FilteredKeywords.Clear();
            SaveAndSync(sp, cfg);
            return TextCommandResult.Success("[LootFilter] Filter cleared.");
        }

        // ── Bool-toggle helpers ──────────────────────────────────────────

        private delegate void BoolSetter(LootFilterConfig cfg, bool value);

        private TextCommandResult CmdSetBool(TextCommandCallingArgs args, BoolSetter setter, bool value)
        {
            var sp = Require(args, out var cfg);
            if (sp == null) return TextCommandResult.Error("[LootFilter] Must be a player.");
            if (cfg == null) return TextCommandResult.Error("[LootFilter] Config unavailable.");

            setter(cfg, value);
            SaveAndSync(sp, cfg);

            string label = setter.Method.Name switch
            {
                nameof(SetAutoDrop) => "Trash-on-Sight",
                nameof(SetAllowlist) => "Allowlist Mode",
                nameof(SetCrouch) => "Crouch Bypass",
                _ => "Setting"
            };

            return TextCommandResult.Success($"[LootFilter] {label}: {(value ? "ON" : "OFF")}");
        }

        private static void SetAutoDrop(LootFilterConfig cfg, bool v) => cfg.AutoDropFiltered = v;
        private static void SetAllowlist(LootFilterConfig cfg, bool v) => cfg.AllowlistMode = v;
        private static void SetCrouch(LootFilterConfig cfg, bool v) => cfg.CrouchBypassEnabled = v;
    }
}
