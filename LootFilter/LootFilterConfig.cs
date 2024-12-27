namespace lootfilter;

public class LootFilterConfig
{
    public List<string> FilteredItemCodes { get; set; } = new List<string>();
    public List<string> FilteredCategories { get; set; } = new List<string>();
    public List<string> FilteredKeywords { get; set; } = new List<string>();
}
