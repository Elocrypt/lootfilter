using lootfilter;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

public class LootFilterCommands
{
    private readonly ICoreServerAPI api;
    private LootFilterConfig config;
    private readonly Action saveConfig;

    public LootFilterCommands(ICoreServerAPI api, LootFilterConfig config, Action saveConfig)
    {
        this.api = api ?? throw new ArgumentNullException(nameof(api));
        this.config = config;
        this.saveConfig = saveConfig ?? throw new ArgumentNullException(nameof(saveConfig));
        RegisterCommands();
    }
    private void RegisterCommands()
    {
        api.ChatCommands
            .Create("lootfilter")
            .WithDescription("Manage the loot filter")
            .RequiresPrivilege("chat")
            .BeginSubCommand("add")
                .WithDescription("Add the currently held item to the loot filter.")
                .HandleWith(AddItemToFilterCommand)
            .EndSubCommand()
            .BeginSubCommand("keyword")
                .WithDescription("Manage keywords for the loot filter.")
                .BeginSubCommand("add")
                    .WithDescription("Add a keyword to the filter.")
                    .WithArgs(api.ChatCommands.Parsers.Word("keyword"))
                    .HandleWith(AddKeywordCommand)
                .EndSubCommand()
                .BeginSubCommand("remove")
                    .WithDescription("Remove a keyword from the filter.")
                    .WithArgs(api.ChatCommands.Parsers.Word("keyword"))
                    .HandleWith(RemoveKeywordCommand)
                .EndSubCommand()
            .EndSubCommand()
            .BeginSubCommand("remove")
                .WithDescription("Remove the currently held item from the loot filter.")
                .HandleWith(RemoveItemFromFilterCommand)
            .EndSubCommand()
            .BeginSubCommand("reset")
                .WithDescription("Clear all items from the loot filter.")
                .HandleWith(ResetItemFilterCommand)
            .EndSubCommand();
    }
    private IServerPlayer? ValidatePlayer(TextCommandCallingArgs args, out int groupId)
    {
        groupId = args.Caller.FromChatGroupId;
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
        {
            api.SendMessageToGroup(groupId, "[Loot Filter] You must be a player to use this command.", EnumChatType.CommandError);
            return null;
        }
        return player;
    }
    private TextCommandResult AddItemToFilterCommand(TextCommandCallingArgs args)
    {
        int groupId;
        var player = ValidatePlayer(args, out groupId);
        if (player == null)
        {
            return TextCommandResult.Error("[Loot Filter] Command execution failed.");
        }
        var heldItem = player.InventoryManager.ActiveHotbarSlot.Itemstack;
        if (heldItem == null)
        {
            return TextCommandResult.Error("[Loot Filter] No item in hand to add.");
        }
        string itemCode = heldItem.Collectible.Code.ToString();
        if (!config.FilteredItemCodes.Contains(itemCode))
        {
            config.AddFilteredItem(itemCode);
            saveConfig();                    
            return TextCommandResult.Success($"[Loot Filter] Added '{itemCode}' to the filter.");
        }

        return TextCommandResult.Error($"[Loot Filter] '{itemCode}' is already in the filter.");
    }
    private TextCommandResult RemoveItemFromFilterCommand(TextCommandCallingArgs args)
    {
        try
        {
            int groupId;
            var player = ValidatePlayer(args, out groupId);
            if (player == null)
            {
                return TextCommandResult.Error("[Loot Filter] Command execution failed.");
            }
            var heldItem = player.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (heldItem == null)
            {
                return TextCommandResult.Error("[Loot Filter] No item in hand to remove.");
            }
            string itemCode = heldItem.Collectible.Code.ToString();
            config.RemoveFilteredItem(itemCode); // Removes the item and triggers NotifyChange
            saveConfig();
            return TextCommandResult.Success($"[Loot Filter] Removed '{itemCode}' from the filter.");
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[Loot Filter] Error in RemoveItemFromFilterCommand: {ex}");
            return TextCommandResult.Error("[Loot Filter] An unexpected error occurred.");
        }
    }
    private TextCommandResult AddKeywordCommand(TextCommandCallingArgs args)
    {
        int groupId;
        var player = ValidatePlayer(args, out groupId);
        if (player == null)
        {
            return TextCommandResult.Error("[Loot Filter] Command execution failed.");
        }
        var keyword = args[0] as string;
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return TextCommandResult.Error("[Loot Filter] You must specify a keyword to add.");
        }
        config.AddKeyword(keyword);
        saveConfig();
        return TextCommandResult.Success($"[Loot Filter] Keyword '{keyword}' added to the filter.");
    }
    private TextCommandResult RemoveKeywordCommand(TextCommandCallingArgs args)
    {
        int groupId;
        var player = ValidatePlayer(args, out groupId);
        if (player == null)
        {
            return TextCommandResult.Error("[Loot Filter] Command execution failed.");
        }
        var keyword = args[0] as string;
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return TextCommandResult.Error("[Loot Filter] You must specify a keyword to remove.");
        }
        api.Logger.Notification($"[Loot Filter] Keywords before removal: {string.Join(", ", config.FilteredKeywords)}");
        config.RemoveKeyword(keyword);
        api.Logger.Notification($"[Loot Filter] Keywords after removal: {string.Join(", ", config.FilteredKeywords)}");
        saveConfig();
        return TextCommandResult.Success($"[Loot Filter] Keyword '{keyword}' removed from the filter.");
    }

    private TextCommandResult ResetItemFilterCommand(TextCommandCallingArgs args)
    {
        int groupId;
        var player = ValidatePlayer(args, out groupId);
        if (player == null)
        {
            return TextCommandResult.Error("[Loot Filter] Command execution failed.");
        }
        config.ClearFilters();
        saveConfig();
        return TextCommandResult.Success("[Loot Filter] Item filter cleared.");
    }
}