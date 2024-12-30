using Vintagestory;
using Vintagestory.API.Common;


namespace lootfilter;

public class LootFilterConfig
{
    private readonly ICoreAPI? api;
    public LootFilterConfig(ICoreAPI? api = null)
    {
        this.api = api;
    }
    public List<string> FilteredItemCodes { get; set; } = new List<string>();
    //public List<string> FilteredCategories { get; set; } = new List<string>();
    public List<string> FilteredKeywords { get; set; } = new List<string>();
    public event Action? OnConfigChanged;
    public void NotifyChange()
    {
        api?.Logger.Debug("[Loot Filter] Config changed.");
        OnConfigChanged?.Invoke();
    }
    public void AddFilteredItem(string itemCode)
    {
        api?.Logger.Debug($"[Loot Filter] Before AddFilteredItem: {string.Join(", ", FilteredItemCodes)}");
        if (!FilteredItemCodes.Contains(itemCode))
        {
            FilteredItemCodes.Add(itemCode);
            NotifyChange();
        }
        api?.Logger.Debug($"[Loot Filter] After AddFilteredItem: {string.Join(", ", FilteredItemCodes)}");
    }
    public void RemoveFilteredItem(string itemCode)
    {
        api?.Logger.Debug($"[Loot Filter] Before RemoveFilteredItem: {string.Join(", ", FilteredItemCodes)}");
        if (FilteredItemCodes.Remove(itemCode))
        {
            NotifyChange();
        }
        api?.Logger.Debug($"[Loot Filter] After RemoveFilteredItem: {string.Join(", ", FilteredItemCodes)}");
    }
    public void AddKeyword(string keyword)
    {
        api?.Logger.Debug($"[Loot Filter] Before AddKeyword: {string.Join(", ", FilteredKeywords)}");
        if (!FilteredKeywords.Contains(keyword))
        {
            FilteredKeywords.Add(keyword);
            NotifyChange();
        }
        api?.Logger.Debug($"[Loot Filter] After AddKeyword: {string.Join(", ", FilteredKeywords)}");
    }
    public void RemoveKeyword(string keyword)
    {
        api?.Logger.Debug($"[Loot Filter] Before RemoveKeyword: {string.Join(", ", FilteredKeywords)}");
        if (FilteredKeywords.Remove(keyword))
        {
            NotifyChange();
        }
        api?.Logger.Debug($"[Loot Filter] After RemoveKeyword: {string.Join(", ", FilteredKeywords)}");
    }
    public void ClearFilters()
    {
        FilteredItemCodes.Clear();
        FilteredKeywords.Clear();
        NotifyChange();
    }
}
