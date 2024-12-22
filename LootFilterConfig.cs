using Newtonsoft.Json;
using System.Collections.Generic;

namespace LootFilter
{
    public class LootFilterConfig
    {
        [JsonProperty("filteredItems")]
        public HashSet<string> FilteredItems { get; set; } = new HashSet<string>();
    }
}